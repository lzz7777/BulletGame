using System;
using UnityEngine;

namespace ByteDance.CloudSync.Match
{
    internal class DebugLogger
    {
        public void Log(object message) => Debug.Log(NowTimePrefix + message);
        public void LogWarning(object message) => Debug.LogWarning(NowTimePrefix + message);
        public void LogError(object message) => Debug.LogError(NowTimePrefix + message);
        public void LogException(Exception exception) => Debug.LogError(NowTimePrefix + exception);

        public static string NowTime => DateTime.Now.ToString("HH:mm:ss.fff");
        private static string NowTimePrefix => DateTime.Now.ToString("HH:mm:ss.fff ");
    }
}