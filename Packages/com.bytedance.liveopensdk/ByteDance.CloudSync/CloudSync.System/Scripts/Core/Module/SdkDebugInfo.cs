// Copyright@www.bytedance.com
// Author: DONEY Dong
// Date: 2024/05/07
// Description:

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace ByteDance.CloudSync
{
    public class SdkDebugInfo
    {
        private static SdkDebugLogger Debug => CloudGameSdkManager.Debug;

        private static ICloudSyncEnv SdkEnv => CloudSyncSdk.Env;

        public void LogDebugVer()
        {
            try
            {
                var gameVer = Application.version;
                var gameVerInfo = $"GameApp ver: {gameVer} name: {Application.productName}" +
                                  $"    {Application.platform} - {Application.unityVersion}";
                Debug.Log(gameVerInfo);

                if (SdkEnv.IsEnvReady)
                {
                    var sdkVer = CloudGameSdkManager.SdkLibVersion;
                    var sdkVerInfo = $"CloudGameSdk lib ver: {sdkVer}";
                    Debug.Log(sdkVerInfo);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void LogDebugEnvs()
        {
            try
            {
                var lines = _GetPathLines();
                var text = string.Join("\n", lines);
                Debug.LogDebug(text);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static List<string> _GetPathLines()
        {
            var lines = new List<string>();

            lines.Add("---- current path ----");
            lines.Add($"System.Environment.CurrentDirectory: {TryGet(() => Environment.CurrentDirectory)}");
            lines.Add($"System.IO.Directory.GetCurrentDirectory: {TryGet(Directory.GetCurrentDirectory)}");
            lines.Add("---- unity app path ----");
            lines.Add($"unity Application.dataPath: {Application.dataPath}");
            lines.Add($"unity Application.persistentDataPath: {Application.persistentDataPath}");
            lines.Add($"unity Application.consoleLogPath: {Application.consoleLogPath}");
            lines.Add($"unity Application.streamingAssetsPath: {Application.streamingAssetsPath}");
            // lines.Add("---- assembly path ----");
            // lines.Add($"CurrentDomain.BaseDirectory: {TryGet(() => AppDomain.CurrentDomain?.BaseDirectory)}");
            // lines.Add($"ExecutingAssembly().Location: {TryGet(() => Assembly.GetExecutingAssembly()?.Location)}");
            // lines.Add($"EntryAssembly().Location: {TryGet(() => Assembly.GetEntryAssembly()?.Location)}");
            return lines;
        }

        private static string TryGet(Func<string> getter)
        {
            try
            {
                return getter.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogWarning(e.ToString());
            }

            return string.Empty;
        }

        public void LogDebugCmdArgs()
        {
            if (!SdkConsts.IsPC)
                return;
            try
            {
                // only standalone has valid commandline
                var commandline = Environment.CommandLine;
                Debug.LogDebug($"LogDebugCmdArgs: {commandline}");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}