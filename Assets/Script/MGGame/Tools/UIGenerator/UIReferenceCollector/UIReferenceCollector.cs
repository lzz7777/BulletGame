using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace XN
{
    public class UIReferenceCollector : MonoBehaviour
    {
#if UNITY_EDITOR
        [VerticalGroup("A")]
        [GUIColor(0, 1, 1)]
        [OnValueChanged("OnComponentsChanged")]
        [LabelText("拖拽添加物体")]
        [ListDrawerSettings(ShowFoldout = false, HideAddButton = true)]
        public List<Transform> addMonoObjects = new();

        [HorizontalGroup("A/A")]
        [HideIf("@this.ErrorGameObject!=null")]
        // [HideIf("@this.CheckIsHideCreateBtn()")]
        [Button("代码生成", ButtonSizes.Medium)]
        public void Generate()
        {
            UIReferenceGenerator.Generate(this);
        }

        [GUIColor(1, 0, 0)] [ShowIf("@this.CheckError()!=null")] [HorizontalGroup("A/A")] [HideLabel]
        public Component ErrorGameObject = null;

        // [InfoBox("列表Item", "@CheckItemView() && this.CheckParent()&& !CheckPrefab()")]
        // [InfoBox("通用列表Item预制体", "@CheckItemView() && this.CheckParent() && CheckPrefab()")]
        [HorizontalGroup("A/A")]
        [Button("绑定代码", ButtonSizes.Medium)]
        public void Refresh()
        {
            string typeName = transform.name;
            string scriptTypeName = typeName; // 脚本类名
            System.Type scriptType = System.Type.GetType($"XN.{scriptTypeName}, HotUpdate");
            Debug.Log($"scriptType:{scriptType}");

            GameObject prefabAsset = gameObject;
            if (prefabAsset == null)
            {
                Debug.LogWarning($"无法加载预制体资源");
                return;
            }

            Component existingComponent = prefabAsset.GetComponent(scriptType);
            if (existingComponent == null)
            {
                existingComponent = prefabAsset.AddComponent(scriptType);
                UnityEditor.EditorUtility.SetDirty(prefabAsset);
                UnityEditor.AssetDatabase.SaveAssets();
            }

            UnityEditor.SerializedObject serializedObject = new UnityEditor.SerializedObject(existingComponent);

            foreach (UICollectorObjcetData objectData in objectDatas)
            {
                GameObject targetGameObject = objectData.transform.gameObject;
                foreach (var componentData in objectData.componentDatas)
                {
                    UnityEngine.Object componentReference =
                        targetGameObject.GetComponent(
                            UICollectorData.GetComponentEntityType(componentData.ComponentEnum));
                    string fieldName = componentData.name;
                    UnityEditor.SerializedProperty property = serializedObject.FindProperty(fieldName);
                    property.objectReferenceValue = componentReference;
                }
                // collectorObjcetData.componentDatas
            }

            serializedObject.ApplyModifiedProperties();
            UnityEditor.EditorUtility.SetDirty(prefabAsset);
            UnityEditor.AssetDatabase.SaveAssets();

            Debug.Log($"{transform.name} 绑定代码!");
        }
        [VerticalGroup("B")]
        [LabelText("物体列表")]
        [Searchable(Recursive = false, FuzzySearch = false,
            FilterOptions = SearchFilterOptions.ISearchFilterableInterface)]
        [ListDrawerSettings(ShowFoldout = false, HideAddButton = true)]
        public List<UICollectorObjcetData> objectDatas = new();

        private void OnComponentsChanged(List<Transform> validateTransform)
        {
            foreach (var obj in addMonoObjects)
            {
                if (this.objectDatas.All(d => d.transform != obj))
                {
                    objectDatas.Add(new UICollectorObjcetData() { transform = obj });
                }
                else
                {
                    Debug.Log($"物体 [{obj.name}] 已存在");
                }
            }

            addMonoObjects.Clear();
        }

        #region 错误判断

        private Component CheckError()
        {
            Component errorObj = null;
            foreach (var objectData in objectDatas)
            {
                if (!objectData.CheckObjectAndRemoveComponents())
                {
                    errorObj = this;
                    break;
                }
            }

            ErrorGameObject = errorObj;
            return errorObj;
        }

        #endregion
#endif
    }
}