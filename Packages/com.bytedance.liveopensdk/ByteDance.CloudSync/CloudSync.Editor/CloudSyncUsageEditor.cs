// Copyright (c) Bytedance. All rights reserved.
// Author: DONEY Dong
// Date: 2025/04/09
// Description:

using Douyin.LiveOpenSDK.Editor;
using UnityEditor;
using UnityEngine;

namespace ByteDance.CloudSync.Editor
{
    public class CloudSyncUsageEditor : ICloudSyncChecker, ICloudSyncUsageEditor, SdkConfigWindow.ICloudSyncUsageEditor
    {
        private static CloudSyncUsageEditor Instance { get; } = new();
        private bool _hasCheckShowWindow;

        public static class SettingsKeys
        {
            internal const string UsingCloudSync = "DouyinLiveUsingCloudSync";
        }

        [InitializeOnLoadMethod]
        private static void OnScriptLoad()
        {
            // Debug.Log("云同步-CloudSyncUsageEditor OnScriptLoad"); // local debug only
            SdkBuildAutoProcess.CloudSyncUsageChecker = Instance;
            SdkConfigWindow.CloudSyncUsageEditor = Instance;
            CloudSyncSdk.CloudSyncUsageEditor = Instance;
        }

        public bool IsUsingCloudSync() => IsSetting(SettingsKeys.UsingCloudSync, true);

        public void SetUsingCloudSync(bool value)
        {
            SetSetting(SettingsKeys.UsingCloudSync, value);
            if (!value)
                return;
            if (_hasCheckShowWindow)
                return;
            _hasCheckShowWindow = true;
            if (!SdkConfigWindow.IsConfigValid())
                SdkConfigWindow.ShowWindow();
        }

        private static bool IsSetting(string key, bool value)
        {
            var configValue = EditorUserSettings.GetConfigValue(key);
            var targetValue = value ? "1" : "0";
            // Debug.Log($"云同步-CloudSyncUsageEditor Is '{key}' {configValue} == {targetValue}, return {configValue == targetValue}"); // local debug only
            return configValue == targetValue;
        }

        private static void SetSetting(string key, bool value)
        {
            var configValue = EditorUserSettings.GetConfigValue(key);
            var targetValue = value ? "1" : "0";
            if (configValue == targetValue)
                return;
            Debug.Log($"云同步-CloudSyncUsageEditor Set '{key}' = {targetValue}");
            EditorUserSettings.SetConfigValue(key, targetValue);
        }
    }
}