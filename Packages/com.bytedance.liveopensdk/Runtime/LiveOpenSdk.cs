// Copyright (c) Bytedance. All rights reserved.
// Description:

using System.Collections.Generic;
using System.Threading;
using ByteDance.CloudSync;
using ByteDance.CloudSync.External;
using ByteDance.LiveOpenSdk.Room;
using ByteDance.LiveOpenSdk.Runtime.Modules;
using ByteDance.LiveOpenSdk.Runtime.Utilities;
using ByteDance.LiveOpenSdk.Tea;
using Douyin.LiveOpenSDK;
using UnityEngine;

namespace ByteDance.LiveOpenSdk.Runtime
{
    /// <summary>
    /// 直播开放 SDK 的 API 入口点。
    /// </summary>
    public static class LiveOpenSdk
    {
        public static ILiveOpenSdk Instance { get; } = new LiveOpenSdkImpl();

        public static ILiveCloudGameAPI CloudGameApi => LiveOpenSdkRuntime.CloudGameAPI;

        /// <summary>
        /// 云同步 SDK
        /// </summary>
        public static ICloudSync CloudSync => ICloudSync.Instance;

        private static IPerfMonitor PerfMonitor => PerfMonitorImpl.Instance;
        private static IDebugUtilsController DebugUtilsController => DebugUtilsControllerImpl.Instance;

        private static bool IsWindows =>
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            true;
#else
            false;
#endif

        static LiveOpenSdk()
        {
            InitTeaCommonProps();
            CloudSyncExternals.LiveOpenSdkProvide = () => Instance;
            if (IsWindows && !Application.isEditor)
            {
                PerfMonitor.StartMonitor();
            }
            DebugUtilsController.StartLoop();
        }

        [RuntimeInitializeOnLoadMethod]
        private static void InitPlugins()
        {
            LiveOpenSdkImpl.InitCallback = new InitCallbackImpl();
        }

        /// <summary>
        /// 初始化 LiveOpenSdk
        /// </summary>
        /// <param name="appId">小玩法appId。 形如'tt123456abcd1234'</param>
        public static void Init(string appId)
        {
            Instance.Uninitialize();
            Instance.LogSource.OnLog -= SdkUnityLogger.LogSink.WriteLog;
            Instance.LogSource.OnLog += SdkUnityLogger.LogSink.WriteLog;
            Instance.DefaultSynchronizationContext = SynchronizationContext.Current;
            Instance.Initialize(appId);
        }

        private static void InitDateProcessTrigger()
        {
            Instance.GetService<IRoomInfoService>().OnRoomInfoChanged -= DebugUtilsControllerImpl.Instance.OnRoomInfoUpdate;
            Instance.GetService<IRoomInfoService>().OnRoomInfoChanged += DebugUtilsControllerImpl.Instance.OnRoomInfoUpdate;
        }

        private static void InitTeaCommonProps()
        {
            var scriptingBackend = GetScriptingBackend();
            var props = LiveOpenSdkImpl.InternalEnv.TeaCommonEventProperties
                        ?? new Dictionary<string, object>();
            props[EventProperties.ScriptingBackend] = scriptingBackend;
            props[EventProperties.UnityVersion] = Application.unityVersion;
            props["game_identifier"] = Application.identifier;
            props["game_version"] = Application.version;
            props["bgdt_sdk_version"] = SdkSettings.Version;
            LiveOpenSdkImpl.InternalEnv.TeaCommonEventProperties = props;

            var deviceProperties = LiveOpenSdkImpl.InternalEnv.TeaDeviceProperties
                        ?? new Dictionary<string, object>();
            deviceProperties[EventProperties.CpuModel] = SystemInfo.processorType;
            deviceProperties[EventProperties.CpuFrequency] = SystemInfo.processorFrequency;
            deviceProperties[EventProperties.GpuModel] = SystemInfo.graphicsDeviceName;
            deviceProperties[EventProperties.GpuMemory] = SystemInfo.graphicsMemorySize;
            deviceProperties[EventProperties.SystemMemory] = SystemInfo.systemMemorySize;
            LiveOpenSdkImpl.InternalEnv.TeaDeviceProperties = deviceProperties;
        }

        private static string GetScriptingBackend()
        {
            const string scriptingBackend =
#if ENABLE_MONO
            "mono";
#elif ENABLE_IL2CPP
            "il2cpp";
#else
            "unknown";
#endif
            if (Application.isEditor)
                return $"editor.{scriptingBackend}";
            return scriptingBackend;
        }

        internal static LiveOpenSdkInternalEnv InternalEnv => LiveOpenSdkImpl.InternalEnv;



        class InitCallbackImpl : IInitCallback
        {
            public void OnInitBegin()
            {

            }

            public void OnInitResult(bool success)
            {
                if (!success)
                    return;
                InitDateProcessTrigger();
            }
        }
    }
}