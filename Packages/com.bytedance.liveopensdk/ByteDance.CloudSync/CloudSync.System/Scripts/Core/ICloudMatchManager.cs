using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ByteDance.CloudSync.MatchManager;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace ByteDance.CloudSync
{
    /// <summary>
    /// 匹配同玩管理器
    /// </summary>
    public interface ICloudMatchManager : IDisposable
    {
        /// <summary>
        /// 请求匹配，使用系统自带提供的匹配池。 成功后进入多人同玩状态。
        /// </summary>
        /// <param name="config">简单匹配配置，选择一个系统自带提供的匹配池</param>
        /// <param name="cancelToken">可选，可用`CancellationTokenSource.Cancel()`取消（停止继续寻找匹配对手）</param>
        Task<IMatchResult> RequestMatch(SimpleMatchConfig config, CancellationToken cancelToken = default);

        /// <summary>
        /// 请求匹配。 成功后进入多人同玩状态。
        /// </summary>
        /// <param name="config">匹配配置，指定匹配服务id、匹配池等信息</param>
        /// <param name="matchParamJson">Json格式的匹配参数，默认可填空字符串。 若使用，内容应当与匹配规则配置的字段一致</param>
        Task<IMatchResult> RequestMatch(MatchConfig config, string matchParamJson = "");

        /// <summary>
        /// 请求匹配，可用参数取消（停止继续寻找匹配对手）。 成功后进入多人同玩状态。
        /// </summary>
        /// <param name="config">匹配配置，指定匹配服务id、匹配池等信息</param>
        /// <param name="matchParamJson">Json格式的匹配参数，默认可填空字符串。 若使用，内容应当与匹配规则配置的字段一致</param>
        /// <param name="cancelToken">可用`CancellationTokenSource.Cancel()`取消（停止继续寻找匹配对手）</param>
        Task<IMatchResult> RequestMatch(MatchConfig config, string matchParamJson, CancellationToken cancelToken);

        /// <summary>
        /// 结束同玩：所有人回到单播状态。
        /// </summary>
        /// <param name="endInfo">结束信息，用json透传，传递给同玩的用户</param>
        /// <remarks>
        /// 举例：A是房主Host，B,C,D是加入的同玩玩家。 则A调用`EndMatchGame`时，B,C,D断开从A实例的拉流、退出同玩、回到单播状态。 A在上述操作完成后，也就进入了单播状态。
        /// </remarks>
        Task<IEndResult> EndMatchGame(string endInfo = "");

        /// <summary>
        /// 结束同玩：所有人回到单播状态。可以给每个用户分别发送不同的结束信息。
        /// </summary>
        /// <param name="infoMapping">结束信息映射，可以给每个用户分别发送不同的结束信息</param>
        Task<IEndResult> EndMatchGame(InfoMapping infoMapping);

        /// <summary>
        /// 单个用户结束同玩：使他退出、回到单播状态。
        /// </summary>
        /// <param name="seatIndex">座位号 （对应<see cref="ICloudClient.Index"/>序号 0,1,2,3）</param>
        /// <param name="endInfo">结束信息，用json透传，传递给同玩的用户</param>
        /// <remarks>
        /// 举例：A是房主Host，B,C,D是加入的同玩玩家。 则A调用、或B调用`EndMatchGame`传入B的index时，B断开从A实例的拉流、退出同玩、回到单播状态。 注意：其中B此时只是输入是自己发起、而逻辑也是执行在A实例上的。
        /// </remarks>
        Task<IEndResult> EndMatchGame(SeatIndex seatIndex, string endInfo = "");

        // ReSharper disable InvalidXmlDocComment
        /// <summary>
        /// 事件：匹配到了用户
        /// </summary>
        /// <param name="matchResult">匹配结果</param>
        /// <remarks>
        /// * 包含本次成局的所有队伍、所有用户的信息  <br/>
        /// * 发起匹配的每个实例，都会分别收到  <br/>
        /// * 注意：还没有完成切流连接
        /// </remarks>
        event MatchUsersHandler OnMatchUsers;

        /// <summary>
        /// 事件：结束同玩了（返回单播）
        /// [Obsolete] 已废弃！请使用新的事件 <see cref="OnEndMatchEvent"/>, 事件参数为<see cref="IEndEvent"/>.
        /// </summary>
        /// <param name="endInfo">结束同玩时传递的结束信息</param>
        /// <remarks>
        /// * 例如：调用单个用户结束同玩 <see cref="ICloudMatchManager.EndMatchGame(int,string)"/> 使得该用户回流到自己实例时，会收到此事件，还会携带`endInfo`信息
        /// * 例如：调用结束同玩 <see cref="ICloudMatchManager.EndMatchGame(string)"/> 使得所有用户回流到单播时，每个用户都会收到此事件，其中房主收到时参数`endInfo`为null，同玩玩家收到时会携带`endInfo`信息
        /// </remarks>
        [System.Obsolete("已废弃! Use new event: `OnEndMatchEvent` instead.", true)]
        public event EndMatchGameHandler OnEndMatchGame;

        /// <summary>
        /// 事件：结束同玩了（返回单播）
        /// 参数 <see cref="IEndEvent"/> `endEvent` 结束同玩事件
        /// </summary>
        /// <remarks>
        /// * 例如：调用单个用户结束同玩 <see cref="ICloudMatchManager.EndMatchGame(int,string)"/> 使得该用户回流到自己实例时，会收到此事件，还会携带<see cref="IEndEvent.EndInfo"/>信息. <br/>
        /// * 例如：调用结束同玩 <see cref="ICloudMatchManager.EndMatchGame(string)"/> 使得所有用户回流到单播时，每个用户都会收到此事件，其中房主收到时参数<see cref="IEndEvent.EndType"/>为<see cref="EndEventType.EndMatchGame"/>，同玩玩家收到时会携带<see cref="IEndEvent.EndInfo"/>信息
        /// </remarks>
        event EndMatchEventHandler OnEndMatchEvent;
    }

    /// <summary>
    /// 简单匹配配置，选择一个系统自带提供的匹配池
    /// </summary>
    public class SimpleMatchConfig
    {
        /// <summary>
        /// 选择匹配池，对应一套匹配规则
        /// </summary>
        public SimpleMatchPoolType PoolType;

        /// <summary>
        /// 匹配标签，相同的才能匹配到一起。
        /// </summary>
        /// <remarks>
        /// 建议用不同值区分开测试环境"test"、线上环境"online"、区分开不同版本"1.0","2.0"，
        /// Example: 例如："test-1.0", "online-2.0"。
        /// 也可以用于开发者自行选定特定用户，让他们匹配到同一局，类似邀请码，
        /// Example: 例如先由游戏服务器生成唯一的字符串，发送给指定的几个用户，他们 MatchTag 使用该字符串，使得只有他们会匹配到一起。
        /// </remarks>
        public string MatchTag;
    }

    /// <summary>
    /// 简单匹配池，系统自带提供
    /// </summary>
    public enum SimpleMatchPoolType
    {
        /// 2人 1v1
        P1v1,
        /// 4人 2v2
        P2v2,
        /// 4人各自1队
        P1x4,
        /// 3人各自1队
        P1x3,
    }

    /// <summary>
    /// 匹配配置，指定匹配服务id、匹配池等信息
    /// </summary>
    public class MatchConfig
    {
        /// <summary>
        /// 匹配appid，是匹配服务分配给到每个业务的独立appid
        /// </summary>
        public int MatchAppId;

        /// <summary>
        /// 匹配池子名，对应一套匹配规则
        /// </summary>
        public string PoolName;

        /// <summary>
        /// 匹配标签，相同的才能匹配到一起。
        /// </summary>
        /// <remarks>
        /// 建议用不同值区分开测试环境"test"、线上环境"online"、区分开不同版本"1.0","2.0"，
        /// Example: 例如："test-1.0", "online-2.0"。
        /// 也可以用于开发者自行选定特定用户，让他们匹配到同一局，类似邀请码，
        /// Example: 例如先由游戏服务器生成唯一的字符串，发送给指定的几个用户，他们 MatchTag 使用该字符串，使得只有他们会匹配到一起。
        /// </remarks>
        public string MatchTag;
    }

    /// <summary>
    /// 事件：匹配到了用户
    /// </summary>
    /// <param name="matchResult">匹配结果</param>
    public delegate void MatchUsersHandler(IMatchResult matchResult);

    /// <summary>
    /// 匹配结果
    /// </summary>
    public interface IMatchResult
    {
        /// 是否成功
        bool IsSuccess { get; }

        /// 返回码
        MatchResultCode Code { get; }

        /// 返回信息、错误信息
        string Message { get; }

        /// 标识一场匹配的id，一般是匹配成功时由服务端给出
        string MatchId { get; }

        /// 我是否房主
        /// <remarks>
        /// 举例：true - 我是匹配后的房主Host； false - 我是同玩玩家，连接到房主的实例。
        /// </remarks>
        bool IsHost { get; }

        /// 我的座位号 0,1,2,3
        /// <remarks>
        /// 举例： 当我匹配成为房主, 座位号 0； 当我匹配成为玩家，分配到座位号 >= 1，以该座位号加入到房主实例。 参考<see cref="ICloudClient.Index"/>。
        /// </remarks>
        SeatIndex MyIndex { get; }

        /// 房主用户
        MatchResultUser HostUser { get; }

        /// Team列表
        /// <remarks>
        /// * 成功时，`Teams`为有效值  <br/>
        /// * 举例：1v1匹配PK，则返回2个Team，每个Team有1个User。  <br/>
        /// * 举例：4个人匹配PK（各自为阵），则返回4个Team，每个Team有1个User； 2v2匹配PK（分配到两组对战），则返回2个Team，每个Team有2个User。
        /// </remarks>
        List<MatchResultTeam> Teams { get; }

        /// 用户列表
        List<MatchResultUser> AllUsers => Teams.SelectMany(t => t.Users).ToList();
    }

    /// <summary>
    /// 匹配结果的返回码
    /// </summary>
    public enum MatchResultCode
    {
        /// 成功
        Success = 0,

        /// 已取消。（即：取消匹配成功）
        Cancelled,

        /// 超时。（即：超过了一定时间，没有匹配到）
        Timeout,

        /// 错误。（例如发生网络错误，或切流失败等）
        Error,

        /// 当前状态无效。（例如：已经在房主状态，或已切流加入到了他人房间）
        InvalidStateError,
    }

    /// <summary>
    /// 匹配结果的Team
    /// </summary>
    public class MatchResultTeam
    {
        public List<MatchResultUser> Users;
    }

    /// <summary>
    /// 匹配结果的User
    /// </summary>
    public class MatchResultUser
    {
        /// 座位号 0,1,2,3 ： 0为主Host，1,2,3为加入的同玩玩家
        public SeatIndex RoomIndex;

        /// 开放平台open_id
        public string OpenId;

        /// 头像
        public string AvatarUrl;

        /// 昵称
        public string Nickname;

        /// 直播间roomId
        public string LiveRoomId;

        /// 客户端透传信息（仅内部）
        internal string ExtraInfo;

        /// 云游戏流信息（仅内部）
        internal MatchCloudGameInfo CloudStreamInfo => _cloudGameInfo ??= JsonUtility.FromJson<MatchCloudGameInfo>(ExtraInfo);

        private MatchCloudGameInfo _cloudGameInfo;

        public AnchorPlayerInfo ToPlayerInfo()
        {
            return new AnchorPlayerInfo
            {
                openId = OpenId,
                nickName = Nickname,
                avatarUrl = AvatarUrl,
                liveRoomId = LiveRoomId,
                liveRoomToken = CloudStreamInfo.roomToken,
            };
        }
    }

    /// <summary>
    /// 结束同玩结果
    /// </summary>
    public interface IEndResult
    {
        /// 是否成功
        bool IsSuccess { get; }

        /// 返回码
        EndResultCode Code { get; }

        /// 返回信息、错误信息
        string Message { get; }

        /// 每个座位的结束同玩回包
        IEndSeatResponse[] SeatResponses { get; }
    }

    /// <summary>
    /// 单个座位的结束同玩回包
    /// </summary>
    public interface IEndSeatResponse
    {
        /// 座位号
        SeatIndex RoomIndex { get; }

        /// 是否成功
        bool IsSuccess { get; }

        /// 返回码
        EndResultCode Code { get; }

        /// 返回信息、错误信息
        string Message { get; }

        string LogId { get; }
    }

    /// <summary>
    /// 结束同玩的返回码
    /// </summary>
    public enum EndResultCode
    {
        /// 成功
        Success = 0,

        /// 超时
        Timeout,

        /// 错误
        Error,

        /// 当前状态无效（例如：不在房主状态）
        InvalidStateError,
    }

    /// <summary>
    /// 信息映射，可以给每个玩家设置不同的信息。 可用于<see cref="IHostRoom.End(InfoMapping)"/>
    /// </summary>
    public class InfoMapping
    {
        public InfoMapping()
        {
        }

        /// <summary>
        /// 构造信息映射，用映射函数设置每个玩家的信息
        /// </summary>
        public InfoMapping(Func<SeatIndex, string> infoGetter) => SetInfo(infoGetter);

        /// <summary>
        /// 设置每个玩家的信息，用映射函数
        /// </summary>
        public void SetInfo(Func<SeatIndex, string> infoGetter)
        {
            for (var i = SeatIndex.Index0; i <= SeatIndex.MaxIndex3; i++)
                SetInfo(i, infoGetter.Invoke(i));
        }

        /// <summary>
        /// 设置单个玩家的信息
        /// </summary>
        public void SetInfo(SeatIndex index, string info)
        {
            _data[index] = info;
        }

        public string GetInfo(SeatIndex index) => _data.GetValueOrDefault(index, null);
        public Dictionary<SeatIndex, string> Data => _data;
        public override string ToString() => $"{_data.Count} infos for [{string.Join(",", _data.Keys.Select(s => (int)s))}]";

        private readonly Dictionary<SeatIndex, string> _data = new();
    }

    /// <summary>
    /// 结束同玩事件. 参考事件：<see cref="ICloudMatchManager.OnEndMatchEvent"/>, <see cref="ICloudSwitchManager.OnEndMatchEvent"/>
    /// </summary>
    public interface IEndEvent
    {
        /// 结束事件类型
        EndEventType EndType { get; }

        /// 结束信息
        string EndInfo { get; }

        bool HasEndInfo();

        /// 发送者Id，通常是openId
        string FromId { get; }
    }

    /// <summary>
    /// 结束同玩事件类型. 参考事件：<see cref="ICloudMatchManager.OnEndMatchEvent"/>, <see cref="ICloudSwitchManager.OnEndMatchEvent"/>
    /// </summary>
    public enum EndEventType
    {
        /// 结束同玩了（结束同玩时，玩家B回流到自己实例，会收到该事件，且事件会携带结束信息 EndInfo）
        EndMatchGame,

        /// 结束房主状态了（房主A结束同玩，相关操作结束后，房主自己收到该事件）
        EndHostState,

        /// 退出同玩了，原因：加入失败（例如：对方不在房主状态，或同玩场次不匹配）
        ExitForJoinError,

        /// 退出同玩了，原因：同玩已失效（例如：房主异常中断了同玩、没有正常结束同玩）
        ExitForGameExpired,
    }
}