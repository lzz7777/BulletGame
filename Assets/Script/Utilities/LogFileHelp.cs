using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public static class LogFileHelp
{
    private static readonly string[] JumpLog =
        { "LockBufferForWrite:", "BoxCollider does not support negative scale or size" };

    public static void Init()
    {
#if UNITY_EDITOR
        return;
#endif
        string logFile;
        if (CommandLine.IsCloudGame())
            logFile = Path.Combine(Application.persistentDataPath,
                "LocalPlayer.log");
        else
            //打印日志
            logFile = Path.Combine(Application.persistentDataPath,
                $"log-{DateTimeHelper.NowServer:yyyy-MM-dd-HH-mm-ss}.log");
        var dir = Path.GetDirectoryName(logFile);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        try
        {
            if (File.Exists(logFile)) File.Delete(logFile);
        }
        catch (Exception)
        {
            return;
        }

        Application.logMessageReceivedThreaded += (condition, stackTrace, type) =>
        {
            if (type == LogType.Log)
            {
                if (JumpLog.Any(se => condition.Contains(se))) return;

                if (condition.Length > 2048)
                {
#if UNITY_EDITOR
                    condition = "[isJumpLog]" + condition;
#else
                    return;
#endif
                }
            }

            try
            {
                //上传日志
                // DataManager.UpdateLog($"{condition}\r\n{stackTrace}\r\n{type}");
                File.AppendAllText(logFile, Debug.bindTime(condition) + "\r\n", Encoding.UTF8);
                //写入本地日志
                File.AppendAllText(logFile, stackTrace + "\r\n", Encoding.UTF8);
            }
            catch
            {
                // ignored
            }
        };
    }
}