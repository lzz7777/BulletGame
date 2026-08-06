// Copyright (c) Bytedance. All rights reserved.
// Description:

namespace ByteDance.CloudSync
{
    /// <summary>
    /// 玩家视图界面，请参考并使用 UCloudView
    /// </summary>
    public interface ICloudView
    {
        /// <summary>
        /// 事件回调：当 ICloudSeat 初始化完成，并且有玩家加入了此座位
        /// </summary>
        /// <param name="seat">对应的座位</param>
        void OnPlayerJoined(ICloudSeat seat);

        /// <summary>
        /// 事件回调：当玩家正在离开此座位
        /// </summary>
        /// <param name="seat">对应的座位</param>
        void OnPlayerLeaving(ICloudSeat seat);

        /// <summary>
        /// 事件回调：云游戏实例即将在倒计时 x 秒后销毁
        /// </summary>
        void OnWillDestroy(ICloudSeat seat, DestroyInfo destroyInfo);
    }

    /// <summary>
    /// 玩家视图界面提供者，根据 index 创建 ICloudView 界面。<br/>
    /// 可参考并使用 Demo 的 CloudViewProvider
    /// </summary>
    public interface ICloudViewProvider<out T> where T : ICloudView
    {
        /// <summary>
        /// 根据座位号创建不同的视图界面。
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        T CreateView(SeatIndex index);
    }
}