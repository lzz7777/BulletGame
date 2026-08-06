// Copyright (c) Bytedance. All rights reserved.
// Description: 性能监控工具

using System.Collections.Generic;
using System.Linq;
using ByteDance.LiveOpenSdk.Perf;
using ByteDance.LiveOpenSdk.Runtime;
using ByteDance.LiveOpenSdk.Utilities;
using UnityEngine;

namespace Douyin.LiveOpenSDK
{
    public interface IPerfMonitor
    {
        void StartMonitor();
    }

    internal class PerfMonitorImpl : MonoBehaviour, IPerfMonitor
    {
        private static IPerfMonitor _instance;

        public static IPerfMonitor Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject($"[{nameof(IPerfMonitor)}]");
                    _instance = go.AddComponent<PerfMonitorImpl>();
                }

                return _instance;
            }
        }

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private readonly List<IPerfIssueReporter> _reporters = new();

        /// <summary>
        /// 启动性能数据上报
        /// </summary>
        void IPerfMonitor.StartMonitor()
        {
            Init();
        }
        async void Init()
        {
            await PerfMonitorConfigUtils.RequestConfig();
            _reporters.Add(PerfReporter.Instance.AddListener<IssueFpsMonitor>());
            _reporters.Add(PerfReporter.Instance.AddListener<IssueJankMonitor>());
            _reporters.Add(PerfReporter.Instance.AddListener<IssueMemMonitor>());

            PerfReporter.Start();
        }

        private void Update()
        {
            foreach (var reporter in _reporters)
            {
                reporter.CheckReport();
            }
        }

        /// <summary>
        /// 性能数据上报
        /// </summary>
        interface IPerfIssueReporter : IPerfListener
        {
            /// <summary>
            /// Unity Thread
            /// </summary>
            void CheckReport();
        }

        /// <summary>
        /// 连续 30 秒 FPS 小于 20
        /// </summary>
        private class IssueFpsMonitor : IPerfIssueReporter
        {
            /// <summary>
            /// 这30s内的所有帧
            /// </summary>
            private readonly List<FpsInfo> _frames = new();

            private readonly int TimeFrame = PerfMonitorConfigUtils.FpsConfig.TimeFrame;
            private readonly int ReportThreshold = PerfMonitorConfigUtils.FpsConfig.Min;
            private struct FpsInfo
            {
                public long FrameTime;
                public long Time;
            }
            public void Start(InitInfo info)
            {
            }

            public void Stop()
            {
            }

            public void OnPerfItem(IReportItem item)
            {
                if (item is FrameInfo f)
                {
                    var mi = new FpsInfo
                    {
                        FrameTime = f.FrameTime,
                        Time = f.Time,
                    };
                    _frames.Add(mi);

                    CheckIssue();
                }
            }

            void IPerfIssueReporter.CheckReport()
            {
            }
            private void CheckIssue()
            {
                if (StripFrames())
                {
                    var time = (_frames[^1].Time - _frames[0].Time) / 1000;
                    var fps = Mathf.FloorToInt(_frames.Count / time);

                    if (fps < ReportThreshold)
                    {
                        ReportIssue(fps);
                        _frames.Clear();
                    }
                }
            }
            /// <summary>
            /// 将超过 30s的帧剔除
            /// </summary>
            /// <returns>返回 True 表示：达成条件，即剩下的这些全是最近30s内的数据</returns>
            private bool StripFrames()
            {
                var last = _frames[^1];
                while (_frames.Count >= 2)
                {
                    var f1 = _frames[0];
                    if (last.Time - f1.Time > TimeFrame)
                    {
                        var f2 = _frames[1];
                        if (last.Time - f2.Time < TimeFrame)
                        {
                            _frames.RemoveAt(0);
                            return true;
                        }

                        _frames.RemoveAt(0);
                        continue;
                    }

                    break;
                }

                return false;
            }

            private void ReportIssue(int fps)
            {
                LiveOpenSdk.Instance.GetPerfReportService().ReportFPS(fps);
            }
        }

        /// <summary>
        /// 瞬时帧率，计时单位1min
        ///     严重卡顿: a. FrameTime > 前3帧平均耗时2倍，且 > 3倍电影帧耗时 b. 1分钟内卡顿累计 > 5次
        ///     普通卡顿: a. FrameTime > 前3帧平均耗时2倍，且 > 2倍电影帧耗时 b. 1分钟内卡顿累计 > 20次
        /// </summary>
        private class IssueJankMonitor : IPerfIssueReporter
        {
            // 计时单位1min
            private readonly int TimeFrame = PerfMonitorConfigUtils.JankConfig.TimeFrame;
            // 前3帧
            private readonly int PreFramesCount = PerfMonitorConfigUtils.JankConfig.PreFrameCount;
            // 从严重到普通的上报要求
            private readonly JankLevelData[] _jankLevelDatas = PerfMonitorConfigUtils.JankConfig.JankLevelLimits;

            /// <summary>
            /// 这 1min 内的所有帧
            /// </summary>
            private readonly List<FpsInfo> _frames = new();

            // p0，p1记的次数
            private List<int> _jankTimes = new List<int>(){0, 0};
            private struct FpsInfo
            {
                public long FrameTime;
                public long Time;
            }
            public void Start(InitInfo info)
            {
            }

            public void Stop()
            {
            }

            public void OnPerfItem(IReportItem item)
            {
                if (item is FrameInfo f)
                {
                    var mi = new FpsInfo
                    {
                        FrameTime = f.FrameTime,
                        Time = f.Time,
                    };
                    _frames.Add(mi);

                    CheckIssue();
                }
            }

            void IPerfIssueReporter.CheckReport()
            {
            }

            private void CheckIssue()
            {
                CheckJank();
                if (StripFrames())
                {
                    for (int i = 0; i < _jankTimes.Count; i++)
                    {
                        if (_jankTimes[i] > _jankLevelDatas[i].Times)
                        {
                            ReportIssue((int)_jankLevelDatas[i].Level, _jankTimes[i]);
                            ResetData();
                            break;
                        }
                    }
                }
            }

            private void CheckJank()
            {
                if (_frames.Count < PreFramesCount + 1)
                {
                    return;
                }
                for (int i = 0; i < _jankLevelDatas.Length; i++)
                {
                    var limit = _jankLevelDatas[i];
                    var lastFrame = _frames[^1];
                    long time = 0;
                    for (int j = 0; j < PreFramesCount; j++)
                    {
                        time += _frames[_frames.Count - 2 - j].FrameTime;
                    }
                    if (lastFrame.FrameTime * PreFramesCount > limit.FrameTimeTimes * time  && lastFrame.FrameTime > limit.MovieFrameTimeCostTimes)
                    {
                        _jankTimes[i]++;
                    }
                }
            }

            private void ResetData()
            {
                _frames.Clear();
                _jankTimes = _jankTimes.ConvertAll(x => 0);
            }
            /// <summary>
            /// 将超过 1min 的帧剔除
            /// </summary>
            /// <returns>返回 True 表示：达成条件，即剩下的这些全是最近30s内的数据</returns>
            private bool StripFrames()
            {
                var last = _frames[^1];
                while (_frames.Count >= 2)
                {
                    var f1 = _frames[0];
                    if (last.Time - f1.Time > TimeFrame)
                    {
                        var f2 = _frames[1];
                        if (last.Time - f2.Time < TimeFrame)
                        {
                            _frames.RemoveAt(0);
                            return true;
                        }

                        _frames.RemoveAt(0);
                        continue;
                    }

                    break;
                }

                return false;
            }

            private void ReportIssue(int leve, int count)
            {
                LiveOpenSdk.Instance.GetPerfReportService().ReportJank(leve, count);
            }
        }

        /// <summary>
        /// 连续 1 分钟内存占用 > 80%
        /// </summary>
        private class IssueMemMonitor : IPerfIssueReporter
        {
            private struct MemInfo
            {
                public float Percent;
                public long Time;
            }

            /// <summary>
            /// 这一分钟内的所有帧
            /// </summary>
            private readonly List<MemInfo> _frames = new();

            private readonly int TimeFrame = PerfMonitorConfigUtils.MemoryConfig.TimeFrame;

            private readonly float ReportThreshold = PerfMonitorConfigUtils.MemoryConfig.MaxPercent;

            private long _systemMem;

            void IPerfListener.Start(InitInfo info)
            {
                _systemMem = info.SystemMemorySize * 1024 / 1024 * 1024 * 1024; // 转化成字节
            }

            void IPerfListener.Stop()
            {
            }

            void IPerfListener.OnPerfItem(IReportItem item)
            {
                if (item is FrameInfo f)
                {
                    var mi = new MemInfo
                    {
                        Percent = (float)f.Memory / _systemMem,
                        Time = f.Time,
                    };
                    _frames.Add(mi);

                    CheckIssue();
                }
            }

            void IPerfIssueReporter.CheckReport()
            {
            }

            /// <summary>
            /// 将超过 1 分钟的帧剔除
            /// </summary>
            /// <returns>返回 True 表示：达成条件，即剩下的这些全是最近一分钟内的数据</returns>
            private bool StripFrames()
            {
                var last = _frames[^1];
                while (_frames.Count >= 2)
                {
                    var f1 = _frames[0];
                    if (last.Time - f1.Time > TimeFrame)
                    {
                        var f2 = _frames[1];
                        if (last.Time - f2.Time < TimeFrame)
                        {
                            // f1 超过 1 分钟，f2 在 1 分钟内
                            _frames.RemoveAt(0);
                            return true;
                        }

                        _frames.RemoveAt(0);
                        continue;
                    }

                    break;
                }

                return false;
            }

            private void CheckIssue()
            {
                if (StripFrames())
                {
                    var percent = 0f;
                    for (var i = 0; i < _frames.Count; i++)
                    {
                        percent += _frames[i].Percent;
                    }
                    percent /= _frames.Count;

                    if (percent >= ReportThreshold)
                    {
                        ReportIssue(percent);
                        _frames.Clear();
                    }
                }
            }

            private void ReportIssue(float percent)
            {
                LiveOpenSdk.Instance.GetPerfReportService().ReportMemory(Mathf.FloorToInt(percent * 100));
            }
        }

    }
}