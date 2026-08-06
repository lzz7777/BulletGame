// Copyright@www.bytedance.com
// Author: DONEY Dong
// Date: 2024/04/28
// Description:

using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ByteDance.CloudSync
{
    public enum LogLevel
    {
        Debug = 0,
        Info,
        Warning,
        Error,
        Exception,
    }

    public static class LogLevelExtension
    {
        public static UnityEngine.LogType ToLogType(this LogLevel level)
        {
            return level switch
            {
                LogLevel.Debug => UnityEngine.LogType.Log,
                LogLevel.Info => UnityEngine.LogType.Log,
                LogLevel.Warning => UnityEngine.LogType.Warning,
                LogLevel.Error => UnityEngine.LogType.Error,
                LogLevel.Exception => UnityEngine.LogType.Exception,
                _ => UnityEngine.LogType.Log
            };
        }
    }

    public interface ILogger
    {
        void LogDebug(string tag, string message);
        void Log(string tag, string message);
        void LogWarning(string tag, string message);
        void LogError(string tag, string message);
        void LogException(Exception exception);
    }

    internal class DefaultLogger : ILogger
    {
        public void LogDebug(string tag, string message) => Debug.Log(message);

        public void Log(string tag, string message) => Debug.Log(message);

        public void LogWarning(string tag, string message) => Debug.LogWarning(message);

        public void LogError(string tag, string message) => Debug.LogError(message);

        public void LogException(Exception exception) => Debug.LogException(exception);
    }

#if CLOUDGAME_ENABLE_STARKLOGS
    internal class StarkLogLogger : ILogger
    {
        public void LogDebug(string tag, string message) => StarkLogs.StarkLog.LogDebug(tag, message);

        public void Log(string tag, string message) => StarkLogs.StarkLog.Log(tag, message);

        public void LogWarning(string tag, string message) => StarkLogs.StarkLog.LogWarning(tag, message);

        public void LogError(string tag, string message) => StarkLogs.StarkLog.LogError(tag, message);

        public void LogException(Exception exception) => StarkLogs.StarkLog.LogException(exception);
    }
#endif

    public class SdkDebugLogger : ILogger
    {
        private string _tag;
        private string _tagText;
        private ILogger _logger;

        public bool IsMsgTagEnabled = true;
        public bool IsMsgTimeEnabled = false;
        public bool IsMsgLevelEnabled = true;

        static SdkDebugLogger()
        {
            InitApplicationFlags();
        }

        public SdkDebugLogger(string tag)
        {
            Tag = tag ?? "";
#if CLOUDGAME_ENABLE_STARKLOGS
            _logger = new StarkLogLogger();
#else
            _logger = new DefaultLogger();
            IsMsgTimeEnabled = !Application.isEditor;
#endif
        }

        public string Tag
        {
            get => _tag;
            set
            {
                _tag = value;
                _tagText = TagText(_tag);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogDebug(string msg) => _logger.LogDebug(Tag, MakeLogMsg(LogLevel.Debug, msg));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log(string msg) => _logger.Log(Tag, MakeLogMsg(LogLevel.Info, msg));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogWarning(string msg) => _logger.LogWarning(Tag, MakeLogMsg(LogLevel.Warning, msg));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogError(string msg) => _logger.LogError(Tag, MakeLogMsg(LogLevel.Error, msg));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogException(Exception e) => _logger.LogException(WrapException(e));

        void ILogger.LogDebug(string tag, string message) => LogDebug(message);
        void ILogger.Log(string tag, string message) => Log(message);
        void ILogger.LogWarning(string tag, string message) => LogWarning(message);
        void ILogger.LogError(string tag, string message) => LogError(message);

        public void Assert(bool condition, string msg = "Assertion failed")
        {
            if (!condition)
                _logger.LogError(Tag, MakeLogMsg(LogLevel.Error, msg));
        }

        private bool IsMakeLogFormats
        {
            get
            {
#if CLOUDGAME_ENABLE_STARKLOGS
                return false;
#else
                return true;
#endif
            }
        }

        internal string MakeLogMsg(LogLevel level, string msg)
        {
            if (!IsMakeLogFormats)
                return msg;
            var timeLevel = TimeLevelText(level);
            var tagText = IsMsgTagEnabled ? _tagText : string.Empty;
            var log = timeLevel + tagText + msg;
            return log;
        }

        internal Exception WrapException(Exception e)
        {
            return e;
        }

        public string ColorText(string s, LogLevel level)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;
            if (!IsEditorClient)
                return s;
            switch (level)
            {
                case LogLevel.Info:
                    return InfoColorText(s);
                case LogLevel.Warning:
                    return WarningColorText(s);
                case LogLevel.Error:
                case LogLevel.Exception:
                    return ErrorColorText(s);
                default:
                    return s;
            }
        }

        public string TagText(string tag) =>
            IsEditorClient ? "<color=#80a0ff>[" + tag + "]</color> " : "[" + tag + "] ";

        public string TimeLevelText(LogLevel level) =>
            IsMsgTimeEnabled || IsMsgLevelEnabled ? ColorText(TimePrefix() + LevelPrefix(level), level) : string.Empty;

        public string TimePrefix() => IsMsgTimeEnabled ? TimeUtil.NowTime + " " : string.Empty;

        public string LevelPrefix(LogLevel level)
        {
            if (!IsMsgLevelEnabled)
                return string.Empty;
            return level switch
            {
                LogLevel.Debug => "D/",
                LogLevel.Info => "I/",
                LogLevel.Warning => "W/",
                LogLevel.Error or LogLevel.Exception => "E/",
                _ => string.Empty
            };
        }

        private static void InitApplicationFlags()
        {
            _isEditorFlag = Application.isEditor ? 1 : 0;
        }

        internal static bool IsEditor => _isEditorFlag == 1;

        private static int _isEditorFlag = 0;

        internal static bool IsServer
        {
#if UNITY_SERVER
            get { return true; }
#else
            get { return false; }
#endif
        }

        internal static bool IsEditorClient => IsEditor && !IsServer;

        public bool isDebugBuild => UnityEngine.Debug.isDebugBuild;

        public string InfoColorText(string s) => "<color=#2DBF2D>" + s + "</color>";
        public string WarningColorText(string s) => "<color=#BFB42F>" + s + "</color>";
        public string ErrorColorText(string s) => "<color=#BF2D2D>" + s + "</color>";

        public string BoolToTF(bool value) => value ? "T" : "F";
        public string BoolTo01(bool value) => value ? "1" : "0";
    }
}