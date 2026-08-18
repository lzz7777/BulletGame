/*
 * Headless Builder Exporter(覆盖Headless Builder中Debug)
 * (c) Salty Devs, 2022
 *
 * Please do not publish or pirate this code.
 * We worked really hard to make it.
 *
 */


#if (HEADLESS && HEADLESS_STRIPLOGGING)
#undef ALLOW_LOGGING_INTERNAL
#define BLOCK_LOGGING_INTERNAL
#else
#define ALLOW_LOGGING_INTERNAL
#undef BLOCK_LOGGING_INTERNAL
#endif

using System;
using UnityEngine;
using Object = UnityEngine.Object;

public enum ShowLogLevel
{
    Always = 0,
    Debug,
    Warn,
    Error,
    Never
}

public static class Debug
{
    /// <summary>
    /// 日志等级
    /// </summary>
    public static ShowLogLevel logLevel = ShowLogLevel.Debug;

    public static bool isDebugBuild => UnityEngine.Debug.isDebugBuild;
    public static string getTime => DateTimeHelper.NowServer.ToString("s");

    public static string bindTime(object message)
    {
        return $"{getTime} - {message}";
    }

    public static bool developerConsoleVisible
    {
        get => UnityEngine.Debug.developerConsoleVisible;
        set => UnityEngine.Debug.developerConsoleVisible = value;
    }

#if UNITY_2017_1_OR_NEWER
    public static ILogger unityLogger => UnityEngine.Debug.unityLogger;
#endif

#if BLOCK_LOGGING_INTERNAL
    [System.Diagnostics.Conditional("ALLOW_LOGGING_INTERNAL")]
#endif
    public static void Assert(bool condition)
    {
        if (logLevel > ShowLogLevel.Error) return;
        UnityEngine.Debug.Assert(condition);
    }

#if BLOCK_LOGGING_INTERNAL
    [System.Diagnostics.Conditional("ALLOW_LOGGING_INTERNAL")]
#endif
    public static void Assert(bool condition, Object context)
    {
        if (logLevel > ShowLogLevel.Error) return;
        UnityEngine.Debug.Assert(condition, context);
    }

#if BLOCK_LOGGING_INTERNAL
    [System.Diagnostics.Conditional("ALLOW_LOGGING_INTERNAL")]
#endif
    public static void Assert(bool condition, object message)
    {
        if (logLevel > ShowLogLevel.Error) return;
        UnityEngine.Debug.Assert(condition, message);
    }

#if BLOCK_LOGGING_INTERNAL
    [System.Diagnostics.Conditional("ALLOW_LOGGING_INTERNAL")]
#endif
    public static void Assert(bool condition, object message, Object context)
    {
        if (logLevel > ShowLogLevel.Error) return;
        UnityEngine.Debug.Assert(condition, message, context);
    }


#if BLOCK_LOGGING_INTERNAL
    [System.Diagnostics.Conditional("ALLOW_LOGGING_INTERNAL")]
#endif
    public static void AssertFormat(bool condition, string format, params object[] args)
    {
        if (logLevel > ShowLogLevel.Error) return;
        UnityEngine.Debug.AssertFormat(condition, format, args);
    }

#if BLOCK_LOGGING_INTERNAL
    [System.Diagnostics.Conditional("ALLOW_LOGGING_INTERNAL")]
#endif
    public static void AssertFormat(bool condition, Object context, string format, params object[] args)
    {
        if (logLevel > ShowLogLevel.Error) return;
        UnityEngine.Debug.AssertFormat(condition, context, format, args);
    }


#if BLOCK_LOGGING_INTERNAL
    [System.Diagnostics.Conditional("ALLOW_LOGGING_INTERNAL")]
#endif
    public static void Break()
    {
        UnityEngine.Debug.Break();
    }


#if BLOCK_LOGGING_INTERNAL
    [System.Diagnostics.Conditional("ALLOW_LOGGING_INTERNAL")]
#endif
    public static void ClearDeveloperConsole()
    {
        UnityEngine.Debug.ClearDeveloperConsole();
    }


#if BLOCK_LOGGING_INTERNAL
    [System.Diagnostics.Conditional("ALLOW_LOGGING_INTERNAL")]
#endif
    public static void DrawLine(Vector3 start, Vector3 end, Color color = default, float duration = 0.0f,
        bool depthTest = true)
    {
        UnityEngine.Debug.DrawLine(start, end, color, duration, depthTest);
    }


#if BLOCK_LOGGING_INTERNAL
    [System.Diagnostics.Conditional("ALLOW_LOGGING_INTERNAL")]
#endif
    public static void DrawRay(Vector3 start, Vector3 dir, Color color = default, float duration = 0.0f,
        bool depthTest = true)
    {
        UnityEngine.Debug.DrawRay(start, dir, color, duration, depthTest);
    }
#if BLOCK_LOGGING_INTERNAL
    [System.Diagnostics.Conditional("ALLOW_LOGGING_INTERNAL")]
#endif
    public static void Log(string message)
    {
        if (logLevel > ShowLogLevel.Debug) return;
        UnityEngine.Debug.Log(message);
    }
#if BLOCK_LOGGING_INTERNAL
    [System.Diagnostics.Conditional("ALLOW_LOGGING_INTERNAL")]
#endif
    public static void Log(object message)
    {
        if (logLevel > ShowLogLevel.Debug) return;
        UnityEngine.Debug.Log(message);
    }
#if BLOCK_LOGGING_INTERNAL
    [System.Diagnostics.Conditional("ALLOW_LOGGING_INTERNAL")]
#endif
    public static void Log(object message, Color color)
    {
        if (logLevel > ShowLogLevel.Debug) return;

        UnityEngine.Debug.Log($"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{message}</color>");
    }

#if BLOCK_LOGGING_INTERNAL
    [System.Diagnostics.Conditional("ALLOW_LOGGING_INTERNAL")]
#endif
    public static void Log(object message, Object context)
    {
        if (logLevel > ShowLogLevel.Debug) return;
        UnityEngine.Debug.Log(message, context);
    }


#if BLOCK_LOGGING_INTERNAL
    [System.Diagnostics.Conditional("ALLOW_LOGGING_INTERNAL")]
#endif
    public static void LogAssertion(object message)
    {
        if (logLevel > ShowLogLevel.Debug) return;
        UnityEngine.Debug.LogAssertion(message);
    }

#if BLOCK_LOGGING_INTERNAL
    [System.Diagnostics.Conditional("ALLOW_LOGGING_INTERNAL")]
#endif
    public static void LogAssertion(object message, Object context)
    {
        if (logLevel > ShowLogLevel.Debug) return;
        UnityEngine.Debug.LogAssertion(message, context);
    }

#if BLOCK_LOGGING_INTERNAL
    [System.Diagnostics.Conditional("ALLOW_LOGGING_INTERNAL")]
#endif
    public static void LogError(string message)
    {
        // if (logLevel > ShowLogLevel.Error) return;
        UnityEngine.Debug.LogError(message);
    }

#if BLOCK_LOGGING_INTERNAL
    [System.Diagnostics.Conditional("ALLOW_LOGGING_INTERNAL")]
#endif
    public static void LogError(object message)
    {
        if (logLevel > ShowLogLevel.Error) return;
        UnityEngine.Debug.LogError(message);
    }

#if BLOCK_LOGGING_INTERNAL
    [System.Diagnostics.Conditional("ALLOW_LOGGING_INTERNAL")]
#endif
    public static void LogError(object message, Object context)
    {
        if (logLevel > ShowLogLevel.Error) return;
        UnityEngine.Debug.LogError(message, context);
    }


#if BLOCK_LOGGING_INTERNAL
    [System.Diagnostics.Conditional("ALLOW_LOGGING_INTERNAL")]
#endif
    public static void LogErrorFormat(string format, params object[] args)
    {
        if (logLevel > ShowLogLevel.Error) return;
        UnityEngine.Debug.LogErrorFormat(format, args);
    }

#if BLOCK_LOGGING_INTERNAL
    [System.Diagnostics.Conditional("ALLOW_LOGGING_INTERNAL")]
#endif
    public static void LogErrorFormat(Object context, string format, params object[] args)
    {
        if (logLevel > ShowLogLevel.Error) return;
        UnityEngine.Debug.LogErrorFormat(context, format, args);
    }


#if BLOCK_LOGGING_INTERNAL
    [System.Diagnostics.Conditional("ALLOW_LOGGING_INTERNAL")]
#endif
    public static void LogException(Exception exception)
    {
        if (logLevel > ShowLogLevel.Debug) return;
        UnityEngine.Debug.LogException(exception);
    }

#if BLOCK_LOGGING_INTERNAL
    [System.Diagnostics.Conditional("ALLOW_LOGGING_INTERNAL")]
#endif
    public static void LogException(Exception exception, Object context)
    {
        if (logLevel > ShowLogLevel.Debug) return;
        UnityEngine.Debug.LogException(exception, context);
    }


#if BLOCK_LOGGING_INTERNAL
    [System.Diagnostics.Conditional("ALLOW_LOGGING_INTERNAL")]
#endif
    public static void LogFormat(string format, params object[] args)
    {
        if (logLevel > ShowLogLevel.Debug) return;
        UnityEngine.Debug.LogFormat(format, args);
    }

#if BLOCK_LOGGING_INTERNAL
    [System.Diagnostics.Conditional("ALLOW_LOGGING_INTERNAL")]
#endif
    public static void LogFormat(Object context, string format, params object[] args)
    {
        if (logLevel > ShowLogLevel.Debug) return;
        UnityEngine.Debug.LogFormat(context, format, args);
    }

#if UNITY_2019_1_OR_NEWER
#if BLOCK_LOGGING_INTERNAL
    [System.Diagnostics.Conditional("ALLOW_LOGGING_INTERNAL")]
#endif
    public static void LogFormat(LogType logType, LogOption logOptions, Object context, string format,
        params object[] args)
    {
        if (logLevel > ShowLogLevel.Debug) return;
        UnityEngine.Debug.LogFormat(logType, logOptions, context, format, args);
    }
#endif

#if BLOCK_LOGGING_INTERNAL
    [System.Diagnostics.Conditional("ALLOW_LOGGING_INTERNAL")]
#endif
    public static void LogWarning(string message)
    {
        UnityEngine.Debug.LogWarning(message);
    }

#if BLOCK_LOGGING_INTERNAL
    [System.Diagnostics.Conditional("ALLOW_LOGGING_INTERNAL")]
#endif
    public static void LogWarning(object message)
    {
        if (logLevel > ShowLogLevel.Warn) return;
        UnityEngine.Debug.LogWarning(message);
    }

#if BLOCK_LOGGING_INTERNAL
    [System.Diagnostics.Conditional("ALLOW_LOGGING_INTERNAL")]
#endif
    public static void LogWarning(object message, Object context)
    {
        if (logLevel > ShowLogLevel.Warn) return;
        UnityEngine.Debug.LogWarning(message, context);
    }


#if BLOCK_LOGGING_INTERNAL
    [System.Diagnostics.Conditional("ALLOW_LOGGING_INTERNAL")]
#endif
    public static void LogWarningFormat(string format, params object[] args)
    {
        if (logLevel > ShowLogLevel.Warn) return;
        UnityEngine.Debug.LogWarningFormat(format, args);
    }

#if BLOCK_LOGGING_INTERNAL
    [System.Diagnostics.Conditional("ALLOW_LOGGING_INTERNAL")]
#endif
    public static void LogWarningFormat(Object context, string format, params object[] args)
    {
        if (logLevel > ShowLogLevel.Warn) return;
        UnityEngine.Debug.LogWarningFormat(context, format, args);
    }

#if BLOCK_LOGGING_INTERNAL
    [System.Diagnostics.Conditional("ALLOW_LOGGING_INTERNAL")]
#endif
    public static void DrawSphere(Vector3 position, Color color, float size = 1, uint frame = 1)
    {
        DebugHelper.Instance.AddSphere(position, color, size, frame);
    }
}