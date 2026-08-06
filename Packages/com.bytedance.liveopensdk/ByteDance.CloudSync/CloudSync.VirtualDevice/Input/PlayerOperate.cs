// Copyright@www.bytedance.com
// Author: Admin
// Date: 2024/06/07
// Description:

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace ByteDance.CloudSync
{
    // todo: refactor 改为 internal
    public class PlayerOperate
    {
        public OperateType op_type;

        public JToken event_data;

        public string ToStr() => $"op_type: {op_type} ({(int)op_type}), event_data: {event_data.ToString(Formatting.None)}";
    }

    /// <summary>
    /// op_type 的 EVENT_TYPE
    /// @see: DataChannel协议 #定义 https://bytedance.larkoffice.com/wiki/HnzuwUfCFiRISjki9Goc6iUpnFh#doxcnZEYX1aWhoGPxSbTsgig2Zd
    /// </summary>
    public enum OperateType
    {
        TOUCH = 0, // 触摸
        MOBILE_KEY = 1, // 手游版按键、键盘
        GDAP_JOYSTICK = 10, // 游戏手柄摇杆
        PC_KEYBOARD = 13, // 用户键盘操作数据
        MOUSE = 14, // 用户鼠标操作数据
    }

    // MARK: - touch

    /// <summary>
    /// 触摸事件, 对应事件类型：`<see cref="OperateType.TOUCH"/>` "op_type": EVENT_TYPE_TOUCH 0
    /// @see: GameService之DataChannel协议 #触摸事件 https://bytedance.larkoffice.com/wiki/HnzuwUfCFiRISjki9Goc6iUpnFh#doxcnw0DHHtuVGdeUklJaFMreYe
    /// </summary>
    public partial class CloudTouchData
    {
        /// "x": double类型。 x,y 左上角坐标 ( 0, 0 ) 原点在左上角，值范围 0~1.0
        public double x;

        /// "y": double类型。 x,y 左上角坐标 ( 0, 0 ) 原点在左上角，值范围 0~1.0
        public double y;

        /// Touch动作类型 see: <see cref="TouchAction"/>
        /// 多指 pointerId 非 0 时，`action`会大于256，换算 see: <see cref="ToDataAction(ByteDance.CloudSync.TouchAction,int)"/>
        public int action;

        public int pointerId;
    }

    /// <summary>
    /// Touch动作类型
    /// </summary>
    public enum TouchAction
    {
        DOWN = 0,
        UP,
        MOVE,
        CANCEL,
        OUTSIDE,
        // 多根手指按下 index << 8 | MULTI_TOUCH_DOWN
        MULTI_DOWN,
        // 多根手指按下 index << 8 | MULTI_TOUCH_UP
        MULTI_UP,
    }

    // MARK: - mouse

    /// <summary>
    /// 云端鼠标事件, 对应事件类型：`<see cref="OperateType.MOUSE"/>` "op_type": EVENT_TYPE_MOUSE 14
    /// 文档： @see: GameService之DataChannel协议 #鼠标Android事件 https://bytedance.larkoffice.com/wiki/HnzuwUfCFiRISjki9Goc6iUpnFh#doxcnDZmfH59yS3K5tU0TJguDrb
    /// </summary>
    public class CloudMouseData
    {
        /// double类型。 x,y 左上角坐标 ( 0, 0 ) 原点在左上角，值范围 0~1.0
        public double x;

        /// double类型。 x,y 左上角坐标 ( 0, 0 ) 原点在左上角，值范围 0~1.0
        public double y;

        /// 动作类型，如0-ACTION_DOWN, 1-ACTION_UP, 2-ACTION_MOVE 8-ACTION_SCROLL（滚轮）
        /// 兼容旧版本： 3:滚轮 （等同于 8 SCROLL）
        public MouseAction action;

        /// double类型 表示上下方向，-1.0 下 1.0 上
        public double axis_v;

        /// double类型 表示左右方向，-1.0 右 1.0 左
        public double axis_h;

        /// int 0:左键， 1:右键， 2:中间键
        public MouseButtonId button;

        /// 0:鼠标位置为绝对值， 1:鼠标位为相对值
        public MousePositionState state;

        /// 兼容旧版本：滚轮  -1.0 下， 1.0 上
        public double wheel;
    }

    public enum MousePositionState
    {
        ABSOLUTE,
        RELATIVE
    }

    /// <summary>
    /// Mouse动作类型，如0-ACTION_DOWN, 1-ACTION_UP, 2-ACTION_MOVE 8-ACTION_SCROLL（滚轮）
    /// 兼容旧版本： 3:滚轮 （等同于 8 SCROLL）
    /// </summary>
    public enum MouseAction
    {
        DOWN = 0,
        UP = 1,
        MOVE = 2,
        WHEEL = 3,
        SCROLL = 8
    }

    /// <summary>
    /// Mouse按键id，0:左键， 1:右键， 2:中间键
    /// </summary>
    public enum MouseButtonId
    {
        LEFT = 0,
        RIGHT,
        MIDDLE
    }

    // MARK: - keyboard

    /// 手游版按键、键盘操作事件, 对应事件类型：`<see cref="OperateType.MOBILE_KEY"/>` "op_type": EVENT_TYPE_KEY 1
    /// 文档： @see: GameService之DataChannel协议 #按键事件 https://bytedance.larkoffice.com/wiki/HnzuwUfCFiRISjki9Goc6iUpnFh#doxcnYXw6Gp730iSqgm8nzhmTte
    internal class CloudMobileKeyData
    {
        /// <summary>
        /// 动作类型，如0-ACTION_DOWN，1-ACTION_UP
        /// </summary>
        public MobileKeyAction action;

        /// <summary>
        /// 键值. 协议 key_code参考 @see: https://bytedance.larkoffice.com/wiki/HnzuwUfCFiRISjki9Goc6iUpnFh#doxcnYXw6Gp730iSqgm8nzhmTte
        /// </summary>
        public int key_code;

        // 可忽略，仅当key_code为-1时有效，直接在当前编辑框插入该文本内容，需使用URLEncoder转换，utf8编码，此时action可不传值，不区分down/up
        // public string value;
    }

    /// 0-ACTION_DOWN，1-ACTION_UP
    internal enum MobileKeyAction
    {
        DOWN = 0,
        UP = 1,
    }

    /// PC版键盘操作事件, 对应事件类型：`<see cref="OperateType.PC_KEYBOARD"/>` 13
    /// 文档： @see: GameService之DataChannel协议 #键盘操作事件 https://bytedance.larkoffice.com/docs/doccnbOGIZYxlN6v31cOiYXbM7b#3za5Iz
    internal class CloudPCKeyboardData
    {
        /// <summary>
        /// 0:key_down, 1:key_up
        /// </summary>
        public PCKeyboardAction action;

        /// <summary>
        /// 键盘虚拟键码
        /// </summary>
        public int key_code;
    }

    /// 0:key_down, 1:key_up
    public enum PCKeyboardAction
    {
        DOWN = 0,
        UP = 1,
    }
}