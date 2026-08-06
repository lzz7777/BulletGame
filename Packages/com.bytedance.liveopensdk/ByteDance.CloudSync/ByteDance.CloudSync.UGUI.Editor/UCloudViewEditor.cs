// Copyright (c) Bytedance. All rights reserved.
// Author: DONEY Dong
// Date: 2025/04/09
// Description:

using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ByteDance.CloudSync.UGUI.Editor
{
    public class UCloudViewEditor : UCloudView.IEditorValidator
    {
        private static UCloudViewEditor Instance { get; } = new();
        private bool _hasCheckedSettings;
        private static bool _sHasCheckedSettings;

        [InitializeOnLoadMethod]
        private static void OnScriptLoad()
        {
            // Debug.Log("云同步-UCloudViewEditor OnScriptLoad"); // local debug only
            UCloudView.EditorValidator = Instance;
        }

        public bool IsInputSystemSettingsChecked() => _hasCheckedSettings && _sHasCheckedSettings;

        private void SetInputSystemSettingsChecked()
        {
            _hasCheckedSettings = true;
            _sHasCheckedSettings = true;
        }

        public bool IsEditorBusy()
        {
            Debug.Log($"isCompiling: {EditorApplication.isCompiling}, isBuilding: {BuildPipeline.isBuildingPlayer}");
            if (EditorApplication.isCompiling)
                return true;
            if (BuildPipeline.isBuildingPlayer)
                return true;
            return false;
        }

        public void ValidateInputSystemSettings()
        {
            SetInputSystemSettingsChecked();
            var settings = GetInputSystemSettings(out var isIgnoreFocus, out var isAlwaysGameView);
            if (settings != null && isAlwaysGameView && isIgnoreFocus)
            {
                return;
            }

            const string title = "云同步设置提示";
            const string msg1 = "请将 Project Settings -> Input System Package -> Background Behavior - 设置为: Ignore Focus";
            const string msg2 = "请将 Project Settings -> Input System Package -> Play Mode Input Behavior - 设置为: All Device Input Always Goes To GameView";
            if (!isIgnoreFocus)
                Debug.LogError(title + ": " + msg1);
            if (!isAlwaysGameView)
                Debug.LogError(title + ": " + msg2);

            const string kSettingsPath = "Project/Input System Package";
            SettingsService.OpenProjectSettings(kSettingsPath);
            if (!isIgnoreFocus)
                EditorUtility.DisplayDialog(title, title + ":\n" + msg1, "OK");
            if (!isAlwaysGameView)
                EditorUtility.DisplayDialog(title, title + ":\n" + msg2, "OK");
        }

        private static InputSettings GetInputSystemSettings(out bool isIgnoreFocus, out bool isAlwaysGameView)
        {
            isIgnoreFocus = false;
            isAlwaysGameView = false;
            var inputSettings = InputSystem.settings;
            if (inputSettings == null)
                return null;
            var backgroundBehavior = inputSettings.backgroundBehavior;
            var playModeInputBehavior = inputSettings.editorInputBehaviorInPlayMode;
            // CGLogger.Log($"Validate device input, backgroundBehavior: {backgroundBehavior}, playModeInputBehavior: {playModeInputBehavior}");
            isIgnoreFocus = backgroundBehavior == InputSettings.BackgroundBehavior.IgnoreFocus;
            isAlwaysGameView = playModeInputBehavior == InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            return inputSettings;
        }
    }
}