#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UICollectorComponentEnum
{
    None,
    Trans,
    RectTrans,
    Canvas,
    CanvasGroup,
    Image,
    Text,
    Btn,
    Toggle,
    Input,
    RawImage,
    PointerClick,
    Slider,
    CanvasRaycastFilter,
    Dropdown,
    LoopGridItem,
    ToggleGroup,
    PointerDoubleClick,
    PointerLongPress,
    PointerDownUp,
    PointerDrag,
    PointerMask,
    CircleMask,
    RectMask,
    CircleImage,
    SwitchGroup,
    Switch,
    GroupView,
    GroupViewItem,
    ScrollRect,
    Scrollbar,
    LayoutView,
    TreeView,
    TreeViewItem,
    PointerDragReceiver,
    SkeletonGraphic,
    CircleMaskPro,
    RectMaskPro,
    FrameAnimation,
    Reuse,// UI复用组件类型
    Tab,
    Animation,
    IconsMeter,
    SubViewGroup,
    DoTween,
    DoTweenManager,
    PointerUp,
    PointerDown,
    WorldImage,
    ParticleImage,
    ParticleManager,
    AnimCtrl,
    DynamicScroll,
    DynamicScrollEx,
    TMP_3DText,
    TMP_UGUI,
    TMP_InputField,
    EventPermeate,
    RichText,
    TextSizeFitter,
    Particle,
    ParticleButton,
    
    LoopList,
    LoopListItem,
    SubView,// 子页面
    VerticalLayoutGroup,
}

public static class UICollectorData
{
    public static UICollectorComponentEnum GetComponentEnum(Component mono)
    {
        UICollectorComponentEnum e;
        string t = mono?.GetType()?.ToString();
        // Debug.Log(t);
        return t switch
        {
            "UnityEngine.RectTransform" => UICollectorComponentEnum.RectTrans,
            "UnityEngine.Transform" => UICollectorComponentEnum.Trans,
            "UnityEngine.Canvas" => UICollectorComponentEnum.Canvas,
            "UnityEngine.CanvasGroup" => UICollectorComponentEnum.CanvasGroup,
            "UnityEngine.Animation" => UICollectorComponentEnum.Animation,
            
            "UnityEngine.UI.Image" => UICollectorComponentEnum.Image,
            "UnityEngine.UI.Text" => UICollectorComponentEnum.Text,
            "UnityEngine.UI.Button" => UICollectorComponentEnum.Btn,
            "UnityEngine.UI.Toggle" => UICollectorComponentEnum.Toggle,
            "UnityEngine.UI.InputField" => UICollectorComponentEnum.Input,
            "UnityEngine.UI.Slider" => UICollectorComponentEnum.Slider,
            "UnityEngine.UI.ToggleGroup" => UICollectorComponentEnum.ToggleGroup,
            "UnityEngine.UI.ScrollRect" => UICollectorComponentEnum.ScrollRect,
            "UnityEngine.UI.Scrollbar" => UICollectorComponentEnum.Scrollbar,
            "UnityEngine.UI.Dropdown" => UICollectorComponentEnum.Dropdown,
            "UnityEngine.UI.VerticalLayoutGroup" => UICollectorComponentEnum.VerticalLayoutGroup,
            
            "TMPro.TextMeshPro" => UICollectorComponentEnum.TMP_3DText,
            "TMPro.TextMeshProUGUI" => UICollectorComponentEnum.TMP_UGUI,
            "TMPro.TMP_InputField" => UICollectorComponentEnum.TMP_InputField,
            
            "XN.UILoopList" => UICollectorComponentEnum.LoopList,
            "XN.UILoopListItem" => UICollectorComponentEnum.LoopListItem,
            "XN.UISubView" => UICollectorComponentEnum.SubView,
            
            _ => UICollectorComponentEnum.None
        };
    }

    public static string GetComponentEntityType(UICollectorComponentEnum componentType)
    {
        return componentType switch
        {
            UICollectorComponentEnum.RectTrans => "RectTransform",
            UICollectorComponentEnum.Trans => "Transform",
            UICollectorComponentEnum.Btn => "Button",
            UICollectorComponentEnum.TMP_UGUI => "TextMeshProUGUI",
            UICollectorComponentEnum.LoopList  => "UILoopList",
            UICollectorComponentEnum.LoopListItem => "UILoopListItem",
            UICollectorComponentEnum.SubView => "UISubView",
            
            _ => $"{componentType}",
        };
    }
}
#endif