using System;

namespace ByteDance.LiveOpenSdk.Perf
{
    internal interface IPerfReporter
    {
        /// <summary>
        /// 启动性能数据录制
        /// </summary>
        void Start();

        /// <summary>
        /// 停止性能数据录制
        /// </summary>
        void Stop();

        /// <summary>
        /// 暂停性能数据录制
        /// </summary>
        void Pause();

        /// <summary>
        /// 恢复性能数据录制
        /// </summary>
        void Resume();

        /// <summary>
        /// 添加额外的性能数据处理器
        /// </summary>
        T AddListener<T>() where T : IPerfListener, new();
    }

    internal static class PerfReporter
    {
        private static IPerfReporter _instance;

        internal static IPerfReporter Instance => _instance ??= new PerfReporterImpl();

        /// <summary>
        /// 启动性能数据录制
        /// </summary>
        public static void Start()
        {
            Instance.Start();
        }

        /// <summary>
        /// 启动并发送。 会启动性能数据录制、并发送给性能观察工具。
        /// </summary>
        public static void StartAndSend()
        {
            Instance.AddListener<PerfToolServer>();
            Instance.Start();
        }

        /// <summary>
        /// 停止性能数据录制
        /// </summary>
        public static void Stop()
        {
            Instance.Stop();
        }

        /// <summary>
        /// 暂停性能数据录制
        /// </summary>
        public static void Pause()
        {
            Instance.Pause();
        }

        /// <summary>
        /// 恢复性能数据录制
        /// </summary>
        public static void Resume()
        {
            Instance.Resume();
        }

        public static long GetCurMemory()
        {
            var reporter = Instance as PerfReporterImpl;
            return reporter.GetMemory();
        }
        public static IntPtr GetCurrentProcess()
        {
            var reporter = Instance as PerfReporterImpl;
            return reporter.GetCurrentProcess();
        }
    }
}