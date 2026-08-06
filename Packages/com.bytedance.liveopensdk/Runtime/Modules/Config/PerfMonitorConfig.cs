// Copyright (c) Bytedance. All rights reserved.
// Description:


using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ByteDance.LiveOpenSdk.Runtime;
using UnityEngine;
using UnityEngine.Networking;

namespace Douyin.LiveOpenSDK
{
    internal class PerfMonitorConfigUtils
    {
        private const string URI = "https://is.snssdk.com/service/settings/v3/?caller_name=LiveOpenSDK";
        private static LiveOpenSdkConfigData _config;
        public static async Task RequestConfig()
        {
            var www = UnityWebRequest.Get(GetSettingsUrl());
            www.downloadHandler = new DownloadHandlerBuffer();
            await www.SendWebRequest();
            var resp = www.downloadHandler.text;
            if (!resp.Contains("PerfReport"))
            {
                Debug.LogError("PerfMonitorConfigUtils RequestConfig cannot find 'PerfReport'");
                return;
            }
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<LiveOpenSdkConfigSettingData>(resp);
            _config = data.data.settings;
            www.Dispose();
        }
        private static string GetSettingsUrl()
        {
#if TEST_PERF_MONITOR_CONFIG
            return $"{URI}&device_id=1234567&game_app_id={LiveOpenSdk.Instance.Env.AppId}";
#else
            return $"{URI}&device_id={SystemInfo.deviceUniqueIdentifier}&game_app_id={LiveOpenSdk.Instance.Env.AppId}";
#endif
        }
        public static PerfMonitorFpsConfig FpsConfig => _config?.PerfReportConfig.PerfMonitorFpsConfig;
        public static PerfMonitorJankConfig JankConfig => _config?.PerfReportConfig.PerfMonitorJankConfig;
        public static PerfMonitorMemoryConfig MemoryConfig => _config?.PerfReportConfig.PerfMonitorMemoryConfig;
    }
    [Serializable]
    internal class LiveOpenSdkConfigSettingData
    {
        public LiveOpenSdkConfigSettings data;
    }

    [Serializable]
    internal class LiveOpenSdkConfigSettings
    {
        public LiveOpenSdkConfigData settings;
    }

    [DataContract]
    [Serializable]
    internal class LiveOpenSdkConfigData
    {
        [DataMember(Name = "PerfReport")] public PerfReportConfig PerfReportConfig { get; set; }
    }

    [DataContract]
    [Serializable]
    internal class PerfReportConfig
    {
        [DataMember(Name = "fps")] public PerfMonitorFpsConfig PerfMonitorFpsConfig { get; set; }
        [DataMember(Name = "jank")] public PerfMonitorJankConfig PerfMonitorJankConfig { get; set; }
        [DataMember(Name = "memory")] public PerfMonitorMemoryConfig PerfMonitorMemoryConfig { get; set; }
    }

    [DataContract]
    [Serializable]
    internal class PerfMonitorFpsConfig
    {
        [DataMember(Name = "time_frame")] public int TimeFrame { get; set; }
        [DataMember(Name = "min")] public int Min { get; set; }
    }

    [DataContract]
    [Serializable]
    internal class PerfMonitorJankConfig
    {
        [DataMember(Name = "time_frame")] public int TimeFrame { get; set; }
        [DataMember(Name = "pre_frame_count")] public int PreFrameCount { get; set; }
        [DataMember(Name = "limit")] public JankLevelData[] JankLevelLimits { get; set; }
    }

    [DataContract]
    [Serializable]
    internal class PerfMonitorMemoryConfig
    {
        [DataMember(Name = "time_frame")] public int TimeFrame { get; set; }
        [DataMember(Name = "max")] public float MaxPercent { get; set; }
    }

    [DataContract]
    [Serializable]
    internal class JankLevelData
    {
        [DataMember(Name = "level")] public int Level { get; set; } // 严重等级
        [DataMember(Name = "times")] public int Times { get; set; } // 卡顿次数
        [DataMember(Name = "ratio_for_pre_frames")] public int FrameTimeTimes { get; set; } // 该帧是前PreFramesCount帧的耗时倍数
        [DataMember(Name = "movie_cost_times_time")] public int MovieFrameTimeCostTimes { get; set; } // 电影帧耗时倍数(ms)
    }
}