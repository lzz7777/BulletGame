// Copyright@www.bytedance.com
// Author: Admin
// Date: 2024/07/16
// Description:

namespace ByteDance.CloudSync
{

    /// <summary>
    /// 动作类型 0:click_down, 1:click_up 2:移动  3:滚轮
    /// </summary>
    [System.Obsolete]
    public enum CloudMouseActionTypeV1
    {
        ClickDown = 0,
        ClickUp = 1,
        Move = 2,
        Wheel = 3
    }

    [System.Obsolete]
    public class CloudMouseDataV1
    {
        // "x": double  //相对屏幕位置左上角x轴  0.00000 - 1.00000
        // "y": double  //相对屏幕位置左上角y轴  0.00000 - 1.00000
        public double x;

        public double y;
        //  action": int // 0:click_down, 1:click_up  2:移动  3:滚轮
        public CloudMouseActionTypeV1 action;
        //         "button": int // 0:左键， 1:右键， 2:中间键
        public MouseButtonId button;
        //         "wheel": int  //滚轮值
        public int wheel;

        // 0:鼠标位置为相对位置， 1:鼠标位置为绝对位置
        public int state;

        // "delta_x": double //鼠标移动时的x轴相对变化
        // "delta_y": double //鼠标移动时的y轴相对变化
        public double delta_x;

        public double delta_y;
    }
}