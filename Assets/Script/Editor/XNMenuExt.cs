#if UNITY_EDITOR
//====================================================
//Author:lixin
//Time  :2025/11/25 17:35
//Desc  :
//====================================================

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using System.Linq;
using System.Reflection;
using RenderHeads.Media.AVProVideo;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace XN.Tools
{
    public static class XNMenuExt
    {
        [MenuItem("Assets/XNTools/替换所有字体")]
        public static void ReplaceAllFont()
        {
            string tmpFontPath = "Assets/Sources/Font/AlimamaShuHeiTi-Bold SDF.asset";
            string uiFontPath = "Assets/Sources/Font/AlimamaShuHeiTi-Bold.ttf";

            var targetTmpFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(tmpFontPath);
            var targetFont = AssetDatabase.LoadAssetAtPath<Font>(uiFontPath);

            var selection = Selection.objects;
            var targets = selection.Where(o => o is GameObject || PrefabUtility.GetPrefabAssetType(o) != PrefabAssetType.NotAPrefab).ToArray();

            if (targets.Length == 0)
            {
                EditorUtility.DisplayDialog("替换所有字体", "请在 Project 或 Hierarchy 选择至少一个预制体/对象", "确定");
                return;
            }
            if (targetFont == null && targetTmpFont == null)
            {
                EditorUtility.DisplayDialog("替换所有字体", "未找到目标字体资源，请检查路径配置", "确定");
                return;
            }

            foreach (var obj in targets)
            {
                if (obj is GameObject go && PrefabUtility.GetPrefabAssetType(go) == PrefabAssetType.NotAPrefab)
                {
                    var (tc, mc) = ReplaceFontsInHierarchy(go, targetFont, targetTmpFont);
                    Debug.Log($"{go.name} 的字体变更成功 Text:{tc}/TMP:{mc}");
                    continue;
                }

                var path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path)) continue;
                var root = PrefabUtility.LoadPrefabContents(path);
                var (textCount, tmpCount) = ReplaceFontsInHierarchy(root, targetFont, targetTmpFont);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
                Debug.Log($"{path} 的字体变更成功 Text:{textCount}/TMP:{tmpCount}");
            }
        }

        private static (int textCount, int tmpCount) ReplaceFontsInHierarchy(GameObject root, Font font, TMP_FontAsset tmpFont)
        {
            int t = 0, m = 0;
            if (font != null)
            {
                foreach (var txt in root.GetComponentsInChildren<Text>(true))
                {
                    var outer = PrefabUtility.GetOutermostPrefabInstanceRoot(txt.gameObject);
                    if (outer != null && outer != root) continue;
                    txt.font = font;
                    t++;
                    EditorUtility.SetDirty(txt);
                }
            }
            if (tmpFont != null)
            {
                foreach (var tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    var outer = PrefabUtility.GetOutermostPrefabInstanceRoot(tmp.gameObject);
                    if (outer != null && outer != root) continue;
                    tmp.font = tmpFont;
                    m++;
                    EditorUtility.SetDirty(tmp);
                }
            }
            return (t, m);
        }


        const string SrcUnityPath = "Assets/StreamingAssets/Video";
        const string OutUnityPath = "Assets/Sources/Video";
        const bool OverwriteExisting = true;
        
        [MenuItem("Assets/XNTools/生成对应AVProVideo")]
        public static void ReplaceAllVideo()
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName.Replace("\\", "/");
            var srcAbs = Path.Combine(projectRoot, SrcUnityPath).Replace("\\", "/");
            if (!Directory.Exists(srcAbs)) { Debug.LogWarning("未找到源目录: " + SrcUnityPath); return; }
            Directory.CreateDirectory(Path.Combine(projectRoot, OutUnityPath));

            var files = Directory.EnumerateFiles(srcAbs, "*.mp4", SearchOption.AllDirectories).ToList();
            int count = 0;

            List<string> saveList = new List<string>();
            foreach (var f in files)
            {
                var unityPath = f.Replace("\\", "/").Replace(projectRoot + "/", "");
                string rel = unityPath.Replace("Assets/StreamingAssets/", "");               // 形如: Video/xxx.mp4
                var nameNoExt = Path.GetFileNameWithoutExtension(f);
                var outAssetPath = (OutUnityPath + "/" + nameNoExt + ".asset").Replace("\\", "/");

                var inst = AssetDatabase.LoadAssetAtPath<MediaReference>(outAssetPath);
                if (inst == null)
                {
                    inst = ScriptableObject.CreateInstance<MediaReference>();
                    AssetDatabase.CreateAsset(inst, outAssetPath);
                    Debug.Log($"new .... : {outAssetPath}");
                }

                inst.MediaPath = new MediaPath(rel, MediaPathType.RelativeToStreamingAssetsFolder);
                inst.Hints = new MediaHints()
                {
                    transparency = TransparencyMode.Transparent,
                    alphaPacking = AlphaPacking.None,
                    stereoPacking = StereoPacking.None,
                };
                var so = new SerializedObject(inst);
                var pathProp = so.FindProperty("m_Path");
                var locProp = so.FindProperty("m_Location");
                if (pathProp != null) pathProp.stringValue = rel;
                if (locProp != null && locProp.propertyType == SerializedPropertyType.Enum)
                {
                    var names = locProp.enumDisplayNames;
                    for (int i = 0; i < names.Length; i++)
                    {
                        var s = names[i].ToLowerInvariant();
                        if (s.Contains("streamingassets")) { locProp.enumValueIndex = i; break; }
                    }
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                
                // 生成缩略图
                // var mi = typeof(MediaReference).GetMethod("GenerateThumbnail", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                // if (mi != null) { try { mi.Invoke(inst, null); } catch { } }

                EditorUtility.SetDirty(inst);
                saveList.Add(nameNoExt);
                count++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Generated/Updated mp4 => MediaReference.asset: {count} 个");
            
            // Delete 无效引用MediaReference
            string[] guids = AssetDatabase.FindAssets("t:MediaReference", new[] { OutUnityPath });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                string nameNoExt = Path.GetFileNameWithoutExtension(path);
                if (!saveList.Contains(nameNoExt))
                {
                    // MediaReference mediaR = AssetDatabase.LoadAssetAtPath<MediaReference>(path);
                    Debug.Log($"delete .... : {path}");
                    AssetDatabase.DeleteAsset(path);
                }
            }
            AssetDatabase.Refresh();
            
            // 更新场景
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (activeScene.name == "Game")
            {
                var vm = Object.FindFirstObjectByType<VideoManager>();
                vm?.CollectorPlayerMedia();
                if (vm == null)
                {
                    Debug.LogError("在 Game 场景中未找到 XN.VideoManager 组件，请检查场景对象。");
                }
            }
            else
            {
                Debug.LogWarning($"当前场景 [{activeScene.name}] 非 Game 场景。无法自动收集 PlayerMedia。\n请手动打开 Game 场景，选中 VideoManager 节点组件，点击 Inspector 中的 [收集资源] 按钮。");
            }
        }
    }
}
#endif