using System.Threading.Tasks;

namespace ByteDance.CloudSync
{
    public delegate void SeatEventHandler(ICloudSeat seat);

    public delegate void MouseEventHandler(ICloudSeat seat, RemoteMouseEvent e);

    public delegate void KeyboardEventHandler(ICloudSeat seat, RemoteKeyboardEvent e);

    public delegate void TouchEventHandler(ICloudSeat seat, RemoteTouchEvent touchEvent);

    /// <summary>
    /// 事件：触摸操作（移动端）
    /// </summary>
    /// <param name="seat">发生该操作的用户座位</param>
    /// <param name="touchesEvent">远端触摸事件，包含touches触摸数据</param>
    public delegate void TouchesEventHandler(ICloudSeat seat, RemoteTouchesEvent touchesEvent);

    /// <summary>
    /// 事件：结束同玩了（返回单播）
    /// </summary>
    /// <param name="endInfo">结束同玩时传递的结束信息</param>
    /// <remarks>已废弃! 请使用新的 <see cref="EndMatchEventHandler"/>, 事件参数为 <see cref="IEndEvent"/> `endEvent` </remarks>
    [System.Obsolete("已废弃! Use `EndMatchEventHandler` instead.", true)]
    public delegate void EndMatchGameHandler(string endInfo);

    /// <summary>
    /// 事件：结束同玩了（返回单播）
    /// </summary>
    /// <param name="endEvent">结束同玩事件</param>
    public delegate void EndMatchEventHandler(IEndEvent endEvent);

    /// <summary>
    /// 事件：云游戏实例即将在倒计时 x 秒后销毁
    /// </summary>
    public delegate void WillDestroyHandler(DestroyInfo info);

    /// <summary>
    /// 云游戏实例即将销毁的信息
    /// </summary>
    public struct DestroyInfo
    {
        /// <summary>
        /// 在 x 秒后销毁
        /// </summary>
        public int Time;

        /// <summary>
        /// 原因
        /// </summary>
        public DestroyReason Reason;
    }

    /// <summary>
    /// 云游戏实例即将销毁原因
    /// </summary>
    public enum DestroyReason
    {
        /// <summary>
        /// Host 玩家离开
        /// </summary>
        HostAnchorLeave
    }

    public interface ICloudSync
    {
        /// <summary>
        /// 获取或者创建 ICloudSyncSdk
        /// </summary>
        public static ICloudSync Instance => CloudSyncSdk.GetInstance();

        /// <summary>
        /// 当前云游戏环境信息
        /// </summary>
        public static ICloudSyncEnv Env => CloudSyncSdk.Env;

        public string Version { get; }

        /// <summary>
        /// 是否已经初始化
        /// </summary>
        public bool IsInitialized { get; }

        /// <summary>
        /// 侦听某个 Client 设备（VirtualDevice）的鼠标事件
        /// </summary>
        event MouseEventHandler OnMouse;

        /// <summary>
        /// 侦听某个 Client 设备（VirtualDevice）的键盘事件
        /// </summary>
        event KeyboardEventHandler OnKeyboard;

        /// <summary>
        /// 侦听某个 Client 设备（VirtualDevice）的触摸事件. 注意：已废弃! 请使用<see cref="ICloudSync.OnTouches"/>事件，其参数类型为<see cref="RemoteTouchesEvent"/>
        /// </summary>
        [System.Obsolete("已废弃! Use event `ICloudSync.OnTouches` instead. (with param `RemoteTouchesEvent`)", true)]
        event TouchEventHandler OnTouch;

        /// <summary>
        /// 侦听某个 Client 设备（VirtualDevice）的触摸事件
        /// </summary>
        event TouchesEventHandler OnTouches;

        /// <summary>
        /// 当有玩家加入了此座位
        /// </summary>
        event SeatEventHandler OnSeatPlayerJoined;

        /// <summary>
        /// 当玩家正在离开此座位
        /// </summary>
        event SeatEventHandler OnSeatPlayerLeaving;

        /// <summary>
        /// event: 云游戏实例即将在倒计时 x 秒后销毁
        /// </summary>
        event WillDestroyHandler OnWillDestroy;

        /// <summary>
        /// “座位” 管理器
        /// </summary>
        ICloudSeatManager SeatManager { get; }

        /// <summary>
        /// 匹配同玩管理器
        /// </summary>
        ICloudMatchManager MatchManager { get; }

        /// <summary>
        /// 云游戏切流管理
        /// </summary>
        ICloudSwitchManager SwitchManager { get; }

        /// <summary>
        /// 初始化云游戏环境
        /// </summary>
        /// <param name="appId">小玩法appId。 形如'tt123456abcd1234'</param>
        /// <param name="deviceFactory">必须，不可为null。指定 DeviceFactory 来创建云同步虚拟设备，用来适配 UGUI/FGUI 或者自定义主播界面渲染。</param>
        /// <param name="playerInfoProvider">可选，默认null。自定义设置主播用户信息的提供者。主播用户包含例如：NickName, OpenId, AvatarUrl, LiveRoomId</param>
        /// <param name="splash"></param>
        /// <returns></returns>
        Task<InitResult> Init(string appId,
            IVirtualDeviceFactory deviceFactory,
            IAnchorPlayerInfoProvider playerInfoProvider = null,
            ISplashScreen splash = null);
    }

    /// <summary>
    /// 云游戏初始化结果状态码
    /// </summary>
    public enum InitResultCode
    {
        Success,
        Failed,
    }

    /// <summary>
    /// 云游戏初始化结果
    /// </summary>
    public struct InitResult
    {
        public InitResultCode Code;
        public string Message;

        public bool IsSuccess => Code == InitResultCode.Success;

        public string ToStr() => $"{Code} ({(int)Code}) {Message}";
    }
}