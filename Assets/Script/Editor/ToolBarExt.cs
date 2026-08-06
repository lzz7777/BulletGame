//====================================================
//Author:Makka Pakka
//Time  :2024/12/21 15:11:05
//Desc  :
//====================================================

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.UIElements;
using UnityEngine;

namespace UnityEditor
{
    [InitializeOnLoad]
    public static class ToolBarExt
    {
        static int m_toolCount;
        static GUIStyle m_commandStyle = null;
        static bool m_toolbarInjected = false;

        public static readonly List<Action> LeftToolbarGUI = new();
        public static readonly List<Action> RightToolbarGUI = new();

#if UNITY_2019_3_OR_NEWER
        public const float space = 8;
#else
		public const float space = 10;
#endif
        public const float largeSpace = 20;
        public const float buttonWidth = 32;
        public const float dropdownWidth = 80;
#if UNITY_2019_1_OR_NEWER
        public const float playPauseStopWidth = 140;
#else
		public const float playPauseStopWidth = 100;
#endif

        static ToolBarExt()
        {
            Type toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");

#if UNITY_2019_1_OR_NEWER
            string fileName = "k_ToolCount";
#else
            string fileName = "s_ShownToolIcons";
#endif

            FieldInfo toolIcons = toolbarType.GetField(fileName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

#if UNITY_2019_3_OR_NEWER
            m_toolCount = toolIcons != null ? ((int)toolIcons.GetValue(null)) : 8;
#elif UNITY_2019_1_OR_NEWER
			m_toolCount = toolIcons != null ? ((int) toolIcons.GetValue(null)) : 7;
#elif UNITY_2018_1_OR_NEWER
			m_toolCount = toolIcons != null ? ((Array) toolIcons.GetValue(null)).Length : 6;
#else
			m_toolCount = toolIcons != null ? ((Array) toolIcons.GetValue(null)).Length : 5;
#endif

            // ToolbarCallback.OnToolbarGUI = OnGUI;
            // ToolbarCallback.OnToolbarGUILeft = GUILeft;
            // ToolbarCallback.OnToolbarGUIRight = GUIRight;

            // Unity 6 使用 UI Toolkit 渲染主工具栏，祖传的 ToolbarCallback 在新版本中不可用。
            // 这里改为在 EditorApplication.update 中，找到 Toolbar 的 VisualElement 并注入 IMGUI 容器。
            EditorApplication.update -= InjectToolbar;
            EditorApplication.update += InjectToolbar;
        }

        static void InjectToolbar()
        {
            if (m_toolbarInjected)
                return;

            var toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
            if (toolbarType == null)
                return;

            var toolbars = Resources.FindObjectsOfTypeAll(toolbarType);
            if (toolbars == null || toolbars.Length == 0)
                return;

            var toolbar = toolbars[0];
            var rootField = toolbarType.GetField("m_Root", BindingFlags.Instance | BindingFlags.NonPublic);
            var root = rootField?.GetValue(toolbar) as VisualElement;
            if (root == null)
                return;

            // UI Toolkit zone names（Unity 2021+，Unity 6 依旧可用）
            var leftZone = root.Q("ToolbarZoneLeftAlign");
            var rightZone = root.Q("ToolbarZoneRightAlign");

            if (leftZone != null && rightZone != null)
            {
                leftZone.Add(new IMGUIContainer(() => GUILeft()));
                rightZone.Add(new IMGUIContainer(() => GUIRight()));
                m_toolbarInjected = true;
            }
            else
            {
                // 兜底：如果找不到左右区域，直接在根上挂一个综合的 IMGUI 容器。
                root.Add(new IMGUIContainer(() => OnGUI()));
                m_toolbarInjected = true;
            }

            if (m_toolbarInjected)
                EditorApplication.update -= InjectToolbar;
        }

        static void OnGUI()
        {
            // Create two containers, left and right
            // Screen is whole toolbar

            m_commandStyle ??= new GUIStyle("CommandLeft");

            var screenWidth = EditorGUIUtility.currentViewWidth;

            // Following calculations match code reflected from Toolbar.OldOnGUI()
            float playButtonsPosition = Mathf.RoundToInt((screenWidth - playPauseStopWidth) / 2);

            Rect leftRect = new(0, 0, screenWidth, Screen.height);
            leftRect.xMin += space; // Spacing left
            leftRect.xMin += buttonWidth * m_toolCount; // Tool buttons

#if UNITY_2019_3_OR_NEWER
            leftRect.xMin += space; // Spacing between tools and pivot
#else
			leftRect.xMin += largeSpace; // Spacing between tools and pivot
#endif

            leftRect.xMin += 64 * 2; // Pivot buttons
            leftRect.xMax = playButtonsPosition;

            Rect rightRect = new(0, 0, screenWidth, Screen.height);
            rightRect.xMin = playButtonsPosition;
            rightRect.xMin += m_commandStyle.fixedWidth * 3; // Play buttons
            rightRect.xMax = screenWidth;
            rightRect.xMax -= space; // Spacing right
            rightRect.xMax -= dropdownWidth; // Layout
            rightRect.xMax -= space; // Spacing between layout and layers
            rightRect.xMax -= dropdownWidth; // Layers

#if UNITY_2019_3_OR_NEWER
            rightRect.xMax -= space; // Spacing between layers and account
#else
			rightRect.xMax -= largeSpace; // Spacing between layers and account
#endif

            rightRect.xMax -= dropdownWidth; // Account
            rightRect.xMax -= space; // Spacing between account and cloud
            rightRect.xMax -= buttonWidth; // Cloud
            rightRect.xMax -= space; // Spacing between cloud and collab
            rightRect.xMax -= 78; // Colab

            // Add spacing around existing controls
            leftRect.xMin += space;
            leftRect.xMax -= space;
            rightRect.xMin += space;
            rightRect.xMax -= space;

            // Add top and bottom margins
#if UNITY_2019_3_OR_NEWER
            leftRect.y = 4;
            leftRect.height = 22;
            rightRect.y = 4;
            rightRect.height = 22;
#else
			leftRect.y = 5;
			leftRect.height = 24;
			rightRect.y = 5;
			rightRect.height = 24;
#endif

            if (leftRect.width > 0)
            {
                GUILayout.BeginArea(leftRect);
                GUILayout.BeginHorizontal();
                foreach (var handler in LeftToolbarGUI) handler();

                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }

            if (rightRect.width > 0)
            {
                GUILayout.BeginArea(rightRect);
                GUILayout.BeginHorizontal();
                foreach (var handler in RightToolbarGUI) handler();

                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }
        }

        public static void GUILeft()
        {
            GUILayout.BeginHorizontal();
            foreach (var handler in LeftToolbarGUI) handler();

            GUILayout.EndHorizontal();
        }

        public static void GUIRight()
        {
            GUILayout.BeginHorizontal();
            foreach (var handler in RightToolbarGUI) handler();

            GUILayout.EndHorizontal();
        }
    }
}
#endif