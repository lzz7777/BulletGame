using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using System.Collections.Generic;

public class PCTextureCompressTool : EditorWindow
{
    private TextureImporterFormat textureFormat = TextureImporterFormat.BC7;
    private int maxTextureSize = 2048;
    private bool includeSpriteAtlas = true;

    [MenuItem("Tools/PC端图片批量压缩工具 (BC7)")]
    public static void ShowWindow()
    {
        var window = GetWindow<PCTextureCompressTool>("PC图片压缩");
        window.minSize = new Vector2(350, 250);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("批量设置选中图片/图集的 PC(Standalone) 压缩格式", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical("box");
        textureFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup("目标压缩格式", textureFormat);
        maxTextureSize = EditorGUILayout.IntPopup("最大尺寸限制", maxTextureSize,
            new string[] { "512", "1024", "2048", "4096", "8192" },
            new int[] { 512, 1024, 2048, 4096, 8192 });
        includeSpriteAtlas = EditorGUILayout.Toggle("同时处理 SpriteAtlas (图集)", includeSpriteAtlas);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("使用方法：\n1. 在 Project 窗口中选中需要处理的 文件夹、图片 或 图集。\n2. 点击下方按钮开始批量压缩。\n\n注：会自动开启 'Override for PC, Mac & Linux Standalone'。", MessageType.Info);
        EditorGUILayout.Space();

        GUI.color = Color.green;
        if (GUILayout.Button("压缩选中的资源", GUILayout.Height(40)))
        {
            ProcessSelectedAssets();
        }
        GUI.color = Color.white;
    }

    private void ProcessSelectedAssets()
    {
        // 获取选中的所有资源 GUID
        string[] guids = Selection.assetGUIDs;
        if (guids == null || guids.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先在 Project 窗口选中要处理的资源或文件夹！", "确定");
            return;
        }

        List<string> assetPaths = new List<string>();
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetDatabase.IsValidFolder(path))
            {
                // 搜索文件夹下的图片和图集
                string filter = includeSpriteAtlas ? "t:Texture2D t:SpriteAtlas" : "t:Texture2D";
                string[] subGuids = AssetDatabase.FindAssets(filter, new string[] { path });
                foreach (var subGuid in subGuids)
                {
                    string subPath = AssetDatabase.GUIDToAssetPath(subGuid);
                    if (!assetPaths.Contains(subPath))
                        assetPaths.Add(subPath);
                }
            }
            else
            {
                // 直接选中文件的情况
                if (path.EndsWith(".png") || path.EndsWith(".jpg") || path.EndsWith(".tga") || path.EndsWith(".tif") || path.EndsWith(".psd"))
                {
                    if (!assetPaths.Contains(path)) assetPaths.Add(path);
                }
                else if (includeSpriteAtlas && (path.EndsWith(".spriteatlas") || path.EndsWith(".spriteatlasv2")))
                {
                    if (!assetPaths.Contains(path)) assetPaths.Add(path);
                }
            }
        }

        if (assetPaths.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "在选中的范围内未找到任何图片或图集资源。", "确定");
            return;
        }

        if (!EditorUtility.DisplayDialog("确认", $"共找到 {assetPaths.Count} 个资源，是否确认将其 PC 平台压缩格式设置为 {textureFormat}？\n\n这可能需要一些时间。", "开始处理", "取消"))
        {
            return;
        }

        int total = assetPaths.Count;
        int count = 0;
        bool isCancel = false;

        try
        {
            AssetDatabase.StartAssetEditing(); // 暂停自动导入，大幅提高批量处理速度
            foreach (string path in assetPaths)
            {
                count++;
                isCancel = EditorUtility.DisplayCancelableProgressBar("批量处理中", $"正在处理: {path} ({count}/{total})", (float)count / total);
                if (isCancel) break;

                if (path.EndsWith(".spriteatlas") || path.EndsWith(".spriteatlasv2"))
                {
                    ProcessSpriteAtlas(path);
                }
                else
                {
                    ProcessTexture(path);
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (isCancel)
                EditorUtility.DisplayDialog("提示", "操作已手动取消。", "确定");
            else
                EditorUtility.DisplayDialog("提示", $"处理完成！\n成功修改了 {count} 个资源的压缩设置。", "确定");
        }
    }

    private void ProcessTexture(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings("Standalone");
        bool needSave = false;

        // 如果还没有被覆盖，或者格式/大小不匹配，则进行修改
        if (!settings.overridden || settings.format != textureFormat || settings.maxTextureSize != maxTextureSize)
        {
            settings.overridden = true;
            settings.format = textureFormat;
            settings.maxTextureSize = maxTextureSize;
            importer.SetPlatformTextureSettings(settings);
            needSave = true;
        }

        if (needSave)
        {
            importer.SaveAndReimport();
        }
    }

    private void ProcessSpriteAtlas(string path)
    {
        SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
        if (atlas == null) return;

        TextureImporterPlatformSettings settings = atlas.GetPlatformSettings("Standalone");
        bool needSave = false;

        if (!settings.overridden || settings.format != textureFormat || settings.maxTextureSize != maxTextureSize)
        {
            settings.overridden = true;
            settings.format = textureFormat;
            settings.maxTextureSize = maxTextureSize;
            
            // 使用扩展方法设置 SpriteAtlas 的平台属性
            atlas.SetPlatformSettings(settings);
            needSave = true;
        }

        if (needSave)
        {
            EditorUtility.SetDirty(atlas);
        }
    }
}
