// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using ByteDance.Live.Foundation.Logging;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ByteDance.LiveOpenSdk.Runtime.Utilities
{
    /// <summary>
    /// 将 <see cref="LogItem"/> 渲染并发送到 Unity 控制台的工具类。
    /// </summary>
    public static class SdkUnityLogger
    {
        private const string ColorTag = "#80a0ff";

        /// <summary>
        /// 用于输出生成的富文本日志。
        /// </summary>
        public static event Action<Severity, string> OnRichLog;

        /// <summary>
        /// 用于输入 LogItem。
        /// </summary>
        public static readonly LogSource LogSink = new LogSource();
        private static Severity _minSeverity = Severity.Warning;
        public static Severity MinSeverity
        {
            get => _minSeverity;
            set
            {
                _minSeverity = value;
                Debug.Log($"Log filter changed to {value}");
            }
        }
        static SdkUnityLogger()
        {
            LogSink.OnLog -= WriteLog;
            LogSink.OnLog += WriteLog;
        }

        private static void WriteLog(LogItem item)
        {
            if (!item.Severity.IsAtLeast(MinSeverity)) return;
            var timestamp = item.Timestamp;
            var severity = item.Severity;
            var tag = item.Tag;
            var message = item.Message;
            var logType = severity switch
            {
                Severity.Verbose => LogType.Log,
                Severity.Debug => LogType.Log,
                Severity.Info => LogType.Log,
                Severity.Warning => LogType.Warning,
                Severity.Error => LogType.Error,
                _ => LogType.Error
            };
            var logStr =
                $"{MakeColor($"[{GetInitial(severity)}]", severity)} {MakeColor($"[{tag}]", ColorTag)} {MakeColor(message, severity)}";

            var context = LiveOpenSdk.Instance.DefaultSynchronizationContext;
            if (context != null)
            {
                context.Post(PerformLog, null);
            }
            else
            {
                PerformLog(null);
            }

            return;

            void PerformLog(object _)
            {
                Debug.unityLogger.Log(logType, logStr);
                OnRichLog?.Invoke(severity, logStr);
            }
        }

        private static string GetInitial(Severity severity)
        {
            return severity switch
            {
                Severity.Verbose => "V",
                Severity.Debug => "D",
                Severity.Info => "I",
                Severity.Warning => "W",
                Severity.Error => "E",
                _ => severity.ToString()
            };
        }

        private static string GetColor(Severity severity)
        {
            return severity switch
            {
                Severity.Verbose => "#A6A6A6",
                Severity.Debug => "#BFBFBF",
                Severity.Info => "#99BF99",
                Severity.Warning => "#BfB42F",
                Severity.Error => "#BF2D2D",
                _ => "#BF2D2D"
            };
        }

        private static string MakeColor(string text, string hexColor)
        {
            return $"<color={hexColor}>{text}</color>";
        }

        private static string MakeColor(string text, Severity severity)
        {
            return MakeColor(text, GetColor(severity));
        }
    }
}