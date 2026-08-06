namespace ByteDance.CloudSync
{
    public static class CGLogger
    {
        public delegate void LogMethod(string tag, string msg);

        private static readonly SdkDebugLogger Debug = new(Tag);
        private static LogMethod _log = DefaultLog;
        private static LogMethod _logDebug = DefaultLogDebug;
        private static LogMethod _warning = DefaultWarning;
        private static LogMethod _error = DefaultError;

        public static void SetLogger(LogMethod log, LogMethod warning, LogMethod error)
        {
            _log = log;
            _logDebug = log;
            _warning = warning;
            _error = error;
        }

        private const string Tag = "CloudSync";

        public static void Log(string tag, string log)
        {
            _log?.Invoke(tag, log);
        }

        public static void LogDebug(string log)
        {
            _logDebug?.Invoke(Tag, log);
        }

        public static void Log(string log)
        {
            _log?.Invoke(Tag, log);
        }

        public static void LogWarning(string log)
        {
            _warning?.Invoke(Tag, log);
        }

        public static void LogError(string log)
        {
            _error?.Invoke(Tag, log);
        }

        /// <summary>
        /// 错误或警告日志
        /// </summary>
        /// <param name="log">日志信息</param>
        /// <param name="isError">是否按Error，否则按Warning</param>
        public static void LogErrorWarning(string log, bool isError)
        {
            if (isError)
                LogError(log);
            else
                LogWarning(log);
        }

        private static void DefaultLogDebug(string tag, string msg)
        {
#if CLOUDGAME_ENABLE_STARKLOGS
            StarkLogs.StarkLog.LogDebug(tag, msg);
#else
            Debug.LogDebug(msg);
#endif
        }

        private static void DefaultLog(string tag, string msg)
        {
#if CLOUDGAME_ENABLE_STARKLOGS
            StarkLogs.StarkLog.Log(tag, msg);
#else
            Debug.Log(msg);
#endif
        }

        private static void DefaultWarning(string tag, string msg)
        {
#if CLOUDGAME_ENABLE_STARKLOGS
            StarkLogs.StarkLog.LogWarning(tag, msg);
#else
            Debug.LogWarning(msg);
#endif
        }

        private static void DefaultError(string tag, string msg)
        {
#if CLOUDGAME_ENABLE_STARKLOGS
            StarkLogs.StarkLog.LogError(tag, msg);
#else
            Debug.LogError(msg);
#endif
        }
    }
}
