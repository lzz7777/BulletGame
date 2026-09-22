using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace XN
{
    public class UIReferenceCollector : MonoBehaviour
    {
        // --- 面试讲述点：Odin 标签解析 ---
        // [VerticalGroup("A")] / [HorizontalGroup("A/A")]：用于实现属性面板的行列排版，让工具界面紧凑直观。
        // [GUIColor]：动态或静态修改颜色，例如给拖拽框青色，给错误提示红色，增强防呆设计。
        // [OnValueChanged]：监听数据变化，拖入节点后触发 OnComponentsChanged 自动去重并加入下方列表。
        // [ListDrawerSettings]：深度定制 List，隐藏原生的添加按钮，强制使用拖拽添加，规范操作流。
        // ---------------------------------
#if UNITY_EDITOR
        [VerticalGroup("A")]
        [GUIColor(0, 1, 1)]
        [OnValueChanged("OnComponentsChanged")]
        [LabelText("拖拽添加物体")]
        [ListDrawerSettings(ShowFoldout = false, HideAddButton = true)]
        public List<Transform> addMonoObjects = new();

        // [HideIf]：防呆设计核心！如果当前预制体存在丢失引用（ErrorGameObject != null），则隐藏生成按钮，强制美术/程序先修错。
        [HorizontalGroup("A/A")]
        [HideIf("@this.ErrorGameObject!=null")]
        // [HideIf("@this.CheckIsHideCreateBtn()")]
        [Button("代码生成", ButtonSizes.Medium)]
        public void Generate()
        {
            UIReferenceGenerator.Generate(this);
        }

        // [ShowIf]：结合 @表达式 调用 CheckError()，实时扫描是否有节点被删除了但引用还在，有则标红显示。
        [GUIColor(1, 0, 0)] [ShowIf("@this.CheckError()!=null")] [HorizontalGroup("A/A")] [HideLabel]
        public Component ErrorGameObject = null;

        // [InfoBox("列表Item", "@CheckItemView() && this.CheckParent()&& !CheckPrefab()")]
        // [InfoBox("通用列表Item预制体", "@CheckItemView() && this.CheckParent() && CheckPrefab()")]
        [HorizontalGroup("A/A")]
        [Button("绑定代码", ButtonSizes.Medium)]
        public void Refresh()
        {
            // 1. 获取需要绑定的脚本类型（基于当前预制体名字）
            string typeName = transform.name;
            string scriptTypeName = typeName; // 脚本类名
            // 面试亮点：体现了热更分离架构，UI 逻辑脚本全部放在 HotUpdate 程序集里
            System.Type scriptType = System.Type.GetType($"XN.{scriptTypeName}, HotUpdate");
            Debug.Log($"scriptType:{scriptType}");

            GameObject prefabAsset = gameObject;
            if (prefabAsset == null)
            {
                Debug.LogWarning($"无法加载预制体资源");
                return;
            }

            // 2. 检查预制体是否已经挂载了该脚本，没有则自动挂载 (自动化绑定第一步)
            Component existingComponent = prefabAsset.GetComponent(scriptType);
            if (existingComponent == null)
            {
                existingComponent = prefabAsset.AddComponent(scriptType);
                UnityEditor.EditorUtility.SetDirty(prefabAsset);
                UnityEditor.AssetDatabase.SaveAssets();
            }

            // 3. 核心：通过 Unity 的 SerializedObject 进行强类型引用绑定
            // 面试亮点：彻底消灭运行时的 GetComponent 和 Transform.Find，全部在 Editor 预先序列化好
            UnityEditor.SerializedObject serializedObject = new UnityEditor.SerializedObject(existingComponent);

            // 遍历所有收集到的数据结构
            foreach (UICollectorObjcetData objectData in objectDatas)
            {
                GameObject targetGameObject = objectData.transform.gameObject;
                foreach (var componentData in objectData.componentDatas)
                {
                    // 获取目标节点上的真实组件引用
                    UnityEngine.Object componentReference =
                        targetGameObject.GetComponent(
                            UICollectorData.GetComponentEntityType(componentData.ComponentEnum));
                    string fieldName = componentData.name;
                    // 通过反射找到生成脚本里对应的字段名
                    UnityEditor.SerializedProperty property = serializedObject.FindProperty(fieldName);
                    // 将真实引用赋值给脚本字段
                    property.objectReferenceValue = componentReference;
                }
                // collectorObjcetData.componentDatas
            }

            // 4. 应用修改并保存预制体
            serializedObject.ApplyModifiedProperties();
            UnityEditor.EditorUtility.SetDirty(prefabAsset);
            UnityEditor.AssetDatabase.SaveAssets();

            Debug.Log($"{transform.name} 绑定代码!");
        }
        
        // [Searchable]：给列表加上搜索框，并实现 ISearchFilterable 接口实现按节点名字模糊搜索。
        [VerticalGroup("B")]
        [LabelText("物体列表")]
        [Searchable(Recursive = false, FuzzySearch = false,
            FilterOptions = SearchFilterOptions.ISearchFilterableInterface)]
        [ListDrawerSettings(ShowFoldout = false, HideAddButton = true)]
        public List<UICollectorObjcetData> objectDatas = new();

        // 拖入节点后的回调：自动去重
        private void OnComponentsChanged(List<Transform> validateTransform)
        {
            foreach (var obj in addMonoObjects)
            {
                // 如果当前收集列表里没有这个节点，才加进去，防止重复收集
                if (this.objectDatas.All(d => d.transform != obj))
                {
                    objectDatas.Add(new UICollectorObjcetData() { transform = obj });
                }
                else
                {
                    Debug.Log($"物体 [{obj.name}] 已存在");
                }
            }

            // 清空拖拽框，准备下一次拖拽
            addMonoObjects.Clear();
        }

        #region 错误判断

        // 实时检测收集的节点是否失效（比如预制体里把这个节点删了）
        private Component CheckError()
        {
            Component errorObj = null;
            foreach (var objectData in objectDatas)
            {
                // 如果发现有丢失引用的节点
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