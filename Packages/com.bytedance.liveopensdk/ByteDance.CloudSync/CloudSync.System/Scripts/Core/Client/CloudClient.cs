using System;
using System.Threading;
using System.Threading.Tasks;
using ByteDance.CloudSync.MatchManager;
using ByteDance.CloudSync.TeaSDK;
using UnityEngine;

namespace ByteDance.CloudSync
{
    internal interface ICloudClient
    {
        /// <summary>
        /// Client序号。 即 rtc多路序号 0,1,2,3. 其中 0 房主主播, 1~3 加入的玩家. 对齐云游戏的房间号`roomIndex`、单实例多路的座位号.
        /// </summary>
        SeatIndex Index { get; }

        int IntIndex => (int)Index;

        /// <summary>
        /// 云游戏rtc的userid. 云游戏链路可信任的id。
        /// </summary>
        string RtcUserId { get; }

        ICloudSeat Seat { get; }

        /// <summary>
        /// 状态
        /// </summary>
        ClientState State { get; }

        /// <summary>
        /// 当 Client Disconnect 时的取消操作 Token
        /// </summary>
        CancellationToken DisconnectToken { get; }

        bool IsConnected { get; }

        IVirtualDevice Device { get; }

        /// <summary>
        /// 此 Client 对应的玩家（主播）信息
        /// </summary>
        IPlayerInfo PlayerInfo { get; }

        /// <summary>
        /// 单个用户结束同玩：使他退出、回到单播状态。
        /// </summary>
        /// <param name="endInfo">结束信息。回到原实例时需要透传的信息</param>
        Task<IEndResult> EndMatchGame(string endInfo = null);
    }

    public enum ClientState
    {
        /// <summary>
        /// 未初始化
        /// </summary>
        None,
        /// <summary>
        /// 正在连接状态
        /// </summary>
        Connecting,
        /// <summary>
        /// 已断开
        /// </summary>
        Connected,
        /// <summary>
        /// 正在断开
        /// </summary>
        Disconnecting
    }

    internal class RenderSettings : IRenderSettings
    {
        // note: 坐标结构 struct 无gc问题
        // 注意：Device 和 Screen 初始化时赋值。不是随时访问Screen数值。
        //      避免问题：例如 Simple Mock  时的 Editor 面板窗口中，如果随时访问Screen，会获取到 Screen 宽高，和 Device 创建时游戏画面的不同。
        public Vector2Int Resolution { get; set; } = new(Screen.width, Screen.height);
    }

    internal class CloudClient : ICloudClient, IDisposable
    {
        private const string Tag = "CloudClient";
        internal const string MsgFetchPlayerInfoFailed = "Fetch player info failed.";

        /// <inheritdoc cref="ICloudClient.Index"/>
        public SeatIndex Index { get; private set; }

        public ICloudSeat Seat => _seat;

        /// <inheritdoc cref="ICloudClient.State"/>
        public ClientState State => _state;

        /// <inheritdoc cref="ICloudClient.RtcUserId"/>
        public string RtcUserId => RtcUserInfo.RtcUserId;

        public CloudUserInfo RtcUserInfo => _rtcUserInfo;

        public IVirtualDevice Device => _device;

        public IPlayerInfo PlayerInfo => _playerInfo;

        private ICloudSeat _seat;
        private IPlayerInfo _playerInfo;
        private CloudUserInfo _rtcUserInfo;
        private IVirtualDevice _device;
        private ClientState _state;
        private CancellationTokenSource _disconnectTokenSource = new();

        public CancellationToken DisconnectToken
        {
            get
            {
                if (_state == ClientState.Disconnecting)
                    return new CancellationToken(true);
                return _disconnectTokenSource.Token;
            }
        }

        public bool IsConnected => _state == ClientState.Connected;

        internal event Action<ICloudClient, ClientState> ConnectStateChanged;

        /// <summary>
        /// 初始化用户座位
        /// </summary>
        public void Initialize(ICloudSeat seat, IVirtualDeviceFactory factory)
        {
            CGLogger.Log($"CloudClient Initialize {seat.Index}");

            Index = seat.Index;
            _seat = seat;
            _rtcUserInfo.SetIndex(Index); // 注意不要取`UserInfo`，会得到临时copy
            Debug.Assert(RtcUserInfo.Index == Index);

            InitVirtualDevice(factory);

            CGLogger.Log("CloudClient Initialize Finished");
        }

        private void InitVirtualDevice(IVirtualDeviceFactory factory)
        {
            _device = VirtualDeviceSystem.Instance.CreateDevice(Index, factory, new RenderSettings());
            _device.Init();
            _device.Input.RemoteInput.OnMouse += e => CloudSyncSdk.InternalCurrent.OnRemoteMouseEvent(e);
            _device.Input.RemoteInput.OnKeyboard += e => CloudSyncSdk.InternalCurrent.OnRemoteKeyboardEvent(e);
            _device.Input.RemoteInput.OnTouches += e => CloudSyncSdk.InternalCurrent.OnRemoteTouchesEvent(e);
        }

        /// <summary>
        /// 开关设备（推流画面、输入）
        /// </summary>
        private void SetDeviceEnable(bool screenEnable, bool inputEnable)
        {
            var index = (int)Index;
            if (screenEnable) TeaReport.Report_visual_screen_enable(index);
            if (inputEnable) TeaReport.Report_visual_device_enable(index);
            _device.Screen.Enable = screenEnable;
            _device.Input.Enable = inputEnable;
        }

        private bool IsInputEnable => _device != null && _device.Input.Enable;

        public async Task OnConnecting(PlayerConnectingMessage msg)
        {
            CGLogger.Log($"Client #{msg.index} connecting ... (OnPlayerJoin {State} -> Connecting) (frame: {Time.frameCount}f)");
            _rtcUserInfo = msg.UserInfo;
            SetState(ClientState.Connecting);

            // get and set player info.
            var token = _disconnectTokenSource.Token;
            var provider = CloudSyncSdk.InternalCurrent.GetOnJoinPlayerInfoProvider(_seat);
            var index = Index.ToInt();
            CGLogger.Log($"云同步-获取用户信息 ... (index: {index})");
            var playerInfo = await provider.FetchOnJoinPlayerInfo(_seat, token);
            if (token.IsCancellationRequested)
                return;
            if (playerInfo != null)
                CGLogger.Log($"云同步-获取用户信息成功 (index: {index}) PlayerInfo: {playerInfo.ToStr()}");
            else
                CGLogger.LogError($"云同步-获取用户信息失败 {MsgFetchPlayerInfoFailed} (index: {index})");
            _playerInfo = playerInfo;
            OnConnected(msg);
        }

        internal void UpdatePlayerInfo(IPlayerInfo playerInfo)
        {
            _playerInfo = playerInfo;
        }

        private void OnConnected(CloudGameMessageBase msg)
        {
            CGLogger.Log($"Client #{msg.index} enter. (OnPlayerJoin {State} -> Connected) (frame: {Time.frameCount}f)");

            // todo: 可以obsolete了？ （边界case已统一由 CloudGameSdkManager.Player 处理？）
            const string eventName = "OnPlayerJoin";
            var index = msg.index;
            var newInfo = msg.UserInfo;
            var oldInfo = RtcUserInfo;
            if (oldInfo.IsValidInfo && State == ClientState.Connected)
            {
                var log = $"WARNING: Client #{index} enter but existing! 边界case Join使用新用户信息顶替. {eventName} new {newInfo}, existing {oldInfo} state: {State}";
                CGLogger.LogError(log);
                // todo: check 在边界检查后再 event notify PlayerLeave?
            }

            _rtcUserInfo = msg.UserInfo;
            SetState(ClientState.Connected);
        }

        /// <summary>
        /// 用户离开
        /// </summary>
        internal void DisconnectSuccess(PlayerDisconnectedMessage msg)
        {
            const string eventName = "OnPlayerExit";
            var index = msg.index;
            var prevState = State;
            var newInfo = msg.UserInfo;
            var rtcUserId = newInfo.RtcUserId;

            // note: 边界case： 如果主播1 把 用户A（在`index` 2），切换成 用户B（他加入时`index`也会是 2），端上无法100%保序为 先 退房信息 A，再 加房信息 B.
            //                 有可能发生 先 加房信息 B，再 退房信息 A。 因此判断userId一致性
            var oldInfo = RtcUserInfo;
            if (rtcUserId == RtcUserId)
            {
                // 正常case：清除旧的用户信息
                _rtcUserInfo = new CloudUserInfo(Index, string.Empty); // 注意不要取`UserInfo`，会得到临时copy
                CGLogger.Log($"Client #{index} leave. {eventName} {oldInfo} state: {State}");
            }
            else
            {
                // todo: 可以obsolete了？ （边界case已统一由 CloudGameSdkManager.Player 处理？）
                // 边界case：本次Exit用户信息不匹配
                if (RtcUserInfo.IsValidInfo)
                {
                    // 边界case：Joined已有新值，本次Exit用户信息是旧的
                    var log = $"WARNING: Client #{index} leave id not match, ignored. 边界case: Exit用户信息是旧的（已被顶替）. {eventName} {newInfo}, existing {oldInfo} state: {State}";
                    CGLogger.LogError(log);
                }
                else
                {
                    // 边界case：已有用户信息是空的
                    var log = $"WARNING: Client #{index} leave, ignored. 边界case: 已有用户信息是空的. {eventName} {newInfo}, existing none, state: {State}";
                    CGLogger.LogError(log);
                }

                // note: return即可. 正确的用户进房由 `OnConnected` 里正确处理。
                return;
            }

            SetState(ClientState.Disconnecting);
            ResetToken();

            // todo: 可以obsolete了？ （边界case已统一由 CloudGameSdkManager.Player 处理？）
            if (prevState != ClientState.Connected)
            {
                if (prevState == ClientState.Connecting)
                    // 边界case: 1 Warning 退房用户查询中、还未返回OnQuery，只需要处理Client内部逻辑和状态，但不抛出离开事件.
                    CGLogger.LogWarning($"Client #{index} leave 边界case: 退房用户查询中、还未返回 state: {prevState}");
                else
                    // 边界case: 2 Error 退房用户不在，不抛出离开事件.
                    CGLogger.LogError($"Client #{index} leave 边界case: 退房用户不在 state: {prevState}");
                // 只有已经connected的抛过Enter事件，才需要对外抛Leave事件。
                return;
            }

            // todo: check 在边界检查后再 event notify PlayerLeave?
        }

        /// <summary>
        /// 收到自定义消息
        /// </summary>
        /// <param name="data"></param>
        public void OnCustomMessage(CustomMessageData data)
        {
            CGLogger.Log($"Client #{Index} OnCustomMessage data index: {data.index}, message: {data.message}");
            CouldGameEventEmitter.Instance.EmitCustomMessage(data);
        }

        public void Operate(PlayerOperate operate)
        {
            if (!IsInputEnable)
                return;
            try
            {
                _device.Input.ProcessInput(operate);
            }
            catch (Exception e)
            {
                CGLogger.LogError($"Operate Exception! index: {Index}, state {_state}, {e}");
            }
        }

        private void SetState(ClientState state)
        {
            if (_state == state)
                return;
            _state = state;

            TeaReport.Report_player_state_change((int)state);
            switch (state)
            {
                case ClientState.Connecting:
                    SetDeviceEnable(true, false);
                    FireStateChanged();
                    break;
                case ClientState.Connected:
                    SetDeviceEnable(true, true);
                    FireStateChanged();
                    break;
                case ClientState.Disconnecting:
                    SetDeviceEnable(false, false);
                    FireStateChanged();
                    break;
            }
        }

        private void FireStateChanged()
        {
            // 单个client自己抛出：我发生了变化
            ConnectStateChanged?.Invoke(this, _state);
            // 让CloudSyncSdk抛出：哪个client发生了变化
            var sdk = CloudSyncSdk.InternalCurrent;
            if (sdk != null)
                sdk.OnClientStateChange(this, _state);
        }

        private void ResetToken()
        {
            _disconnectTokenSource.Cancel();
            _disconnectTokenSource = new CancellationTokenSource();
        }

        public Task<IEndResult> EndMatchGame(string endInfo = null)
        {
            if (this.IsHost())
            {
                return Task.FromResult<IEndResult>(new MatchEndResult
                {
                    Code = EndResultCode.Success,
                    Message = "EndMatchGame for 'Host client' was ignored."
                });
            }
            if (!IsConnected)
            {
                CGLogger.LogError($"EndMatchGame Error: Wrong state! Client #{Index} not connected!");
            }
            return ICloudSync.Instance.MatchManager.EndMatchGame(Index, endInfo);
        }

        public void Dispose()
        {
            VirtualDeviceSystem.Destroy(_device);
        }
    }
}
