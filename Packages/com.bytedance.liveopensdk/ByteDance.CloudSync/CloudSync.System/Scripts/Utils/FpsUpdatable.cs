// Copyright (c) Bytedance. All rights reserved.
// Author: DONEY Dong
// Date: 2025/04/27
// Description:

using System;
using System.Linq;
using UnityEngine;

namespace ByteDance.CloudSync
{
    /// <summary>
    /// Fps更新
    /// 提供如下作用：
    /// 1 定时间隔的打印Fps
    /// 2 定时输出便于排查确认主线程是否卡死。
    /// </summary>
    public class FpsUpdatable : ISafeActionsUpdatable
    {
        private float CurrentFPS { get; set; }
        private float FrameTime { get; set; }

        private int _hasValidFrameCount;

        // 从多少帧起计算
        private int StartFrame { get; set; } = 1;

        // 间隔多少时间输出一次。 若<=0无效。 单位: 秒
        private long OutputIntervalThreshold => Application.isEditor ? OutputTimeIntervalEditor : OutputTimeInterval;

        private long OutputTimeInterval { get; set; } = 10;

        private long OutputTimeIntervalEditor { get; set; } = 10;

        // 检测游戏卡死的阈值（秒）
        private float FreezeDetectionThreshold { get; set; } = 5f;

        private static long NowTimestampMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        private static readonly SdkDebugLogger Debug = new("CloudSync");

        // 区段信息结构体
        private struct IntervalInfo
        {
            public FrameInfo BeginFrame;
            public FrameInfo EndFrame;
            public DateTime BeginTime => BeginFrame.BeginTime; // 起始时间戳。 为该区段第一个`FrameInfo`的起始时间戳
            public DateTime EndTime => EndFrame.EndTime; // 结束时间戳。 为该区段最后一个`FrameInfo`的结束时间戳
            public float BeginUnscaledTime; // 起始时刻（秒）。 为该区段第一个`FrameInfo`的`BeginUnscaledTime`
            public float EndUnscaledTime; // 结束时刻（秒）。 为该区段最后一个`FrameInfo`的`EndUnscaledTime`
            public float DurationSec; // 累计时长（秒）
            public int FrameCount; // 累计帧数量
            public float FPS; // 算得FPS
            public float AvgFrameTime => FrameCount > 0 ? DurationSec / FrameCount : DurationSec;
            public bool IsFinish { get; set; }

            public static IntervalInfo BeginWith(FrameInfo beginFrame)
            {
                return new IntervalInfo
                {
                    BeginUnscaledTime = beginFrame.BeginUnscaledTime,
                    BeginFrame= beginFrame
                };
            }

            public void EndWith(FrameInfo endFrame)
            {
                EndUnscaledTime = endFrame.EndUnscaledTime;
                EndFrame = endFrame;
            }
        }

        // 帧信息结构体
        private struct FrameInfo
        {
            public float RealTime; // 获得此数据的时刻（秒）。 赋值为`Time.realtimeSinceStartup`（当前实时，一帧内可变）
            public float DeltaRealTime; // 距离上一次`RealTime`间隔
            public DateTime DateTime; // 获得此数据的时间戳（当前实时）
            public DateTime BeginTime; // 起始时间戳。 赋值为前一帧`FrameInfo`的`EndTime`
            public DateTime EndTime; // 结束时间戳
            public float BeginUnscaledTime; // 起始时刻（秒）。 赋值为前一帧`FrameInfo`的`EndUnscaledTime`
            public float EndUnscaledTime; // 结束时刻（秒）。 赋值为Update时取到的`Time.unscaledTime`（等于已完成的最后一帧的结束时刻）
            public float UnscaledTime => EndUnscaledTime; // 赋值为Update时取到的`Time.unscaledTime`（等于已完成的最后一帧的结束时刻）
            public float DeltaSec; // 帧耗时（秒），使用Update时取到的`Time.unscaledDeltaTime`（等于已完成的最后一帧的耗时）
            public int Frame; // 帧序号

            /// 取初始化用的初始值
            public static FrameInfo GetInit()
            {
                var realtime = Time.realtimeSinceStartup;
                var endGameTime = Time.unscaledTime;
                var frameEndTime = DateTime.Now - TimeSpan.FromSeconds(realtime - endGameTime);
                var basePrev = new FrameInfo
                {
                    RealTime = realtime,
                    EndTime = frameEndTime,
                    EndUnscaledTime = endGameTime,
                };
                return GetLatest(basePrev);
            }

            /// 取已完成的最后一帧的数据
            /// 依赖参数`prevFrame`，因为新一帧的范围起始时间，由前一帧数据才能取到
            public static FrameInfo GetLatest(FrameInfo prevFrame)
            {
                var realtime = Time.realtimeSinceStartup;
                var dateTime = DateTime.Now;
                var endGameTime = Time.unscaledTime;
                var frameEndTime = dateTime - TimeSpan.FromSeconds(realtime - endGameTime);
                var frameInfo = new FrameInfo
                {
                    RealTime = realtime,
                    DeltaRealTime = realtime - prevFrame.RealTime,
                    DateTime = dateTime,
                    BeginTime = prevFrame.EndTime,
                    EndTime = frameEndTime,
                    BeginUnscaledTime = prevFrame.EndUnscaledTime,
                    EndUnscaledTime = endGameTime,
                    DeltaSec = Time.unscaledDeltaTime,
                    Frame = Time.frameCount
                };
                return frameInfo;
            }
        }

        private IntervalInfo _interval;
        private FrameInfo _latestFrame;

        // 耗时阈值（毫秒）
        private readonly int _topItemThresholdMs = 100;
        private const int MaxTopFrames = 3;
        private const int ValidMinimumFrameCount = 10;

        // 存储时常内耗时最长且超过阈值的帧
        private FrameInfo[] _topFrames = new FrameInfo[MaxTopFrames];
        // 超过阈值的总帧数
        private int _topCount;

        public FpsUpdatable Init()
        {
            ResetTopFrames();
            _latestFrame = FrameInfo.GetInit();
            return this;
        }

        public void Update()
        {
            if (Time.frameCount < StartFrame)
                return;

            // 更新 frame 数据
            var prevFrame = _latestFrame;
            var frame = FrameInfo.GetLatest(prevFrame);
            _latestFrame = frame;
            if (_hasValidFrameCount < ValidMinimumFrameCount)
                _hasValidFrameCount++;

            // 检测长时间无更新（卡帧）
            var deltaRealTime = frame.DeltaRealTime;
            if (deltaRealTime > FreezeDetectionThreshold)
            {
                Debug.LogWarning($"[FPS] 检测到游戏严重卡顿: {deltaRealTime:F1}s (at {TimeRangeStr(prevFrame.DateTime, frame.DateTime, frame.Frame)})");
            }

            UpdateIntervalFrame(frame);
            UpdateCurrentFps(frame.DeltaSec, _interval);

            var hasValidData = _hasValidFrameCount >= ValidMinimumFrameCount;
            if (!hasValidData)
                return;

            // 检查 interval 区段过了多久，是否要输出
            var intervalElapsed = frame.UnscaledTime - _interval.BeginUnscaledTime;
            if (intervalElapsed > OutputIntervalThreshold)
            {
                OutputInterval();
                ResetTopFrames(); // 重置记录
            }
        }

        // 更新 interval 数据
        private void UpdateIntervalFrame(FrameInfo latestFrame)
        {
            var deltaTime = latestFrame.DeltaSec;
            if (_interval.IsFinish)
                _interval = IntervalInfo.BeginWith(latestFrame);
            _interval.DurationSec += deltaTime;
            _interval.FrameCount++;
            _interval.FPS = CalcFps(_interval.DurationSec, _interval.FrameCount);

            // 更新 interval 内的Top列表
            if (deltaTime * 1000 > _topItemThresholdMs)
            {
                AddTopFrame(_latestFrame);
            }
        }

        // 计算当前fps
        private void UpdateCurrentFps(float deltaTime, IntervalInfo interval)
        {
            var hasValidData = _hasValidFrameCount >= ValidMinimumFrameCount;
            if (hasValidData)
            {
                const float historyFactor = 5f / 10f; // 历史权重系数
                FrameTime = FrameTime * historyFactor + deltaTime * (1 - historyFactor); // 平均化处理
                CurrentFPS = CalcFps(FrameTime, 1);
            }
            else if (interval.FrameCount > 0)
            {
                // 数据量太少时，用已累计的
                FrameTime = interval.AvgFrameTime;
                CurrentFPS = interval.FPS;
            }
            else
            {
                // 数据量0，只用最新值
                FrameTime = deltaTime;
                CurrentFPS = CalcFps(FrameTime, 1);
            }
        }

        private string GameTimeStr() => GameTimeStr(DateTime.Now, Time.frameCount);
        private string GameTimeStr(DateTime dateTime, int frame) => $"{dateTime:HH:mm:ss.fff} frame: {frame}f";
        private string TimeRangeStr(DateTime begin, DateTime end, int frame) => $"{begin:HH:mm:ss.fff} ~ {end:HH:mm:ss.fff} frame: {frame}f";
        private string TimeRangeStr(FrameInfo begin, FrameInfo end) => TimeRangeStr(begin.BeginTime, end.EndTime, begin.Frame, end.Frame);

        private string TimeRangeStr(DateTime begin, DateTime end, int frame1, int frame2) =>
            $"{begin:HH:mm:ss.fff} ~ {end:HH:mm:ss.fff} frame: {frame1}f ~ {frame2}f";


        private static float CalcFps(float time, int frameCount) => time > 0 ? frameCount / time : 199.9f;

        // 更新 interval 内的Top列表
        private void AddTopFrame(FrameInfo frameInfo)
        {
            _topCount++;

            // 维护前三帧（按耗时降序）
            if (frameInfo.DeltaSec > _topFrames[0].DeltaSec)
            {
                _topFrames[2] = _topFrames[1];
                _topFrames[1] = _topFrames[0];
                _topFrames[0] = frameInfo;
            }
            else if (frameInfo.DeltaSec > _topFrames[1].DeltaSec)
            {
                _topFrames[2] = _topFrames[1];
                _topFrames[1] = frameInfo;
            }
            else if (frameInfo.DeltaSec > _topFrames[2].DeltaSec)
            {
                _topFrames[2] = frameInfo;
            }
        }

        private void ResetTopFrames()
        {
            _topFrames[0] = default;
            _topFrames[1] = default;
            _topFrames[2] = default;
            _topCount = 0;
        }

        private void OutputInterval()
        {
            var targetFps = Application.targetFrameRate;
            var duration = _interval.DurationSec;
            var count = _interval.FrameCount;
            var avgFPS = _interval.FPS = CalcFps(duration, count);
            _interval.EndWith(_latestFrame);
            _interval.IsFinish = true;
            Debug.Log($"[FPS] 当前fps: {CurrentFPS:F1}, {FrameTime * 1000f:F0}ms. targetFps: {targetFps}. " +
                         IntervalLogMsg(_interval));
            if (_topCount > 0)
            {
                var infos = _topFrames.Where(s => s.DeltaSec > 0).Select(TopFrameItemMsg);
                var topFramesInfo = string.Join(", \n", infos);
                // 输出日志（包含超过阈值的帧数量和前三帧信息）
                Debug.Log($"[FPS] 帧耗时过高: {_topCount}次 (>{_topItemThresholdMs}ms), Top耗时 {topFramesInfo}");
            }
        }

        private string IntervalLogMsg(IntervalInfo v)
        {
            return $"最近区段 {v.DurationSec:F1}s {v.FrameCount}f 平均fps: {v.FPS:F1} \n(at {TimeRangeStr(v.BeginFrame, v.EndFrame)})";
        }

        private string TopFrameItemMsg(FrameInfo s, int i)
        {
            return $"#{i + 1}: {s.DeltaSec * 1000:0}ms (at {TimeRangeStr(s.BeginTime, s.EndTime, s.Frame)})";
        }
    }
}