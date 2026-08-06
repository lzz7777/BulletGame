namespace ByteDance.CloudSync
{
    public class CloudGameAndroidLaunchParams
    {
        [Newtonsoft.Json.JsonProperty("StartAppParam")]
        public string startAppParam;

        [Newtonsoft.Json.JsonProperty("ad_channel")]
        public string adChannel;

        [Newtonsoft.Json.JsonProperty("ad_id")]
        public string adId;

        [Newtonsoft.Json.JsonProperty("ad_request_id")]
        public string adRequestId;

        [Newtonsoft.Json.JsonProperty("click_id_nature")]
        public string clickIdNature;

        [Newtonsoft.Json.JsonProperty("cloud_device_id")]
        public string cloudDeviceId;

        [Newtonsoft.Json.JsonProperty("crash_info")]
        public string crashInfo;

        [Newtonsoft.Json.JsonProperty("data")] public string data;

        [Newtonsoft.Json.JsonProperty("debug_enable")]
        public string debugEnable;

        [Newtonsoft.Json.JsonProperty("device_info")]
        public string deviceInfo;

        [Newtonsoft.Json.JsonProperty("enable_archive_download")]
        public string enableArchiveDownload;

        [Newtonsoft.Json.JsonProperty("enable_archive_upload")]
        public string enableArchiveUpload;

        [Newtonsoft.Json.JsonProperty("enable_browser")]
        public string enableBrowser;

        [Newtonsoft.Json.JsonProperty("enable_crashlog")]
        public string enableCrashLog;

        [Newtonsoft.Json.JsonProperty("enter_from")]
        public string enterFrom;

        [Newtonsoft.Json.JsonProperty("env")] public string env;

        [Newtonsoft.Json.JsonProperty("frame_height")]
        public string frameHeight;

        [Newtonsoft.Json.JsonProperty("frame_width")]
        public string frameWidth;

        [Newtonsoft.Json.JsonProperty("is_play_card")]
        public string isPlayCard;

        [Newtonsoft.Json.JsonProperty("is_vip")]
        public string isVip;

        [Newtonsoft.Json.JsonProperty("isp")] public string isp;

        [Newtonsoft.Json.JsonProperty("live_room_id")]
        public string liveRoomId;

        [Newtonsoft.Json.JsonProperty("prod_id_or_accessKey_id")]
        public string prodIdOrAccessKeyId;

        [Newtonsoft.Json.JsonProperty("request_id")]
        public string requestId;

        [Newtonsoft.Json.JsonProperty("start_app_param_inject")]
        public string startAppParamInject;

        [Newtonsoft.Json.JsonProperty("supplier_name")]
        public string supplierName;
    }
}