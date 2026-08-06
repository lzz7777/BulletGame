// Copyright (c) Bytedance. All rights reserved.
// Description:

namespace Douyin.LiveOpenSDK.Utilities
{
    internal static class LogLevelExtension
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
}