using Douyin.LiveOpenSDK.Modules;
using Douyin.LiveOpenSDK.Utilities;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Douyin.LiveOpenSDK
{
    /// <summary>
    /// 直播小玩法开放SDK Runtime的旧API入口，暂时保留用作兼容。
    /// </summary>
    internal static class LiveOpenSdkRuntime
    {
        /// <summary>
        /// 云启动云游戏API
        /// </summary>
        public static ILiveCloudGameAPI CloudGameAPI => s_cloudGameApi ?? CreateCloudGameApi();

        #region internal

        private static readonly SdkCore Core;
        internal static readonly SdkDebugInfo DebugInfo;
        internal static readonly SdkDebugLogger Debug;
        private static ILiveCloudGameAPI s_cloudGameApi;

        static LiveOpenSdkRuntime()
        {
            Core = new SdkCore();
            DebugInfo = Core.SdkDebugInfo;
            Debug = Core.Debug;
            DebugInfo.LogDebugVer();
            DebugInfo.LogDebugEnvs();
            DebugInfo.LogDebugCmdArgs();
            DebugInfo.LogDebugDid();
            var env = Core.Env;
            env.TryInitCloudGameScreen();
        }

        [RuntimeInitializeOnLoadMethod]
        private static void TryInitCloudGame()
        {
            var env = Core.Env;
            var ret = env.TryInitCloudGameScreen();
            Debug.LogDebug($"TryInitCloudGame screen: {ret}");
        }

        private static ILiveCloudGameAPI CreateCloudGameApi()
        {
            s_cloudGameApi = new ApiCloudGame(Core);
            return s_cloudGameApi;
        }

        #endregion
    }
}