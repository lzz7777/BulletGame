//====================================================
//Author:Makka Pakka
//Time  :2024/12/21 15:27:42
//Desc  :
//====================================================

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace XN.Tools
{
    [InitializeOnLoad]
    public static class ToolBar
    {
        static ToolBar()
        {
            ToolBarExt.LeftToolbarGUI.Add(ToolBarLeft);
            ToolBarExt.RightToolbarGUI.Add(ToolBarRight);
        }

        private static void ToolBarLeft()
        {
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("切换场景")) ShowScenesMenu();
            if (GUILayout.Button("查日志")) XnGitSvnExt.DumpGitSvn();
            if (GUILayout.Button("构建")) XnBuildExt.ShowWindow();
        }

        private static void ToolBarRight()
        {
            var is2D = UnityEngine.Rendering.GraphicsSettings.transparencySortMode == TransparencySortMode.CustomAxis;
            var is3D = UnityEngine.Rendering.GraphicsSettings.transparencySortMode == TransparencySortMode.Default;

            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal(GUILayout.Width(100));
            var pressed2D = GUILayout.Toggle(is2D, "2D-Y", "Button", GUILayout.Width(60));
            var pressed3D = GUILayout.Toggle(is3D, "Default", "Button", GUILayout.Width(60));
            GUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                bool change = false;
                if (pressed2D && !is2D)
                {
                    UnityEngine.Rendering.GraphicsSettings.transparencySortMode = TransparencySortMode.CustomAxis;
                    UnityEngine.Rendering.GraphicsSettings.transparencySortAxis = new Vector3(0f, 0.01f, 0);
                    change = true;
                }
                else if (pressed3D && !is3D)
                {
                    UnityEngine.Rendering.GraphicsSettings.transparencySortMode = TransparencySortMode.Default;
                    change = true;
                }

                // 设置切换后，强制刷新
                if (change)
                {
                    // 脚本热重载 触发Unity重新编译 触发渲染生效
                    UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
                }
            }
        }

        private static void ShowScenesMenu()
        {
            var sceneGUIDS = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
            if (sceneGUIDS is { Length: > 0 })
            {
                GenericMenu menu = new();
                foreach (var sceneGUID in sceneGUIDS)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(sceneGUID);
                    var relativeName = assetPath.Replace("Assets/Scenes/", "").Replace(".unity", "");
                    menu.AddItem(new GUIContent(relativeName), false, () => EditorSceneManager.OpenScene(assetPath));
                }

                menu.ShowAsContext();
            }
        }
    }
}
#endif