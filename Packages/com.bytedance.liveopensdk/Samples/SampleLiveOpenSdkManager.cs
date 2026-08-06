// Copyright (c) Bytedance. All rights reserved.
// Description:

#nullable enable
using System;
using System.Threading;
using ByteDance.Live.Foundation.Logging;
using ByteDance.LiveOpenSdk;
using ByteDance.LiveOpenSdk.Runtime;
using ByteDance.LiveOpenSdk.Runtime.Utilities;

namespace Douyin.LiveOpenSDK.Samples
{
    /// <summary>
    /// 直播开放 SDK 的接入示例代码。
    /// 配置项：<see cref="AppId"/>
    /// </summary>
    /// <seealso cref="ILiveOpenSdk"/>
    public static class SampleLiveOpenSdkManager
    {
        private static readonly LogWriter Log = new LogWriter(SdkUnityLogger.LogSink, "SampleGameStartup");

        /// <summary>
        /// 直播伴侣 token，仅供调试用。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 此参数用于相关 API 鉴权。若没有 token，大部分开放能力无法使用。
        /// </para>
        /// <para>
        /// 小玩法的可执行程序被直播伴侣或云游戏正常启动时，会从命令行参数传递 token。
        /// SDK 初始化时，会自动解析命令行并获取 token，不需要开发者设置。
        /// </para>
        /// <para>
        /// 目前暂时不支持开发者手动获取 token，但在 Unity 中调试时，可以用这个属性手动指定一个 token。
        /// </para>
        /// </remarks>
        public static string Token
        {
            get => Sdk.Env.Token;
            set => Sdk.Env.Token = value;
        }

        /// <summary>
        /// 直播开放 SDK 的实例对象。
        /// </summary>
        public static ILiveOpenSdk Sdk => LiveOpenSdk.Instance;

        static SampleLiveOpenSdkManager()
        {
            // 将 SDK 内部的日志发往 Unity 的控制台。
            Sdk.LogSource.OnLog -= SdkUnityLogger.LogSink.WriteLog;
            Sdk.LogSource.OnLog += SdkUnityLogger.LogSink.WriteLog;
        }

        /// <summary>
        /// 初始化 SDK。
        /// </summary>
        /// <remarks>
        /// 请在 Unity 主线程调用。
        /// </remarks>
        public static void Initialize(string appId)
        {
            // 设置 SDK 的事件触发线程为 Unity 主线程。
            Sdk.DefaultSynchronizationContext = SynchronizationContext.Current;

            try
            {
                // 同步初始化。
                Log.Info($"开始初始化直播开放 SDK");
                Sdk.Initialize(appId);
                Log.Info($"初始化直播开放 SDK：成功");
            }
            catch (Exception)
            {
                // 正常情况下不会失败，若遇到问题，请和我们联系。
                Log.Error($"初始化直播开放 SDK：失败");
                throw;
            }
        }

        /// <summary>
        /// 释放 SDK。通常在退出游戏或停止预览时调用。
        /// </summary>
        public static void Uninitialize()
        {
            Sdk.Uninitialize();
        }
    }
}