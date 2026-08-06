using UnityEngine;

namespace GameMain
{
    public static partial class DataManager
    {
        private static void LogKV(string str)
        {
#if UNITY_EDITOR
            Log($"调试key : {str}");
#endif
        }

        private static void Log(string str)
        {
#if UNITY_EDITOR
            Debug.Log(str, Color.cyan);
#else
            Debug.Log(str);
#endif
        }
        
    }
}