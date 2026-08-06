using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace ByteDance.CloudSync
{
    /// <summary>
    /// 云游戏切流管理
    /// </summary>
    public interface ICloudSwitchManager
    {
        /// <summary>
        /// event: 结束同玩了（返回单播）
        /// [Obsolete] 已废弃！请使用新的事件 <see cref="OnEndMatchEvent"/>, 事件参数为<see cref="IEndEvent"/>.
        /// </summary>
        /// <param name="endInfo">结束同玩时从 Host 端透传的结束信息</param>
        /// <remarks>
        /// * 例如：调用单个用户结束同玩 <see cref="IHostRoom.Kick"/> 使得该用户回流到自己实例时，会收到此事件，还会携带`endInfo`信息
        /// * 例如：调用结束同玩 <see cref="IHostRoom.End"/> 使得所有用户回流到单播时，每个用户都会收到此事件，其中房主收到时参数`endInfo`为null，同玩玩家收到时会携带`endInfo`信息
        /// </remarks>
        [System.Obsolete("已废弃! Use new event: `OnEndMatchEvent` instead.", true)]
        public event EndMatchGameHandler OnEndMatchGame;

        /// <summary>
        /// event: 结束同玩了（返回单播）
        /// 参数 <see cref="IEndEvent"/> `endEvent` 结束同玩事件
        /// </summary>
        /// <remarks>
        /// * 例如：调用单个用户结束同玩 <see cref="IHostRoom.Kick"/> 使得该用户回流到自己实例时，会收到此事件，还会携带<see cref="IEndEvent.EndInfo"/>信息. <br/>
        /// * 例如：调用结束同玩 <see cref="IHostRoom.End(string)"/> 使得所有用户回流到单播时，每个用户都会收到此事件，其中房主收到时参数<see cref="IEndEvent.EndType"/>为<see cref="EndEventType.EndMatchGame"/>，同玩玩家收到时会携带<see cref="IEndEvent.EndInfo"/>信息
        /// </remarks>
        event EndMatchEventHandler OnEndMatchEvent;

        /// <summary>
        /// 本云游戏实例的 HostToken
        /// </summary>
        string Token { get; }

        /// <summary>
        /// 本实例作为 Host，准备接收其它端连入
        /// </summary>
        /// <param name="tokenProvider">其它端 Token 提供器</param>
        /// <returns></returns>
        IHostRoom BeginHost(ICloudSwitchTokenProvider tokenProvider);

        /// <summary>
        /// 作为 Client 切流到指定的 Host 房主云游戏实例上，接收 Host 画面
        /// </summary>
        /// <param name="token">目标房主的 HostToken</param>
        /// <param name="myIndex">我的座位号。 我作为玩家切到目标 Host 房主实例上时，分配到指定的座位号。 应当不为0</param>
        /// <param name="matchKey">匹配唯一ID，调试用</param>
        /// <returns>切换成功与否，错误码</returns>
        Task<SwitchResult> SwitchTo(string token, SeatIndex myIndex, string matchKey = null);

        /// <summary>
        /// 当前实例调用 `BeginHost` 后的 IHostRoom 对象
        /// </summary>
        IHostRoom CurrentHost { get; }
    }

    public interface IHostRoom
    {
        /// <summary>
        /// 将指定座位上的 Client 踢回原实例，并可透传、携带额外信息。<br/>
        /// 可用于以下情形：<br/>
        /// 对局结束时，非 Host 主播想要结束匹配同玩状态回到单播时，可以使用该接口。即：在 Host 实例上将自己踢出 Host 房间。
        /// </summary>
        /// <param name="index">座位号</param>
        /// <param name="info">需要透传的信息</param>
        Task<IEndResult> Kick(SeatIndex index, string info);

        /// <summary>
        /// 结束当前同玩状态，并且将其它端都踢回原实例。<br/>
        /// 可用于以下情形：<br/>
        /// 对局结束时，Host 主播想要结束匹配同玩状态回到单播时，可以使用该接口。即：在 Host 实例上将除了自己的其它所有主播踢出房间
        /// </summary>
        /// <param name="info">将其它端都踢回原实例时需要透传的信息</param>
        Task<IEndResult> End(string info);

        /// <summary>
        /// 结束当前同玩状态，并且将其它端都踢回原实例。可以给每个用户分别发送不同的结束信息。
        /// </summary>
        /// <param name="endInfoMapping">结束信息映射，可以给每个用户分别发送不同的结束信息</param>
        Task<IEndResult> End(InfoMapping endInfoMapping);
    }

    /// <summary>
    /// 其它端 Token 提供器
    /// <seealso cref="ICloudSwitchManager.Token"/>
    /// </summary>
    public interface ICloudSwitchTokenProvider
    {
        /// <summary>
        /// SwitchToken 提供器：在 Host 端中，为 ICloudSwitchManager 提供指定座位号的 Switch Token 信息。
        /// </summary>
        /// <param name="seat">对应 ICloudClient</param>
        /// <param name="cancellationToken"></param>
        /// <seealso cref="ICloudSwitchManager.Token"/>
        /// <returns></returns>
        Task<string> GetToken(ICloudSeat seat, CancellationToken cancellationToken);
    }

    public enum SwitchResultCode
    {
        /// <summary>
        /// 成功
        /// </summary>
        Success,
        /// <summary>
        /// 传递的 Token 无效
        /// </summary>
        InvalidToken,
        /// <summary>
        /// 传递的座位号无效
        /// </summary>
        InvalidIndex,
        /// <summary>
        /// 当前状态无效（可能调用过 SwitchTo, BeginHost 方法）
        /// </summary>
        InvalidState,
        /// <summary>
        /// 切流失败
        /// </summary>
        Error,
        /// <summary>
        /// 被 Host 端拒绝
        /// </summary>
        Rejected,
    }

    /// <summary>
    /// 调用 SwitchTo 方法的返回结果
    /// </summary>
    public struct SwitchResult
    {
        public bool Success => Code == SwitchResultCode.Success;

        public SwitchResultCode Code;

        public string Message;
    }
}