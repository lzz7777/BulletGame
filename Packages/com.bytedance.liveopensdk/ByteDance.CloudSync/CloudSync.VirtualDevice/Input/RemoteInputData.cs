// Copyright (c) Bytedance. All rights reserved.
// Description:

using System.Collections.Generic;
using UnityEngine;

namespace ByteDance.CloudSync
{
    public interface IRemoteInputEvent
    {
        // ReSharper disable once InconsistentNaming
        public IVirtualDevice device { get; }
        public string InputType { get; }
    }

    /// <summary>
    /// 鼠标事件
    /// </summary>
    public struct RemoteMouseEvent : IRemoteInputEvent
    {
        public IVirtualDevice device { get; set; }
        public string InputType => "Mouse";

        public MouseAction action;

        /// `viewPosition` 左上角坐标 ( 0, 1 ) , 原点 ( 0, 0 ) 在左下角，值范围 0~1.0
        public Vector2 viewPosition;

        /// `position` 左上角坐标 ( 0, Screen.Height ) , 原点 ( 0, 0 ) 在左下角，值范围 0~Screen宽高
        public Vector2 position;

        /// <summary>
        /// 位移变化量. 对齐 <see cref="UnityEngine.InputSystem.LowLevel.MouseState.delta"/>
        /// </summary>
        public Vector2 delta;

        /// <summary>
        /// 按键状态。 多个按键按位赋值，从低位到高位为：Left, Right, Middle。 对齐 <see cref="UnityEngine.InputSystem.LowLevel.MouseState.buttons"/>
        /// </summary>
        /// <example>
        /// 按键对应值举例：
        /// * 没有键按下： = 0 = 0b_0000_0000
        /// * 左键按下： = 1 = 0b_0000_0001
        /// * 仅右键按下： = 2 = 0b_0000_0010
        /// * 左键右键都在按下态： = 3 = 0b_0000_0011
        /// * 仅中键按下： = 4 = 0b_0000_0100
        /// </example>
        /// <remarks>注意：在按下态时，对应位的值保持为 1，直到它松开时变为0。 而不只是在按下的那一刻为 1。</remarks>
        public ushort buttons;

        /// <summary>
        /// 鼠标按键id。 是发生此事件时状态变化的按键。 值为枚举：LEFT, RIGHT, MIDDLE
        /// </summary>
        public MouseButtonId buttonId;

        /// <summary>
        /// 显示屏序号. 对齐 <see cref="UnityEngine.InputSystem.LowLevel.MouseState.displayIndex"/>
        /// </summary>
        public ushort displayIndex;

        /// <summary>
        /// 连续点击次数. 对齐 <see cref="UnityEngine.InputSystem.LowLevel.MouseState.clickCount"/>
        /// </summary>
        public ushort clickCount;

        /// 滚轮  -1.0 下， 1.0 上
        public double wheel;

        /// 可读的事件名
        public string EventName => action switch
        {
            MouseAction.UP => buttonId + "_UP",
            MouseAction.DOWN => buttonId + "_DOWN",
            MouseAction.MOVE => "MOVE",
            MouseAction.WHEEL => "WHEEL" + (wheel > 0 ? "_UP" : wheel < 0 ? "_DOWN" : string.Empty),
            MouseAction.SCROLL => "SCROLL" + (wheel > 0 ? "_UP" : wheel < 0 ? "_DOWN" : string.Empty),
            _ => string.Empty
        };

        /// 可读的事件名，中文
        public string EventNameCn => action switch
        {
            MouseAction.UP => buttonId.ToCnString() + "抬起",
            MouseAction.DOWN => buttonId.ToCnString() + "按下",
            MouseAction.MOVE => "移动",
            MouseAction.WHEEL => "滚轮" + (wheel > 0 ? "上" : wheel < 0 ? "下" : string.Empty),
            MouseAction.SCROLL => "滚动" + (wheel > 0 ? "上" : wheel < 0 ? "下" : string.Empty),
            _ => string.Empty
        };
    }

    /// <summary>
    /// 注意：已废弃! 请使用事件`ICloudSync.OnTouches` 其参数类型为 <see cref="RemoteTouchesEvent"/>
    /// </summary>
    [System.Obsolete("已废弃! Use event `ICloudSync.OnTouches` instead. (with param `RemoteTouchesEvent`)")]
    public struct RemoteTouchEvent : IRemoteInputEvent
    {
        public IVirtualDevice device { get; set; }
        public string InputType => "Touch";

        public int touchId;

        public RemoteTouchPhase phase;

        /// `viewPosition` 左上角坐标 ( 0, 1 ) , 原点在左下角，值范围 0~1.0
        public Vector2 viewPosition;

        /// `position` 左上角坐标 ( 0, Screen.Height ) , 原点在左下角，值范围 0~Screen宽高
        public Vector2 position;

        public byte displayIndex;
    }

    /// <summary>
    /// 触摸事件
    /// </summary>
    public struct RemoteTouchesEvent : IRemoteInputEvent
    {
        public IVirtualDevice device { get; set; }

        public string InputType => "Touch";

        public byte displayIndex;

        /// 触摸touches数据列表
        public List<RemoteTouch> touches;
    }

    /// <summary>
    /// 触摸Touch（单个手指的数据）
    /// </summary>
    public struct RemoteTouch
    {
        /// 触摸手指id
        public int touchId;

        /// 触摸手指id。 等同于`touchId`，只是命名习惯不同。`fingerId`是按照传统的`UnityEngine.Touch`。
        public int fingerId => touchId;

        /// 触摸阶段类型。 枚举值按`UnityEngine.InputSystem.TouchPhase`对齐，推荐使用。
        public RemoteTouchPhase phase;

        /// 触摸动作类型。 枚举值按云游戏的定义。
        public TouchAction action;

        /// `viewPosition` 左上角坐标 ( 0, 1 ) , 原点 ( 0, 0 ) 在左下角，值范围 0~1.0
        public Vector2 viewPosition;

        /// `position` 左上角坐标 ( 0, Screen.Height ) , 原点在左下角，值范围 0~Screen宽高
        public Vector2Int position;
    }

    /// <summary>
    /// 触摸阶段类型。
    /// </summary>
    /// <remarks>
    /// 注：取值与 Unity InputSystem 的<see cref="UnityEngine.InputSystem.TouchPhase"/>对齐。
    /// 注：取值与 Unity Legacy 的 <see cref="UnityEngine.TouchPhase"/> 稍有不同。
    /// </remarks>
    public enum RemoteTouchPhase
    {
        None,
        Began,
        Moved,
        Ended,
        Canceled,
        Stationary,
    }

    /// <summary>
    /// 键盘事件
    /// </summary>
    public struct RemoteKeyboardEvent : IRemoteInputEvent
    {
        public IVirtualDevice device { get; set; }
        public string InputType => "Keyboard";

        /// 按键，枚举类型 `UnityEngine.KeyCode`
        public KeyCode key;

        /// 键盘动作类型，按下 或 抬起
        public KeyboardAction action;

        /// 键盘修饰键，目前支持判断 Shift 。
        /// <remarks>
        /// 其他修饰键受所在云端平台不同、目前不保证支持。
        /// </remarks>
        /// <example>
        /// 用法举例：判断是否按住了Shift： `modifiers.HasFlag(EventModifiers.Shift)`
        /// </example>
        public EventModifiers modifiers { get; set; }

        /// <summary>
        /// 当前输入的字符。 只包含 ASCII 有效字符。
        /// </summary>
        /// <remarks>
        /// 使用了云同步内置实现的从 <see cref="KeyCode"/> 转换为 `char`。
        /// 若要开启或关闭此功能，可修改设置 <see cref="IRemoteInput.EnableKeyboardCharacter"/>。
        /// </remarks>
        public char character { get; set; }
    }

    /// <summary>
    /// 键盘动作类型
    /// </summary>
    public enum KeyboardAction
    {
        DOWN = 0,
        UP = 1,
    }
}