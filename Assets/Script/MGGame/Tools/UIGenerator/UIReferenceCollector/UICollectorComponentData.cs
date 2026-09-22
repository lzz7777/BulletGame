#if UNITY_EDITOR
using System;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class UICollectorComponentData
{
    // [ReadOnly]：设置字段为只读，这里 name 字段是最终生成的变量名，不允许手动修改，只能通过后缀拼接自动生成。
    [HorizontalGroup("A")] [HideLabel, ReadOnly]
    public string name;

    [HideInInspector] public Transform transform;

    [HorizontalGroup("A")]
    [GUIColor(0, 1, 0, GetColor = "@CheckComponent()")]
    [HideLabel, ReadOnly, HorizontalGroup("A", width: 100)]
    public UICollectorComponentEnum ComponentEnum;

    // [OnValueChanged]：当用户修改自定义后缀时，触发 OnSuffixValueChanged，自动把 节点名+后缀+组件类型 拼接成最终的字段名 (如：Btn_Confirm_Button)
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