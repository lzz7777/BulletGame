#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class UICollectorObjcetData : ISearchFilterable
{
    [GUIColor(0, 1, 0, GetColor = "@TransformColor()")]
    [VerticalGroup("A")]
    [HideLabel, HorizontalGroup("A/节点", width: 200)]
    public Transform transform;
    
    [GUIColor(0, 1, 0, GetColor = "@ComponentEnumColor()")]
    [ValueDropdown("GetComponentEnums", IsUniqueList = false)]
    [HorizontalGroup("A/节点")]
    [OnValueChanged("OnAddComponent")]
    [HideLabel]
    public UICollectorComponentEnum componentEnum;
    
    [ShowIf("@componentDatas.Count > 0")]
    [VerticalGroup("A")]
    [ListDrawerSettings(ShowFoldout = true, HideAddButton = true, HideRemoveButton = false, CustomRemoveIndexFunction = "RemoveButton")]
    [LabelText("组件事件列表")]
    public List<UICollectorComponentData> componentDatas = new();

    public bool IsMatch(string searchString)
    {
        // 只搜索 transform.name
        if (transform != null && !string.IsNullOrEmpty(transform.name))
        {
            return transform.name.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        return false;
    }
    
    /// <summary>
    /// 获取所有组件的枚举
    /// </summary>
    private IEnumerable GetAllComponentEnums()
    {
        if (transform == null) yield return default;

        foreach (var component in transform.GetComponents<Component>())
        {
            var componentType = UICollectorData.GetComponentEnum(component);

            if (componentType == UICollectorComponentEnum.RectTrans)
            {
                yield return UICollectorComponentEnum.Trans;
            }

            yield return componentType;
        }
    }
    
    /// <summary>
    /// 检测物体是否存在，并且移除不存在的组件
    /// </summary>
    public bool CheckObjectAndRemoveComponents()
    {
        if (transform == null) return false;
        if (componentDatas.Count == 0) return false;

        //获取物体的组件
        IEnumerable components = GetAllComponentEnums();
        //遍历组件标记枚举
        for (int i = 0; i < componentDatas.Count;)
        {
            UICollectorComponentData componentData = componentDatas[i];

            bool isContains = false;
            //遍历组件存在
            foreach (UICollectorComponentEnum component in components)
            {
                //查询是否包含这个组件
                if (componentData.ComponentEnum == component)
                {
                    isContains = true;
                    break;
                }
            }

            if (isContains)
            {
                i++;
            }
            else
            {
                //移除这个组件枚举
                RemoveButton(i);
            }
        }

        return true;
    }
    
    /// <summary>
    /// 获取剔除后的枚举
    /// </summary>
    private IEnumerable GetComponentEnums()
    {
        if (transform == null) yield return default;
    
        var result = new List<UICollectorComponentEnum>();
    
        foreach (var component in transform.GetComponents<Component>())
        {
            var componentEnum = UICollectorData.GetComponentEnum(component);
    
            if (componentEnum == UICollectorComponentEnum.RectTrans)
            {
                if (component is RectTransform)
                {
                    if (!componentDatas.Any(d => d.ComponentEnum == UICollectorComponentEnum.Trans))
                    {
                        if (!result.Contains(UICollectorComponentEnum.Trans))
                            result.Add(UICollectorComponentEnum.Trans);
                    }
                }
            }
    
            if (!componentDatas.Any(d => d.ComponentEnum == componentEnum))
            {
                if (!result.Contains(componentEnum))
                    result.Add(componentEnum);
            }
        }
    
        // 按枚举定义顺序排序（即枚举的整数值）
        result.Sort((a, b) => ((int)a).CompareTo((int)b));
    
        foreach (var item in result)
        {
            yield return item;
        }
    }
    //选择添加组件
    private void OnAddComponent(UICollectorComponentEnum type)
    {
        if (type != UICollectorComponentEnum.None)
        {
            int index = componentDatas.FindIndex(d => d.ComponentEnum == type);
            if (index == -1)
            {
                componentDatas.Add(new UICollectorComponentData()
                {
                    transform = transform,
                    ComponentEnum = type,
                    name = this.transform.name + type
                });
            }
            else
            {
                componentDatas.RemoveAt(index);
            }
        }
        else
        {
            componentDatas.Clear();
        }

        componentEnum = UICollectorComponentEnum.None;
    }

    //移除组件
    private void RemoveButton(int index)
    {
        componentDatas.RemoveAt(index);
    }
    
    /// <summary>
    /// 物体颜色
    /// </summary>
    private Color TransformColor() => transform == null || componentDatas.Count == 0 ? Color.red : Color.green;

    /// <summary>
    /// 组件颜色
    /// </summary>
    private Color ComponentEnumColor() => componentDatas.Count == 0 ? Color.red : Color.yellow;
}
#endif
