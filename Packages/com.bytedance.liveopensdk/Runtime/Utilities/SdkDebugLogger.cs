// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using System.Runtime.CompilerServices;

namespace Douyin.LiveOpenSDK.Utilities
{
    public enum LogLevel
    {
        Debug = 0,
        Info,
        Warning,
        Error,
        Exception,
    }

    public class SdkDebugLogger
    {
        private string _tag;
        private string _tagText;

        /// <summary>
        /// 监听log，不保证main thread
        /// </summary>
        /// <remarks>监听后，注意移除</remarks>
        internal event Action<LogLevel, string> onDebugLogAction;

        public bool IsTimeEnabled = true;
        public bool IsDateEnabled = false;

        static SdkDebugLogger()
        {
            InitApplicationFlags();
        }

        public SdkDebugLogger(string tag)
        {
            Tag = tag ?? "";
            IsTimeEnabled = !UnityEngine.Application.isEditor;
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
        internal void PrintLog(string tag, string msg) => UnityEngine.Debug.Log(MakeLogMsg(LogLevel.Debug, tag, msg));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if LIVEOPENSDK_ENABLE_STARKLOGS
        public void LogDebug(string msg) => StarkLogs.StarkLog.LogDebug(MakeLogMsg(LogLevel.Debug, msg));
#else
        public void LogDebug(string msg) => UnityEngine.Debug.Log(MakeLogMsg(LogLevel.Debug, msg));
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log(string msg) => UnityEngine.Debug.Log(MakeLogMsg(LogLevel.Info, msg));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogWarning(string msg) => UnityEngine.Debug.LogWarning(MakeLogMsg(LogLevel.Warning, msg));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogError(string msg) => UnityEngine.Debug.LogError(MakeLogMsg(LogLevel.Error, msg));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogException(Exception e) => UnityEngine.Debug.LogException(WrapException(e));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void LogByLevel(LogLevel level, string msg) => UnityEngine.Debug.unityLogger.Log(level.ToLogType(), MakeLogMsg(level, msg));

        public void Assert(bool condition, string msg = "Assertion failed")
        {
            if (!condition)
                UnityEngine.Debug.LogError(MakeLogMsg(LogLevel.Error, msg));
        }

        internal string MakeLogMsg(LogLevel level, string tag, string msg)
        {
            var timeText = ColorTimeText(level);
            var log = timeText + "[" + tag + "] " + msg;
            onDebugLogAction?.Invoke(level, log);
            return log;
        }

        internal string MakeLogMsg(LogLevel level, string msg)
        {
            var timeText = ColorTimeText(level);
            var log = timeText + _tagText + msg;
            onDebugLogAction?.Invoke(level, log);
            return log;
        }

        private string TimeText => IsDateEnabled ? TimeUtil.NowDateTime : (IsTimeEnabled ? TimeUtil.NowTime : string.Empty);
        private string ColorTimeText(LogLevel level) => ColorText(TimeText, level) + (IsDateEnabled || IsTimeEnabled ? " " : string.Empty);

        internal Exception WrapException(Exception e)
        {
            var log = e.ToString();
            onDebugLogAction?.Invoke(LogLevel.Exception, log);
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

        private static void InitApplicationFlags()
        {
            _isEditorFlag = UnityEngine.Application.isEditor ? 1 : 0;
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