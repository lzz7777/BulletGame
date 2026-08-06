#if UNITY_EDITOR
using System;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class UICollectorComponentData
{
    [HorizontalGroup("A")] [HideLabel, ReadOnly]
    public string name;

    [HideInInspector] public Transform transform;

    [HorizontalGroup("A")]
    [GUIColor(0, 1, 0, GetColor = "@CheckComponent()")]
    [HideLabel, ReadOnly, HorizontalGroup("A", width: 100)]
    public UICollectorComponentEnum ComponentEnum;

    [HorizontalGroup("A", width: 200)] [OnValueChanged("OnSuffixValueChanged")]
    public string suffix;

    /// <summary>
    /// 检测组件枚举是否存在
    /// </summary>
    private Color CheckComponent()
    {
        return ComponentEnum == UICollectorComponentEnum.None ? Color.red : Color.white;
    }

    private void OnSuffixValueChanged(string str)
    {
        name = transform.name + str + ComponentEnum.ToString();
    }
}
#endif