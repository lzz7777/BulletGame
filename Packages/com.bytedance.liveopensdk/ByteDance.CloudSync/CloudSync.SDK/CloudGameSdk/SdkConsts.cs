using UnityEngine;

namespace ByteDance.CloudSync
{
    internal static class SdkConsts
    {
        public const string GameArgAppId = "game@app-id";

        public const string ArgLaunchToken = "token";
        public const string ArgOpenId = "open-id";
        public const string ArgStartAppParam = "StartAppParam";

        public const string ArgMobile = "mobile";
        public const string ArgCloudGame = "cloud-game";
        public const string ArgScreenFullscreen = "screen-fullscreen";
        public const string ArgScreenHeight = "screen-height";
        public const string ArgScreenWidth = "screen-width";

        public const string ArgAppCloudDeviceId = "app@cloud_device_id";
        public const string ArgAppLogId = "app@log_id";
        public const string ArgAppDevicePlatform = "app@device_platform";
        public const string ArgAppGameId = "app@game_id";
        public const string ArgAppCloudGameToken = "app@cloud_game_token";

        public static bool IsAndroidPlayer => !Application.isEditor && Application.platform == RuntimePlatform.Android;

        /// <summary>
        /// 是否PC版本 (含Editor)
        /// </summary>
        public static bool IsPC => Application.platform == RuntimePlatform.WindowsEditor ||
                                   Application.platform == RuntimePlatform.WindowsPlayer;
    }
}