using System;
using UnityEngine;

namespace ByteDance.CloudSync.CloudGameAndroid
{
    internal static class LogUtils
    {
        public static void LogInfo(string tag, string msg)
        {
            Debug.Log($"CloudSync - {tag} - Info:\n{msg}");
        }
        public static void LogWarning(string tag, string msg)
        {
            Debug.LogWarning($"CloudSync - {tag} - Warning:\n{msg}");
        }
        public static void LogError(string tag, string msg)
        {
            Debug.LogError($"CloudSync - {tag} - Error:\n{msg}");
        }

        public static T WrapExceptionLog<T>(Func<T> func, string tag)
        {
            try
            {
                return func.Invoke();
            }
            catch (Exception e)
            {
                LogError(tag, e.ToString());
                return default;
            }
        }
        public static void WrapExceptionLog(Action act, string tag)
        {
            try
            {
                act.Invoke();
            }
            catch (Exception e)
            {
                LogError(tag, e.ToString());
            }
        }
    }
}
