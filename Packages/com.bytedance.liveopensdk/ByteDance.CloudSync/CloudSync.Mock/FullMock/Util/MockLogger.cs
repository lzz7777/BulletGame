using UnityEngine;

namespace ByteDance.CloudSync.Mock
{
    internal interface IMockLogger
    {
        public static IMockLogger GetLogger(string tag)
        {
            return new Logger(tag);
        }

        void Log(string message);
        void LogWarning(string message);
        void LogError(string message);
        void Assert(bool condition, string msg = "Assertion failed");
    }

    internal class Logger : SdkDebugLogger, IMockLogger
    {
        public Logger(string tag) : base(tag)
        {
        }
    }
}