using System;
using System.Threading;
using System.Threading.Tasks;
using ByteDance.CloudSync.Match;
using ByteDance.CloudSync.TeaSDK;
using UnityEngine;


// ReSharper disable once CheckNamespace
namespace ByteDance.CloudSync.MatchManager
{
    internal enum CloudMatchState
    {
        None,

        /// 匹配用户中
        MatchingUser,

        /// 匹配切流中
        MatchingStream,

        /// 游戏中作为房主
        InGameAsHost,

        /// 游戏中连接到了他人的房间（从房主拉流）
        InConnectOtherRoom,

        /// 同玩结束中
        EndingGame
    }

    internal interface ICloudMatchManagerEx : ICloudMatchManager, ICloudManager, ICloudSwitchTokenProvider
    {
        CloudMatchState State { get; }
        /// <summary>
        /// 匹配选项（可选选项，默认无需修改）
        /// </summary>
        CloudMatchOptionsEx OptionsEx { get; }

        IHostRoom HostRoom { get; }

        void SetInners(ICloudSwitchManagerEx switchManager,
            IMatchService customService = null,
            ICloudGameAPI customSdkAPI = null,
            ICloudSyncEnv customEnv = null);
    }

    /// <summary>
    /// 匹配选项
    /// </summary>
    internal class CloudMatchOptionsEx
    {
        /// <summary>
        /// 是否手动切流。 默认false，系统自动切流。 true，手动切流。
        /// </summary>
        public bool IsManualSwitch { get; set; } = false;
    }

    internal partial class CloudMatchManagerImpl : ICloudMatchManagerEx, IAnchorPlayerInfoProvider, IMultiAnchorPlayerInfoProvider, ICloudSwitchTokenProvider
    {
        internal const string MsgMobileCannotMatch = "目前移动端仅支持玩法内的单人模式，暂不支持多人联机模式";

        /// <inheritdoc cref="ICloudMatchManager.OnMatchUsers"/>
        public event MatchUsersHandler OnMatchUsers;

        [System.Obsolete("已废弃! Use new event: `OnEndMatchEvent` instead.", true)]
        public event EndMatchGameHandler OnEndMatchGame;

        /// <inheritdoc cref="ICloudMatchManager.OnEndMatchEvent"/>
        public event EndMatchEventHandler OnEndMatchEvent;

        public CloudMatchOptionsEx OptionsEx { get; private set; }

        #region 依赖外部输入 Dependencies

        /// <summary>
        /// Server的匹配服务
        /// </summary>
        private IMatchService MatchService { get; set; }

        /// <summary>
        /// gameSdk dll的切流接口
        /// </summary>
        private ICloudGameAPI MatchAPI { get; set; }

        /// <summary>
        /// SdkEnv环境
        /// </summary>
        private ICloudSyncEnv Env { get; set; }

        private CloudSyncSdk CloudSync => CloudSyncSdk.InternalCurrent;
        private ICloudSwitchManagerEx SwitchManager { get; set; }
        private CancellationToken OnDestroyToken => CloudSync.OnDestroyToken;

        private bool IsMockAuth { get; set; }

        #endregion


        public CloudMatchState State
        {
            get => _state;
            private set
            {
                var prev = _state;
                _state = value;
                Debug.Log($"set state: `{_state}` (prev: `{prev}`)");
            }
        }

        public IHostRoom HostRoom => _hostRoom;

        private MatchCloudGameInfo MyCloudGameInfo { get; set; }
        private AnchorPlayerInfo MyPlayerInfo => CloudSync.AnchorPlayerInfo as AnchorPlayerInfo;
        private CloudMatchUsersResult MatchUsersResult { get; set; }

        private static readonly SdkDebugLogger Debug = new("CloudMatch");
        private static IMatchService _matchServiceSingleton;
        private FetchPlayerInfoOperation _playerInfoOp;
        private TaskCompletionSource<IMatchResult> _matchTaskSource;
        private CancellationTokenSource _matchCancelSource;
        private MatchUserOperation _matchUserOp;
        private CloudMatchState _state;
        private bool _waitMyClientConnect;
        private IHostRoom _hostRoom;

        // MARK: - 初始化

        public CloudMatchManagerImpl(ICloudSyncEnv env, ICloudSwitchManagerEx switchManager,
            IMatchService customService = null,
            ICloudGameAPI customSdkAPI = null)
        {
            Debug.Log($"CloudMatchManager ctor, frame: {Time.frameCount}");
            Env = env;
            OptionsEx = new CloudMatchOptionsEx();
            SetInners(switchManager, customService, customSdkAPI);
        }

        public void SetInners(ICloudSwitchManagerEx switchManager,
            IMatchService customService = null,
            ICloudGameAPI customSdkAPI = null,
            ICloudSyncEnv customEnv = null)
        {
            SwitchManager = switchManager;
            if (customService != null)
            {
                Debug.LogDebug($"use injected MatchService: {customService}");
                MatchService = customService;
            }

            if (customSdkAPI != null)
            {
                Debug.LogDebug($"use injected MatchAPI: {customSdkAPI}");
                MatchAPI = customSdkAPI;
            }

            if (customEnv != null)
            {
                Debug.LogDebug($"use injected customEnv: {customEnv}");
                Env = customEnv;
            }
        }

        public void Initialize()
        {
            Debug.Assert(SwitchManager != null, "Assert switchManager");
            TeaReport.Report_cloudmatchmanager_init_start();
            if (!Env.IsDouyin() || !Env.IsCloud())
                Debug.LogWarning($"CloudMatchManager 运行环境: IsDouyin: {Env.IsDouyin()}, IsCloud: {Env.IsCloud()}");
            Debug.Assert(!string.IsNullOrEmpty(Env.AppId), "Assert Env.AppId");
            Debug.Assert(!string.IsNullOrEmpty(Env.CloudGameToken), "Assert Env.CloudGameToken");
            IsMockAuth = Env.IsMockWebcastAuth();
            if (!IsMockAuth)
                Debug.Assert(!string.IsNullOrEmpty(Env.GetLaunchToken()), "Assert Env.GetLaunchToken");
            Debug.LogDebug($"IsMockAuth: {IsMockAuth}");

            InitService();
            InitSdkAPI();

            var cloudGameToken = Env.CloudGameToken;
            MyCloudGameInfo = new MatchCloudGameInfo
            {
                cloudGameToken = cloudGameToken,
                rtcUserId = null
            };
            _waitMyClientConnect = true;
            InitCloudClients();
            LogCloudGameInfo();

            SwitchManager.OnEndMatchEvent += OnSwitchEndMatchEvent;
            SwitchManager.OnBeginHost += OnBeginHost;
            SwitchManager.OnSwitchTo += OnSwitchTo;

            BaseMatchOperation.Debug = Debug;
            BaseMatchOperation.MatchService = MatchService;
            BaseMatchOperation.MatchAPI = MatchAPI;
            Debug.Assert(MatchService != null);
            Debug.Assert(MatchAPI != null);
            TeaReport.Report_cloudmatchmanager_init_end(ServiceIP);
        }

        private void InitCloudClients()
        {
            Debug.LogDebug($"InitCloudClients, frame: {Time.frameCount}");
            CloudSync.OnClientStateChanged += OnClientStateChanged;
            var myClient = GetHostClient();
            if (myClient != null)
                OnClientStateChanged(myClient, myClient.State);
        }

        public void Dispose()
        {
            Debug.LogDebug("CloudMatchManager Dispose");
            if (CloudSync != null)
                CloudSync.OnClientStateChanged -= OnClientStateChanged;

            if (SwitchManager != null)
            {
                SwitchManager.OnEndMatchEvent -= OnSwitchEndMatchEvent;
                SwitchManager.OnBeginHost -= OnBeginHost;
                SwitchManager.OnSwitchTo -= OnSwitchTo;
            }

            if (MatchService != null)
            {
                MatchService.Dispose();
            }
        }

        private void InitService()
        {
            Debug.LogDebug($"InitService, frame: {Time.frameCount}");
            _serviceAppId = Env.AppId;
            if (MatchService != null)
                return;

            // MatchService 服务要求，其连接应保持单例
            if (_matchServiceSingleton != null)
            {
                MatchService = _matchServiceSingleton;
                return;
            }

            var appId = _serviceAppId;
            var webcastToken = Env.GetLaunchToken();
            var connectionToken = Match.MatchService.GenerateConnectionToken(appId, webcastToken);
            Debug.Log($"InitMatchService appId: {appId}, webcastToken: {webcastToken}, connectionToken: {connectionToken}, isFake: {IsMockAuth}");
            // note: 分号分隔的ip: 内部支持多个ip自动选择竞速机制
            var service = new MatchService(ServiceIP, ServicePort, connectionToken, IsMockAuth);
            MatchService = service;
            _matchServiceSingleton = MatchService;
        }

        private static string _serviceAppId;
        internal static string ServiceIP => "49.7.48.101:8900;221.194.138.123:8900;111.62.49.244:8900";
        internal static int ServicePort => 0;

        private void InitSdkAPI()
        {
            Debug.LogDebug($"InitSdkAPI, frame: {Time.frameCount}");
            if (MatchAPI != null)
                return;

            MatchAPI = CloudGameSdk.API;
        }

        private void LogCloudGameInfo()
        {
            Debug.LogDebug("CloudGame info: " +
                           $"logId: {Env.CloudGameLogId}, did: {Env.CloudDeviceDid}, AppId: {Env.AppId}" +
                           $", MyCloudGameInfo: {MyCloudGameInfo?.ToStr()}, frame: {Time.frameCount}");
        }

        // MARK: - 匹配

        #region 匹配

        public Task<IMatchResult> RequestMatch(SimpleMatchConfig simpleMatchConfig, CancellationToken cancelToken)
        {
            var config = simpleMatchConfig.ToMatchConfig();
            return RequestMatch(config, "", cancelToken);
        }

        public Task<IMatchResult> RequestMatch(MatchConfig config, string matchParamJson)
        {
            return RequestMatch(config, matchParamJson, CancellationToken.None);
        }

        public Task<IMatchResult> RequestMatch(MatchConfig config, string matchParamJson, CancellationToken cancelToken)
        {
            Debug.Log("RequestMatch 云同步-请求匹配");
            if (State == CloudMatchState.MatchingStream) Debug.LogWarning("已在切流中 Already matching stream, keep waiting...");
            if (State == CloudMatchState.MatchingUser) Debug.LogWarning("已在匹配中 Already matching user, keep waiting... ");

            if (State is CloudMatchState.InGameAsHost or CloudMatchState.InConnectOtherRoom or CloudMatchState.EndingGame)
            {
                var msg = GetInvalidStateMsg(State);
                Debug.LogError(msg);
                IMatchResult result = MatchErrorResult(msg, MatchResultCode.InvalidStateError);
                return Task.FromResult(result);
            }

            if (!Env.CanUseOnlineMatch())
            {
                Debug.LogError(ToMatchErrorMsg(MsgMobileCannotMatch));
                IMatchResult result = MatchErrorResult(ToMatchErrorMsg(MsgMobileCannotMatch));
                return Task.FromResult(result);
            }

            if (_matchTaskSource != null)
            {
                _matchCancelSource = CancellationTokenSource.CreateLinkedTokenSource(cancelToken, _matchCancelSource.Token);
                return _matchTaskSource.Task;
            }

            return _RequestMatch(config, matchParamJson, cancelToken);
        }

        private static string ToMatchErrorMsg(string msg) => "匹配失败：" + msg;

        private string GetInvalidStateMsg(CloudMatchState state)
        {
            switch (state)
            {
                case CloudMatchState.InGameAsHost:
                    return ToMatchErrorMsg($"当前状态无效。已在房主状态! Invalid state. Current State: {state}");
                case CloudMatchState.InConnectOtherRoom:
                    return ToMatchErrorMsg($"当前状态无效。已切流到他人房间! Invalid state. Current State: {state}");
                case CloudMatchState.EndingGame:
                    return ToMatchErrorMsg($"当前状态无效。等待同玩结束中! Invalid state. Current State: {state}");
                default:
                    return ToMatchErrorMsg($"当前状态无效。 State: {state}");
            }

            return "";
        }

        private async Task<IMatchResult> _RequestMatch(MatchConfig matchConfig, string matchParamJson, CancellationToken inCancelToken)
        {
            var disposable = new MonitoredDisposable();
            disposable.OnDispose = () =>
            {
                _matchTaskSource = null;
                _matchCancelSource?.Dispose();
                _matchCancelSource = null;
                _matchUserOp = null;
            };

            using (disposable)
            {
                _matchTaskSource = new TaskCompletionSource<IMatchResult>();
                var myClient = GetHostClient();
                var disconnectToken = myClient?.DisconnectToken ?? CancellationToken.None; // 房主退出时，系统自动取消匹配
                _matchCancelSource = CancellationTokenSource.CreateLinkedTokenSource(inCancelToken, OnDestroyToken, disconnectToken);
                var cancelToken = _matchCancelSource.Token;
                LogCloudGameInfo();

                // 自动配置区分appid
                Debug.Assert(!string.IsNullOrEmpty(_serviceAppId), "Assert AppId");
                var config = InternalMatchConfig.CreateFrom(matchConfig, _serviceAppId);

                // 1. 调用 MatchService 匹配用户
                _matchUserOp = new MatchUserOperation(config, matchParamJson, MyPlayerInfo, MyCloudGameInfo, GetSwitchToken);
                State = CloudMatchState.MatchingUser;
                if (MyPlayerInfo == null)
                {
                    Debug.LogWarning("RequestMatch but MyPlayerInfo is not ready! Now fetch PlayerInfo...");
                    var info = await FetchPlayerInfo(inCancelToken);
                    Debug.LogWarning($"RequestMatch got MyPlayerInfo: {info?.ToStr() ?? "null"}");
                    _matchUserOp.MyPlayerInfo = info;
                }

                Report_MatchStart(config, matchParamJson);
                CloudMatchUsersResult matchUsersResult = await _matchUserOp.Run(cancelToken);
                _matchUserOp = null;
                Debug.Log($"云同步-请求匹配结果：{matchUsersResult.Code}, {matchUsersResult.Message}");
                if (!matchUsersResult.IsSuccess)
                {
                    State = CloudMatchState.None;
                    Report_MatchEnd(config, matchParamJson, matchUsersResult);
                    _matchTaskSource.SetResult(matchUsersResult);
                    return matchUsersResult;
                }

                MatchUsersResult = matchUsersResult;
                State = CloudMatchState.MatchingStream;
                Debug.Log($"云同步-事件：匹配到了用户 {GetMatchUsersEventInfo(matchUsersResult)}");
                InvokeEvent("OnMatchUsers", () => OnMatchUsers?.Invoke(matchUsersResult));

                // 如果手动切流模式，只做匹配用户，此处任务完成。 后续上层逻辑使用 SwitchManager 的接口和事件
                // see: `OnBeginHost`, `OnSwitchTo`
                if (OptionsEx.IsManualSwitch)
                {
                    Debug.LogWarning("云同步-手动切流模式：已匹配用户，等待切流 RequestMatch done, IsManualSwitch true");
                    State = CloudMatchState.None;
                    Report_MatchEnd(config, matchParamJson, matchUsersResult);
                    _matchTaskSource.SetResult(matchUsersResult);
                    return matchUsersResult;
                }

                LogCloudGameInfo();
                // 默认自动切流模式，衔接调用 SwitchManager
                var isHost = matchUsersResult.IsHost;
                IMatchResult result;
                if (isHost)
                {
                    // 2.A 作为房主A，等待其他玩家BCD加入Join
                    result = _SwitchBeginHost(matchUsersResult);
                }
                else
                {
                    // 2.B 作为玩家B，加入目标房主A，调用 MatchAPI 切流
                    result = await _SwitchTo(matchUsersResult);
                }

                Report_MatchEnd(config, matchParamJson, matchUsersResult);
                _matchTaskSource.SetResult(matchUsersResult);
                return result;

            }
        }

        private static string GetMatchUsersEventInfo(CloudMatchUsersResult r)
        {
            return r == null ? string.Empty : $"我方: {(r.IsHost ? "房主" : "玩家")}, index: {r.MyIndex.ToInt()}, 队伍数: {r.Teams?.Count ?? 0}, 用户数: {r.AllUsers?.Count ?? 0}";
        }

        private void Report_MatchStart(InternalMatchConfig config, string matchParamJson)
        {
            _ = config == null;
            _ = matchParamJson == null;
            TeaReport.Report_cloudmatchmanager_request_match_start((int)State, OptionsEx.IsManualSwitch, config?.MatchAppId ?? 0, matchParamJson,
                config?.MatchTag, config?.PoolName);
        }

        private void Report_MatchEnd(InternalMatchConfig config, string matchParamJson, CloudMatchUsersResult result)
        {
            _ = config == null;
            _ = matchParamJson == null;
            _ = result.Teams == null;
            TeaReport.Report_cloudmatchmanager_request_match_end((int)result.Code, result.IsHost,
                OptionsEx.IsManualSwitch,
                result.MatchId,
                config?.MatchAppId ?? 0,
                matchParamJson,
                config?.MatchTag,
                config?.PoolName,
                result.IsSuccess,
                (int)result.MyIndex,
                result.Teams?.Count ?? 0);
        }

        private IMatchResult _SwitchBeginHost(CloudMatchUsersResult result)
        {
            if (!ValidateSwitchState(result, out var errorResult))
            {
                State = CloudMatchState.None;
                return errorResult;
            }

            try
            {
                var hostRoom = SwitchManager.BeginHost(this);
                TeaReport.Report_switchmanager_begin_host();
                if (hostRoom != null)
                {
                    State = CloudMatchState.InGameAsHost;
                    _hostRoom = hostRoom;
                    return result;
                }
                else
                {
                    State = CloudMatchState.None;
                    var errMsg = "BeginHost unknown error";
                    Debug.LogError(errMsg);
                    return MatchErrorResult(errMsg);
                }
            }
            catch (Exception e)
            {
                State = CloudMatchState.None;
                var errMsg = BaseMatchOperation.ExceptionToMessage(e);
                Debug.LogException(e);
                return MatchErrorResult(errMsg);
            }
        }

        private async Task<IMatchResult> _SwitchTo(CloudMatchUsersResult result)
        {
            if (!ValidateSwitchState(result, out var errorResult))
            {
                State = CloudMatchState.None;
                return errorResult;
            }

            try
            {
                var hostUser = result.HostUser;
                var switchToken = GetSwitchToken(hostUser);
                var myIndex = result.MyIndex;
                var matchId = result.MatchId;
                TeaReport.Report_switchmanager_switchto_start((int)myIndex);
                var resp = await SwitchManager.SwitchTo(switchToken, myIndex, matchId);
                var code = resp.Code;
                TeaReport.Report_switchmanager_switchto_end((int)code,  result.MatchId,code == SwitchResultCode.Success, (int)myIndex, switchToken);
                switch (code)
                {
                    case SwitchResultCode.Success:
                    {
                        State = CloudMatchState.InConnectOtherRoom;
                        Debug.Log($"SwitchTo response success");
                        return result;
                    }
                    default:
                    {
                        State = CloudMatchState.None;
                        var errMsg = resp.Message;
                        Debug.LogError("SwitchTo response error! " +
                                       $"roomIndex: {myIndex}, code: {code} ({(int)code}), errMsg: {errMsg}, logId: --");
                        return MatchErrorResult(errMsg);
                    }
                }
            }
            catch (Exception e)
            {
                State = CloudMatchState.None;
                var errMsg = BaseMatchOperation.ExceptionToMessage(e);
                Debug.LogException(e);
                return MatchErrorResult(errMsg);
            }
        }

        private bool ValidateSwitchState(CloudMatchUsersResult userResult, out IMatchResult errorResult)
        {
            var state = SwitchManager.State;
            var host = SwitchManager.CurrentHost;
            if (state != SwitchState.None || host != null)
            {
                var errMsg = GetInvalidSwitchStateMsg(state, host);
                Debug.LogError(errMsg);
                errorResult = MatchErrorResult(userResult, errMsg, MatchResultCode.InvalidStateError);
                return false;
            }

            errorResult = null;
            return true;
        }

        private static string GetInvalidSwitchStateMsg(SwitchState state, IHostRoom host)
        {
            if (state != SwitchState.None)
            {
                switch (state)
                {
                    case SwitchState.Host:
                        return ToMatchErrorMsg($"当前状态无效。已在房主状态! Invalid state. Current state is {state}");
                    case SwitchState.Switching:
                    case SwitchState.Switched:
                        return ToMatchErrorMsg($"当前状态无效。已切流到他人房间! Invalid state. Current state is {state}");
                    default:
                        return ToMatchErrorMsg($"当前状态无效。Invalid state. Current state is {state}");
                }
            }

            if (host != null)
                return ToMatchErrorMsg("当前状态无效。The current host is already set.");
            return string.Empty;
        }

        private static CloudMatchUsersResult MatchErrorResult(string errMsg, MatchResultCode code = MatchResultCode.Error) =>
            MatchUserOperation.ErrorResult(errMsg, code);

        private static CloudMatchUsersResult MatchErrorResult(CloudMatchUsersResult baseResult, string errMsg, MatchResultCode code)
        {
            baseResult.Code = code;
            baseResult.Message = errMsg;
            return baseResult;
        }

        #endregion

        // MARK: - 结束

        #region 结束

        public async Task<IEndResult> EndMatchGame(string endInfo = "")
        {
            Debug.Log($"EndMatchGame 云同步-结束同玩：所有人 (info len: {endInfo?.Length})");
            var responses = await _EndMatchGame(SeatIndex.Invalid, endInfo);
            return responses;
        }

        public async Task<IEndResult> EndMatchGame(InfoMapping infoMapping)
        {
            Debug.Log($"EndMatchGame 云同步-结束同玩：所有人 (infoMapping: {infoMapping})");
            if (infoMapping == null)
            {
                var errorResult = EndErrorResult("EndMatchGame arg error: `endInfoMapping` is null!");
                Debug.LogError(errorResult.Message);
                return errorResult;
            }

            var responses = await _EndMatchGame(SeatIndex.Invalid, null, infoMapping);
            return responses;
        }

        public async Task<IEndResult> EndMatchGame(SeatIndex seatIndex, string endInfo = "")
        {
            Debug.Log($"EndMatchGame 云同步-结束同玩：单个用户 (index: {seatIndex})");
            var result = await _EndMatchGame(seatIndex, endInfo);
            Debug.Assert(result.SeatResponses.Length > 0, "Assert responses.Length > 0");
            return result;
        }

        private async Task<IEndResult> _EndMatchGame(SeatIndex seatIndex, string endInfo, InfoMapping infoMapping = null)
        {
            TeaReport.Report_cloudmatchmanager_end_match_start();
            // todo: 异常处理：游戏状态冲突：不在游戏中
            if (State != CloudMatchState.InGameAsHost)
                Debug.LogError($"游戏状态冲突：不在游戏中作为房主 State: {State}");

            var nonHostInfo = GetNonHostStatesDebugInfo("已没有其他玩家连接");
            Debug.LogDebug($"before EndMatchGame NonHost players states: {nonHostInfo}");

            LogCloudGameInfo();
            var result = await _SwitchEnd(seatIndex, endInfo, infoMapping);
            // TODO: 使用哪个作为上报结果
            // TeaReport.Report_cloudmatchmanager_end_match_end();
            return result;
        }

        private async Task<IEndResult> _SwitchEnd(SeatIndex roomIndex, string endInfo, InfoMapping infoMapping)
        {
            try
            {
                Debug.Assert(_hostRoom != null, "_SwitchEnd _hostRoom is null!");
                if (_hostRoom == null)
                {
                    return EndErrorResult("hostRoom is null");
                }

                IEndResult result;
                var hostRoom = _hostRoom;
                if (roomIndex != SeatIndex.Invalid)
                {
                    result = await hostRoom.Kick(roomIndex, endInfo);
                    return result;
                }

                if (infoMapping != null)
                {
                    result = await hostRoom.End(infoMapping);
                }
                else
                {
                    result = await hostRoom.End(endInfo);
                }

                return result;
            }
            catch (Exception e)
            {
                var errMsg = BaseMatchOperation.ExceptionToMessage(e);
                Debug.LogException(e);
                return EndErrorResult(errMsg);
            }
        }

        private static MatchEndResult EndErrorResult(string errMsg)
        {
            return new MatchEndResult
            {
                Code = EndResultCode.Error,
                Message = errMsg
            };
        }

        private static MatchEndSeatResponse MatchEndErrorSeatResponse(string errMsg)
        {
            return new MatchEndSeatResponse
            {
                Code = EndResultCode.Error,
                Message = errMsg
            };
        }

        #endregion

        // MARK: - 事件

        private static void InvokeEvent(string name, Action action) => CloudSyncSdk.InvokeEvent(name, action);

        private void OnSwitchEndMatchEvent(IEndEvent endEvent)
        {
            switch (State)
            {
                // 房主，结束同玩状态 （发生在房主实例）
                case CloudMatchState.InGameAsHost:
                // 玩家回流了，结束同玩状态 （发生在玩家自己原来的实例）
                case CloudMatchState.InConnectOtherRoom:
                    State = CloudMatchState.None;
                    break;
                default:
                    Debug.LogError($"OnSwitchEndMatchEvent 回流但当前状态异常 unexpected current state: {State}");
                    break;
            }

            CloudSyncSdk.InternalInvokeEvent("OnEndMatchGame", () => OnEndMatchGame?.Invoke(endEvent.EndInfo));
            InvokeEvent("OnEndMatchEvent", () => OnEndMatchEvent?.Invoke(endEvent));
        }

        private void OnBeginHost(IHostRoom hostRoom)
        {
            if (hostRoom == null)
                return;
            const CloudMatchState toState = CloudMatchState.InGameAsHost;
            if (State == toState)
                return;
            Debug.Log($"切流状态更新 OnBeginHost: {toState}");
            State = toState;
            _hostRoom = hostRoom;
        }

        private void OnSwitchTo(SwitchResult result)
        {
            if (!result.Success)
                return;
            const CloudMatchState toState = CloudMatchState.InConnectOtherRoom;
            if (State == toState)
                return;
            Debug.Log($"切流状态更新 OnSwitchTo: {toState}");
            State = toState;
        }
    }
}