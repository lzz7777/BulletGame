// Copyright (c) Bytedance. All rights reserved.
// Description:

using UnityEngine;

namespace Douyin.LiveOpenSDK.Utilities
{
    internal static class SdkUtils
    {
        internal static string GetUnityDeviceId()
        {
            return SystemInfo.deviceUniqueIdentifier;
        }

        private static string s_hashDeviceId;

        internal static string GetHashDeviceId()
        {
            if (!string.IsNullOrEmpty(s_hashDeviceId))
                return s_hashDeviceId;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            // note: 在一些主机、例如tb云主机上，可能主板和bios的序列号为空、且windows序列号为相同，导致 unity api 取到的 did 相同
            // note: 因此我们这里hash增加一些计算因子
            // 1. unity api did
            var unity_did = GetUnityDeviceId();
            // 2. windows 设备名
            var name = SystemInfo.deviceName;
            // 3. 设备型号
            var model = SystemInfo.deviceModel;
            // 4. windows 设备ID。 即Windows系统信息(about)里的"设备ID"
            var ad_id = "";
#if UNITY_WSA
            // note: `ad_id`: 需要设备上打开隐私设置的允许广告id（PC Settings -> Privacy -> Let apps use my advertising ID）, 否则为空
            ad_id = UnityEngine.WSA.Application.advertisingIdentifier;
#endif
            var hash = Hash128.Compute($"{unity_did}_{name}_{model}_{ad_id}");
            var did = hash.ToString();
#else
            var did = GetUnityDeviceId();
#endif
            s_hashDeviceId = did;
            return did;
        }
    }
}