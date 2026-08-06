using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ByteCloudGameSdk;
using Newtonsoft.Json;
using ByteDance.CloudSync.MatchManager;
using ByteDance.CloudSync.TeaSDK;
using UnityEngine;

namespace ByteDance.CloudSync
{
    internal interface ICloudSwitchManagerEx : ICloudSwitchManager, ICloudManager
    {
        SwitchState State { get; }
        // 内部监听切流动作
        event Action<IHostRoom> OnBeginHost;
        // 内部监听切流动作
        event Action<SwitchResult> OnSwitchTo;
    }

    internal interface IHostRoomEx : IHostRoom
    {
        string ID { get; }
        Task<AnchorPlayerInfo> FetchPlayerInfo(ICloudSeat seat, CancellationToken token);
        Task<ICloudGameAPI.Response> SendPodMessage(SeatIndex seatIndex, string message, MatchPodMessageType type);
    }

    internal interface IHostRoomProvider
    {
        IHostRoomEx CreateHostRoom(ICloudSwitchManager manager, ICloudSwitchTokenProvider tokenProvider);
    }

    internal class CloudSwitchManager : ICloudSwitchManagerEx, IMultiAnchorPlayerInfoProvider, IMatchAPIListener, ISafeActionsUpdatable, IHostRoomProvider, HostRoom.IHandler
    {
        public event Action<IHostRoom> OnBeginHost;
        public event Action<SwitchResult> OnSwitchTo;

        /// <summary>
        /// <inheritdoc cref="ICloudSwitchManager.CurrentHost"/>
        /// </summary>
        public IHostRoom CurrentHost => _currentHost;
        public SwitchState State => _state;
        internal IHostRoomProvider HostRoomProvider { get; set; }
        internal ICloudSyncEnv Env
        {
            get => _env ?? CloudSyncSdk.Env;
            set => _env = value;
        }

        private IHostRoomEx _currentHost;
        private SwitchState _state;
        private SafeMatchAPIListener _safeMatchAPIListener;
        private ICloudSyncEnv _env;

        private const string Tag = nameof(CloudSwitchManager);
        // 等待Host的最大超时
        internal const int MaxWaitTimeMs = 3000;
        private static readonly SdkDebugLogger Debug = new(Tag);

        public CloudSwitchManager()
        {
            Debug.Log($"CloudSwitchManager ctor, frame: {Time.frameCount}");
            HostRoom.Debug = Debug;
        }

        public void Initialize()
        {
            Debug.Log($"CloudSwitchManager Initialize, frame: {Time.frameCount}");
            InitMatchListener();
        }

        private void InitMatchListener()
        {
            _safeMatchAPIListener = new SafeMatchAPIListener(this);
            CloudSyncSdk.InternalCurrent.AddUpdatable(this);
            CloudGameSdk.API.InitMatchAPI(_safeMatchAPIListener);
        }

        public void Dispose()
        {
            Debug.Log("CloudSwitchManager Dispose");
        }

        [System.Obsolete("已废弃! Use new event: `OnEndMatchEvent` instead.", true)]
        public event EndMatchGameHandler OnEndMatchGame;

        /// <inheritdoc cref="ICloudSwitchManager.OnEndMatchEvent"/>
        public event EndMatchEventHandler OnEndMatchEvent;

        public string Token
        {
            get
            {
                var client = GetHostSeat();
                Debug.Assert(client != null, "Assert host client");
                var hostToken = Env.CloudGameToken;
                return MakeToken(client, hostToken);
            }
        }

        private ICloudSeat GetHostSeat()
        {
            return ICloudSync.Instance.SeatManager.HostSeat;
        }

        private static string MakeToken(ICloudSeat seat, string hostToken)
        {
            return MakeToken(hostToken, (AnchorPlayerInfo)seat.PlayerInfo, seat.RtcUserId);
        }

        internal static string MakeToken(string hostToken, AnchorPlayerInfo playerInfo, string rtcUserId)
        {
            var data = new SwitchTokenData
            {
                hostToken = hostToken,
                anchor = playerInfo,
                rtcUserId = rtcUserId
            };
            return data.ToToken();
        }

        private void SetState(SwitchState state)
        {
            Debug.Log($"SetState({state})");
            _state = state;
        }

        internal void InternalSetState(SwitchState state)
        {
            Debug.LogWarning($"InternalSetState({state})");
            _state = state;
        }

        IHostRoom ICloudSwitchManager.BeginHost(ICloudSwitchTokenProvider tokenProvider)
        {
            Debug.Log("BeginHost 云同步-开始房主状态");
            if (_state != SwitchState.None)
                throw new InvalidOperationException(ToHostErrorMsg($"当前状态无效! Invalid state. State is {_state}"));
            if (_currentHost != null)
                throw new InvalidOperationException(ToHostErrorMsg("已创建了房间! The current host is already set."));

            if (!Env.CanUseOnlineMatch())
            {
                Debug.LogError(ToHostErrorMsg("" + CloudMatchManagerImpl.MsgMobileCannotMatch));
                return null;
            }

            if (tokenProvider is not ICloudMatchManagerEx)
            {
                if (ICloudSync.Instance.MatchManager is ICloudMatchManagerEx matchManagerEx)
                {
                    if (matchManagerEx.State == CloudMatchState.None)
                        Debug.Log("云同步-检测到手动切流模式-BeginHost");
                }
            }

            if (GetHostSeat().PlayerInfo == null)
            {
                Debug.LogError(ToHostErrorMsg("获取主播用户信息失败！"));
                return null;
            }

            _currentHost = CreateHostRoom(tokenProvider);
            if (_currentHost == null)
            {
                Debug.LogError(ToHostErrorMsg("创建房间失败!"));
                return null;
            }

            Debug.LogDebug($"Create: {_currentHost?.ID}");
            SetState(SwitchState.Host);
            CloudSyncSdk.InternalInvokeEvent(nameof(OnBeginHost),() => OnBeginHost?.Invoke(_currentHost));
            return _currentHost;
        }

        private IHostRoomEx CreateHostRoom(ICloudSwitchTokenProvider tokenProvider)
        {
            if (HostRoomProvider != null)
                return HostRoomProvider.CreateHostRoom(this, tokenProvider);
            return (this as IHostRoomProvider).CreateHostRoom(this, tokenProvider);
        }

        public IHostRoomEx CreateHostRoom(ICloudSwitchManager manager, ICloudSwitchTokenProvider tokenProvider)
        {
            var hostSeat = GetHostSeat();
            var anchor = hostSeat.PlayerInfo as AnchorPlayerInfo;
            if (anchor == null)
                return null;
            return new HostRoom(this, anchor, tokenProvider);
        }

        void HostRoom.IHandler.OnEndHostRoom(IHostRoomEx room)
        {
            Debug.Log($"EndHost: {room?.ID}");
            if (_currentHost == null)
                Debug.LogError($"OnEndHostRoom current is null!");
            if (_currentHost?.ID != room?.ID)
                Debug.LogError($"OnEndHostRoom room != current, roomID: {room?.ID}, currentID: {_currentHost?.ID}");

            _currentHost = null;
            SetState(SwitchState.None);
            var endEvent = new CloudMatchEndEvent
            {
                EndType = EndEventType.EndHostState,
            };
            OnSwitchBack(endEvent);
        }

        public async Task<SwitchResult> SwitchTo(string token, SeatIndex index, string matchKey)
        {
            if (_state != SwitchState.None)
                return ErrorResult(SwitchResultCode.InvalidState, ToSwitchErrorMsg($"当前状态无效! Invalid state. State is: {_state}"));

            if (GetHostSeat().PlayerInfo == null)
                return ErrorResult(SwitchResultCode.InvalidState, ToSwitchErrorMsg("获取主播用户信息失败! PlayerInfo is null!"));

            if (!Env.CanUseOnlineMatch())
                return ErrorResult(SwitchResultCode.Error, ToSwitchErrorMsg(CloudMatchManagerImpl.MsgMobileCannotMatch));

            Debug.Log($"SwitchTo 云同步-切流到目标 index: {index.ToInt()}");
            SetState(SwitchState.Switching);

            if (ICloudSync.Instance.MatchManager is ICloudMatchManagerEx matchManagerEx)
            {
                if (matchManagerEx.State == CloudMatchState.None)
                    Debug.Log("云同步-检测到手动切流模式-SwitchTo");
            }

            var result = await DoSwitchTo(token, index, matchKey);
            SetState(result.Success ? SwitchState.Switched : SwitchState.None);
            CloudSyncSdk.InternalInvokeEvent(nameof(OnSwitchTo),() => OnSwitchTo?.Invoke(result));
            return result;
        }

        private static string ToHostErrorMsg(string msg) => "云同步-Host开始房主失败：" + msg;
        private static string ToSwitchErrorMsg(string msg) => "云同步-Switch切流失败：" + msg;

        private static SwitchResult ErrorResult(SwitchResultCode code, string message)
        {
            Debug.LogError(message);
            return CreateResult(code, message);
        }

        private static SwitchResult CreateResult(SwitchResultCode code, string message)
        {
            return new SwitchResult
            {
                Code = code,
                Message = message
            };
        }

        private async Task<SwitchResult> DoSwitchTo(string token, SeatIndex index, string matchKey)
        {
            Debug.Log($"SwitchTo(token = {token}, index = {index}, matchKey = {matchKey})");
            if (index <= 0)
            {
                return ErrorResult(SwitchResultCode.InvalidIndex, ToSwitchErrorMsg("The index is invalid."));
            }

            var data = SwitchTokenData.FromToken(token);

            // 检查 token 是否合法
            if (data == null)
            {
                return ErrorResult(SwitchResultCode.InvalidToken, ToSwitchErrorMsg("The token is invalid. (Data format error)"));
            }

            // 不能为自己的 token （自己不能连自己）
            if (data.hostToken == Env.CloudGameToken)
            {
                return ErrorResult(SwitchResultCode.InvalidToken, ToSwitchErrorMsg("The token is invalid. (Can't be your own token!)"));
            }

            var matchParam = new ApiMatchParams
            {
                hostToken = data.hostToken,
                roomIndex = (int)index,
                matchKey = matchKey ?? "default-match-key"
            };

            TeaReport.Report_sdk_send_match_start();
            var resp = await CloudGameSdk.API.SendMatchBegin(matchParam);

            SwitchResult result;
            if (resp.IsSuccess)
            {
                result = CreateResult(SwitchResultCode.Success, "Success");
                Debug.Log($"云同步-Switch切流成功：Result: {result.Code}, Message: {result.Message}");
            }
            else
            {
                result = ErrorResult(SwitchResultCode.Error, resp.ToStr());
                Debug.LogError(ToSwitchErrorMsg($"error! {result.Code}, Message: {result.Message}"));
            }

            TeaReport.Report_sdk_send_match_end((int)result.Code, data.hostToken, result.Message, matchKey, result.Code == SwitchResultCode.Success, (int)index);
            return result;
        }

        /// <summary>
        /// Host Room 可能在比较靠后的时间才创建，这里做一个兜底等待
        /// </summary>
        /// <param name="token"></param>
        /// <param name="joiningSeat"></param>
        private async Task TryWaitHostRoom(CancellationToken token, ICloudSeat joiningSeat)
        {
            if (_currentHost == null)
            {
                Debug.LogWarning($"收到玩家进房：等待我方成为房主。 join seat: {joiningSeat?.Index} (timeout = {MaxWaitTimeMs / 1000f:F1}s)");
                var now = DateTimeOffset.UtcNow;
                while (_currentHost == null && !token.IsCancellationRequested)
                {
                    await Task.Yield();

                    if (DateTimeOffset.UtcNow - now > TimeSpan.FromMilliseconds(MaxWaitTimeMs))
                        break;
                }
            }
            else
            {
                Debug.Log($"收到玩家进房：我方是房主。 join seat: {joiningSeat?.Index}");
            }
        }

        public async Task<AnchorPlayerInfo> FetchOnJoinPlayerInfo(ICloudSeat seat, CancellationToken token)
        {
            Debug.Log($"FetchOnJoinPlayerInfo index: {seat.Index}");
            await TryWaitHostRoom(token, seat);

            if (_currentHost == null)
            {
                Debug.LogError($"玩家进房错误：我方没有成为房主！ Wait host room timeout, seat: {seat.Index}");
                return null;
            }

            return await _currentHost.FetchPlayerInfo(seat, token);
        }

        void IMatchAPIListener.OnPodCustomMessage(ApiPodMessageData msgData)
        {
            var message = msgData?.message ?? "{}";
            var wrapMessage = JsonConvert.DeserializeObject<MatchPodMessage>(message);
            if (wrapMessage == null)
            {
                Debug.LogError($"receive OnPodCustomMessage convert MatchPodCustomMessage error! msgData: {msgData?.ToStr()}");
                return;
            }

            var type = wrapMessage.type; // 封装消息类型
            var info = wrapMessage.info; // 透传消息内容
            Debug.LogDebug($"receive OnPodCustomMessage wrapMessage: {wrapMessage.ToStr()}");
            if (type == MatchPodMessageType.DefaultMsg)
                return;

            switch (type)
            {
                case MatchPodMessageType.EndEvent:
                    var endEvent = CloudMatchEndEvent.CreateFrom(wrapMessage);
                    OnSwitchBack(endEvent);
                    break;
            }
        }

        void IMatchAPIListener.OnCommandMessage(ApiMatchCommandMessage msg)
        {
            switch (msg.command)
            {
                case MatchCommand.ReportSwitchBack:
                    Debug.LogDebug("receive msg.command: ReportSwitchBack");
                    // @see: `OnPodCustomMessage` 我们使用封装消息类型 `MatchPodMessageType.EndInfo` 来作为结束时回流时的消息、并携带透传消息内容
                    break;
            }
        }

        private void OnSwitchBack(CloudMatchEndEvent endEvent)
        {
            Debug.LogDebug($"OnSwitchBack, frame: {Time.frameCount}");
            Debug.Assert(_currentHost == null);
            if (IgnoreEndEvent(endEvent))
            {
                return;
            }

            Debug.Log(endEvent.EndType == EndEventType.EndHostState
                ? $"云同步-事件：结束同玩了（结束房主状态） EndType: {endEvent.EndType}"
                : $"云同步-事件：结束同玩了（玩家回流） EndType: {endEvent.EndType}, fromId: {endEvent.FromId}, endInfo len: {endEvent.EndInfo?.Length ?? 0}");
            SetState(SwitchState.None);
            InvokeEvent(nameof(OnEndMatchEvent), () => OnEndMatchEvent?.Invoke(endEvent));
        }

        private bool IgnoreEndEvent(CloudMatchEndEvent endEvent)
        {
            var eventType = endEvent.EndType;
            var endInfo = endEvent.EndInfo;
            var playerSessionId = endEvent.PlayerSessionId;

            // todo: implement 校验 PlayerSessionId 判断是否已收到过该场次
            return false;
        }

        public void Update()
        {
            _safeMatchAPIListener?.Update();
        }

        private static void InvokeEvent(string name, Action action) => CloudSyncSdk.InvokeEvent(name, action);
    }

    internal enum SwitchState
    {
        None,
        Host,
        Switching,
        Switched,
    }

    [Serializable]
    internal class SwitchTokenData
    {
        public string hostToken;

        public string rtcUserId;

        public AnchorPlayerInfo anchor;

        public string ToToken()
        {
            var json = JsonUtility.ToJson(this);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }

        public static SwitchTokenData FromToken(string token)
        {
            try
            {
                var s = Convert.FromBase64String(token);
                var json = Encoding.UTF8.GetString(s);
                return JsonUtility.FromJson<SwitchTokenData>(json);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}