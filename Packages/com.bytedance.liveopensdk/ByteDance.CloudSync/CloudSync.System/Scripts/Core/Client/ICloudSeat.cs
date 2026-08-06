// Copyright (c) Bytedance. All rights reserved.
// Description:

using System.Threading;
using System.Threading.Tasks;
using ByteDance.LiveOpenSdk.Push;

namespace ByteDance.CloudSync
{
    /// <summary>
    /// 座位状态
    /// </summary>
    public enum SeatState
    {
        /// <summary>
        /// 空闲
        /// </summary>
        Empty,

        /// <summary>
        /// 在使用中（有玩家）
        /// </summary>
        InUse
    }

    /// <summary>
    /// 云座位，一个云同步能力的游戏最可以支持 4 个人同玩。座位号对应 0 ~ 3。
    /// <seealso cref="SeatIndex"/>
    /// </summary>
    public interface ICloudSeat
    {
        /// <summary>
        /// 当有玩家加入了此座位
        /// </summary>
        event SeatEventHandler OnSeatPlayerJoined;

        /// <summary>
        /// 当玩家正在离开此座位
        /// </summary>
        event SeatEventHandler OnSeatPlayerLeaving;

        /// <summary>
        /// 云游戏实例即将在倒计时 x 秒后销毁
        /// </summary>
        event WillDestroyHandler OnWillDestroy;

        /// <summary>
        /// 座位号
        /// </summary>
        SeatIndex Index { get; }

        /// <summary>
        /// 座位号
        /// </summary>
        int IntIndex { get; }

        /// <summary>
        /// 当前座位状态
        /// </summary>
        SeatState State { get; }

        /// <summary>
        /// 这个座位上的用户信息。<br/>
        /// 注意：只有 <see cref="State"/> 为 SeatState.InUse 时才有 PlayerInfo 对象，否则为 null。
        /// <seealso cref="OnSeatPlayerJoined"/>
        /// </summary>
        IPlayerInfo PlayerInfo { get; }

        /// <summary>
        /// 启动此座位对应直播间的互动指令推送服务。该服务可以直接接收直播间内的送礼、点赞、评论等互动指令。
        /// 注意：只有 State 为 SeatState.InUse 时才有 PushService 对象，否则为 null。
        /// 注意：启动推送失败会抛异常Exception
        /// <seealso cref="IMessagePushService"/>
        /// <seealso cref="OnSeatPlayerJoined"/>
        /// </summary>
        /// <remarks>
        /// 如果有对该用户启动过指令消息直推，在该用户离开<see cref="ICloudView.OnPlayerLeaving"/>之后，系统会自动停止和清除其直推。
        /// </remarks>
        /// <exception cref="System.Exception">异常，启动推送失败</exception>
        Task<IMessagePushService> StartPushService();

        /// <summary>
        /// 此座位对应的虚拟设备对象。<br/>
        /// 注意：只有 State 为 SeatState.InUse 时才有 Device 对象，否则为 null。
        /// <seealso cref="IVirtualDevice"/>
        /// </summary>
        IVirtualDevice Device => Client?.Device;

        /// <summary>
        /// “坐”在这个座位上的玩家可以看到的视图对象，通过 ICloudViewProvider 创建提供。<br/>
        /// 注意：只有 State 为 SeatState.InUse 时才有 View 对象，否则为 null。
        /// <seealso cref="ICloudViewProvider{T}"/>
        /// <seealso cref="ICloudView"/>
        /// <seealso cref="IVirtualDeviceFactory"/>
        /// </summary>
        ICloudView View { get; }

        /// <summary>
        /// 单个用户结束同玩：使他退出、回到单播状态。
        /// </summary>
        /// <param name="endInfo">结束信息。回到原实例时需要透传的信息</param>
        void EndMatchGame(string endInfo = null);

        /// <summary>
        /// 等待玩家加入
        /// </summary>
        /// <param name="token">取消令牌</param>
        /// <returns></returns>
        Task WaitJoin(CancellationToken token);


        internal CloudClient Client { get; }

        internal string RtcUserId { get; }
    }
}