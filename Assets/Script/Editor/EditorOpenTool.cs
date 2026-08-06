#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace XN
{
    public class EditorOpenTool
    {
        [MenuItem("GameObject/📦>> 打开 ui预制体", false, 0)]
        private static void OpenPrefab()
        {
            var (selectedGameObject, info) = GetUIRef();
            var childName = selectedGameObject.name;
            GUIUtility.systemCopyBuffer = childName;

            var isOne = Selection.gameObjects.Length == 1;
            if (!isOne)
            {
                Debug.Log($"selectedGameObject {childName}");
                Debug.Log("=== !isOne ====");
                foreach (var go in Selection.gameObjects)
                {
                    Debug.Log(go.name);
                }

                Debug.Log("##########");
            }

            if (!isOne)
            {
                selectedGameObject = Selection.gameObjects[^1];
            }

            var hierarchyPath = GetFullPath(info.gameObj.transform, selectedGameObject.transform);

            OpenPrefab(info);

            PingUIByPath(hierarchyPath);
        }

        [MenuItem("GameObject/🔴>> 打开 System代码", false, 0)]
        private static void OpenSystemScript()
        {
            var (selectedGameObject, info) = GetUIRef();
            OpenPrefab(info, true);

            var fileName = info.Name + "System";

            var searchStr = string.Empty;
            var searchStr2 = string.Empty;
            if (selectedGameObject != info.gameObj)
            {
                searchStr2 = "self." + selectedGameObject.name;
                var IsBtn = selectedGameObject.GetComponent<Button>() != null;

                if (IsBtn)
                {
                    searchStr = $" {selectedGameObject.name}ButtonOnClick";
                }
                else
                {
                    searchStr = "self." + selectedGameObject.name;

                    var graphics = selectedGameObject.GetComponents<MaskableGraphic>();
                    if (graphics.Length == 1)
                    {
                        var graphic = graphics[0];
                        var t = graphic.GetType().Name;
                        Debug.Log(t);
                        searchStr2 = searchStr;
                        searchStr += t;
                    }
                }

                GUIUtility.systemCopyBuffer = "self." + selectedGameObject.name;
            }

            SearchScriptAndOpen(fileName, searchStr, searchStr2);
        }

        public struct UIInfo : IEquatable<UIInfo>
        {
            public GameObject gameObj;

            public string Name
            {
                get => RemoveCloneSuffix(gameObj.name);
            }

            public bool Equals(UIInfo other)
            {
                return Equals(this.gameObj, other.gameObj);
            }

            public override bool Equals(object obj)
            {
                return obj is UIInfo other && this.Equals(other);
            }

            public override int GetHashCode()
            {
                return (this.gameObj != null ? this.gameObj.GetHashCode() : 0);
            }
        }

        private static string RemoveCloneSuffix(string name)
        {
            if (name.Contains("(Clone)"))
            {
                //去掉 (Clone) 后所有内容
                name = name.Substring(0, name.LastIndexOf("(Clone)", StringComparison.Ordinal));
            }

            return name;
        }

        /// <summary>
        /// 递归构建从父物体到子物体的完整路径
        /// </summary>
        private static string GetFullPath(Transform parent, Transform target)
        {
            if (target == parent) return parent.name;

            List<string> pathSegments = new List<string>();
            Transform current = target;

            // 向上遍历直到父物体
            while (current != null && current != parent)
            {
                pathSegments.Add(RemoveCloneSuffix(current.name));
                current = current.parent;
            }

            // 添加父物体名称
            // pathSegments.Add(parent.name);
            pathSegments.Reverse(); // 反转列表：父->子

            return string.Join("/", pathSegments); // 输出 Parent/Child/GrandChild
        }

        /// <summary>
        /// 通过路径差查找子物体
        /// </summary>
        private static void PingUIByPath(string relativePath)
        {
            /*// 1. 组合完整路径
            string fullPath = parent.name + "/" + relativePath;
            // 2. 使用Transform.Find()按路径查找
            return parent.Find(fullPath);*/

            var orgTran = Selection.activeGameObject.transform.Find(relativePath);
            if (orgTran == null)
            {
                Debug.LogError($"未找到子对象: {relativePath}");
                PingUI(relativePath.Split("/")[^1]);
                return;
            }

            EditorGUIUtility.PingObject(orgTran.gameObject);
            Selection.activeGameObject = orgTran.gameObject;
        }

        private static (GameObject, UIInfo) GetUIRef(GameObject go = null)
        {
            // 获取当前选中的游戏对象  
            GameObject selectedGameObject = Selection.activeGameObject;
            if (go != null)
            {
                selectedGameObject = go;
            }

            // 检查是否选中了游戏对象  
            if (selectedGameObject != null)
            {
                var prefabGo = PrefabUtility.GetNearestPrefabInstanceRoot(selectedGameObject);

                if (!prefabGo)
                {
                    Transform target = selectedGameObject.transform;
                    while (true)
                    {
                        if (!target)
                        {
                            break;
                        }

                        if (target.name.Contains("(Clone)"))
                        {
                            prefabGo = target.gameObject;
                            break;
                        }

                        if (target.TryGetComponent<UIPanelBase>(out var _) || target.TryGetComponent<UIItemBase>(out var _))
                        {
                            prefabGo = target.gameObject;
                            break;
                        }

                        target = target.parent;
                    }
                }

                var info = new UIInfo()
                {
                    gameObj = prefabGo,
                };
                return (selectedGameObject, info);
            }

            throw new Exception("没有选中游戏对象");
        }

        private static Transform FindChildRecursively(Transform parent, string name)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>())
            {
                if (child.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return child;
            }

            return null;
        }

        public static GameObject OpenPrefab(UIInfo ui, bool isOnlyMark = false)
        {
            var prefabName = ui.Name;

            Debug.Log($"Name {prefabName}");

            string[] matches = AssetDatabase.FindAssets($"t:Prefab {prefabName}");

            if (matches.Length > 0)
            {
                foreach (var aMatch in matches)
                {
                    var prefabPath = AssetDatabase.GUIDToAssetPath(aMatch);
                    string fileName = Path.GetFileNameWithoutExtension(prefabPath);
                    if (fileName.Equals(prefabName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (isOnlyMark)
                        {
                            return null;
                        }

                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                        AssetDatabase.OpenAsset(prefab);
                        EditorWindow.GetWindow<SceneView>()?.Focus();
                        return prefab;
                    }
                }
            }

            Debug.LogError($"未找到: {prefabName}");
            GUIUtility.systemCopyBuffer = prefabName;

            var ui2 = ui.gameObj.transform.parent.GetComponentInParent<UIReferenceCollector>();
            if (ui2 != null)
            {
                var go = OpenPrefab(new UIInfo() { gameObj = ui2.gameObject }, isOnlyMark);

                if (!isOnlyMark)
                    PingUI(ui.Name);

                return go;
            }

            return null;
        }

        private static void PingUI(string childName)
        {
            Debug.Log($"PingUI {childName}");

            // 查找目标子对象
            Transform target = FindChildRecursively(Selection.activeGameObject.transform, childName);
            if (target != null)
            {
                // 高亮选中对象
                EditorGUIUtility.PingObject(target.gameObject);
                Selection.activeGameObject = target.gameObject;
                // SceneView.lastActiveSceneView.FrameSelected();
            }
            else
            {
                Debug.LogError($"未找到子对象: {childName}");
            }
        }

        private static (string, int) SearchScript(string name, string searchStr = "", string searchStr2 = "")
        {
            Debug.Log($"Name {name} {searchStr} {searchStr}");

            if (string.IsNullOrEmpty(searchStr))
            {
                // uiName = name;
                searchStr = "[UIEntitySystem]";
            }

            string targetDirectory = Path.Combine(Application.dataPath, "Script\\MGGame");
            string[] scriptFiles = Directory.GetFiles(targetDirectory, $"*{name}.cs", SearchOption.AllDirectories);

            foreach (string filePath in scriptFiles)
            {
                if (!filePath.Contains($"\\{name}.cs")) continue;

                string[] lines = File.ReadAllLines(filePath);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains(searchStr))
                    {
                        return (filePath.Replace('/', '\\'), i + 1);
                    }
                }

                Debug.LogError($"未搜索到: {name} {searchStr}");

                //再搜索
                if (!string.IsNullOrEmpty(searchStr2))
                {
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].Contains(searchStr2))
                        {
                            return (filePath.Replace('/', '\\'), i + 1);
                        }
                    }

                    Debug.LogError($"未搜索到: {name} {searchStr2}");
                }

                return (filePath.Replace('/', '\\'), 10);
            }

            throw new Exception($"未找到: {name} {searchStr}");
        }

        private static void SearchScriptAndOpen(string name, string searchStr = "", string searchStr2 = "")
        {
            var (filePath, line) = SearchScript(name, searchStr, searchStr2);
            // filePath = filePath.Replace('/', '\\');
            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(filePath, line);
        }
    }
}
#endif