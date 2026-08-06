namespace ByteDance.CloudSync
{
    public interface ICloudSyncEnv : IEnv
    {
        bool IsEnvReady { get; }

        /// <summary>
        /// 是否真实线上环境
        /// </summary>
        bool IsRealEnv();

        /// <summary>
        /// 是否云游戏环境（云启动）
        /// </summary>
        bool IsCloud();

        /// <summary>
        /// 是否抖音直播环境
        /// </summary>
        bool IsDouyin();

        /// <summary>
        /// 是否抖音PC直播伴侣本地启动
        /// </summary>
        bool IsDouyinPcLocal();

        /// <summary>
        /// 是否移动端
        /// </summary>
        /// <remarks>若是 true，是使用抖音手机端开播玩法。 若 false，则PC端使用直播伴侣开播玩法。</remarks>
        bool IsMobile();

        /// <summary>
        /// 是否可用联网匹配（多人联机模式）。
        /// </summary>
        /// <remarks>目前移动端仅支持玩法内的单人模式，暂不支持多人联机模式</remarks>
        bool CanUseOnlineMatch();

        /// <summary>
        /// 小玩法appId。 形如'tt123456abcd1234'。 应当在CloudGame系统初始化Init时设置。
        /// </summary>
        string AppId { get; }

        string CloudGameToken => GetStringValue(SdkConsts.ArgAppCloudGameToken);

        string CloudDeviceDid => GetStringValue(SdkConsts.ArgAppCloudDeviceId);

        string CloudGameLogId => GetStringValue(SdkConsts.ArgAppLogId);

        string DevicePlatform => GetStringValue(SdkConsts.ArgAppDevicePlatform);

        string GameId => GetStringValue(SdkConsts.ArgAppGameId);
    }
}