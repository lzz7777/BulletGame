// Copyright (c) Bytedance. All rights reserved.

using UnityEngine.Scripting;

namespace ByteDance.LiveOpenSdk.Perf
{
    /// <summary>
    /// 性能压测记录器
    /// </summary>
    [Preserve]
    public static class PerfTestRecorder
    {
        /// <summary>
        /// 启动录制。 会启动性能数据录制、并发送给性能观察工具。
        /// </summary>
        [Preserve]
        public static void Start()
        {
            PerfReporter.StartAndSend();
        }

        /// <summary>
        /// 停止录制
        /// </summary>
        [Preserve]
        public static void Stop()
        {
            PerfReporter.Stop();
        }

        /// <summary>
        /// 暂停录制
        /// </summary>
        [Preserve]
        public static void Pause()
        {
            PerfReporter.Pause();
        }

        /// <summary>
        /// 恢复录制
        /// </summary>
        [Preserve]
        public static void Resume()
        {
            PerfReporter.Resume();
        }
    }
}