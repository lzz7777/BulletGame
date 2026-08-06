// Copyright@www.bytedance.com
// Author: DONEY Dong
// Date: 2024/04/28
// Description:

// ReSharper disable once CheckNamespace
namespace ByteDance.CloudSync
{
    internal class CloudSyncEnv : ProxyEnv, ICloudSyncEnv
    {
        private readonly SdkEnv _rawEnv;
        private static bool hasFirstPrint;

        internal CloudSyncEnv()
        {
            _rawEnv = new SdkEnv();
            Inner = _rawEnv;
        }

        internal SdkEnv SdkEnv => _rawEnv;

        /// <summary>
        /// 用 newEnv 上已有的 k-v 覆盖
        /// </summary>
        /// <param name="newEnv"></param>
        public void OverrideWith(IWritableEnv newEnv)
        {
            Inner = newEnv.Override(Inner);
        }

        public bool IsRealEnv()
        {
            // note: 需要在EnvReady前，就决定是否真实环境
            var cloudGame = CloudSyncSdk.Env.GetIntValue(SdkConsts.ArgCloudGame);
            var isCloud = cloudGame == 1;
            var token = CloudSyncSdk.Env.GetStringValue(SdkConsts.ArgLaunchToken);
            var isDouyin = !string.IsNullOrEmpty(token);
            var isReal = isCloud || isDouyin;
            if (!hasFirstPrint)
            {
                hasFirstPrint = true;
                CGLogger.Log($"云同步-运行环境Env: IsRealEnv: {isReal}, IsCloud: {isCloud}, IsDouyin: {isDouyin}" +
                             $", IsDouyinPcLocal: {IsDouyinPcLocal()}, IsMobile: {IsMobile()}");
            }

            return isReal;
        }

        public bool IsCloud() => _rawEnv.IsCloud();

        public bool IsDouyin() => _rawEnv.IsDouyin();

        public bool IsDouyinPcLocal() => IsDouyin() && !IsCloud() && !IsMobile();

        public bool IsMobile() => _rawEnv.IsStartFromMobile();

        public bool CanUseOnlineMatch()
        {
            // 移动端和非移动端都能支持多人联机模式
            return true;
        }

        /// <summary>
        /// 小玩法appId。 形如'tt123456abcd1234'。 应当在CloudGame系统初始化Init时设置。
        /// </summary>
        public string AppId
        {
            get => Inner.GetStringValue(SdkConsts.GameArgAppId);
            set => Inner.SetValue(SdkConsts.GameArgAppId, value);
        }

        /// <summary>
        /// 环境是否ready。 在 CloudGameSdk.API.Init() 成功时设置为 true.
        /// </summary>
        public bool IsEnvReady => _rawEnv.IsEnvReady;
    }
}