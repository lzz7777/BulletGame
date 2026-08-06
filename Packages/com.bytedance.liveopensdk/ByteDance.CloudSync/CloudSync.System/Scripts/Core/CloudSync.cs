using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ByteDance.CloudSync.MatchManager;
using ByteDance.CloudSync.TeaSDK;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace ByteDance.CloudSync
{
    internal delegate void ClientEventHandler(ICloudClient client);

    internal delegate void ClientStateEventHandler(ICloudClient client, ClientState state);

    internal interface ICloudSyncUsageEditor
    {
        void SetUsingCloudSync(bool value);
    }

    internal partial class CloudSyncSdk : MonoBehaviour, ICloudSync
    {
        private const string Tag = nameof(CloudSyncSdk);

        /// <summary>
        /// 全局单例 Env
        /// </summary>
        public static ICloudSyncEnv Env => InternalEnv;

        public bool IsInitialized => _initialized;

        private static CloudSyncSdk _current;
        private static NewInputSystem _newInputSystem;

        internal static CloudSyncSdk InternalCurrent => _current;

        /// <summary>
        /// 获取或者创建 ICloudSyncSdk
        /// </summary>
        public static CloudSyncSdk GetInstance() => _current ?? Load();

        private static CloudGameSdkManager _sdkManager;

        /// <summary>
        /// 全局单例 Sdk Manager
        /// </summary>
        internal static CloudGameSdkManager SdkManager => _sdkManager ??= new CloudGameSdkManager();

        /// <summary>
        /// 是否已经初始化
        /// </summary>
        public static bool Initialized => _current != null && _current._initialized;

        /// <summary>
        /// 发生 Destroy
        /// </summary>
        public CancellationToken OnDestroyToken => _destroyTokenSource?.Token ?? PreCancelledToken;

        private static CloudSyncEnv _env;
        internal static CloudSyncEnv InternalEnv => _env ??= new CloudSyncEnv();

        private bool _initialized;
        private CancellationTokenSource _destroyTokenSource = new();

        internal static event Action<ICloudSync> OnLoad;

        internal event Action<InitResult> OnInitialized;

        /// <inheritdoc cref="ICloudSync.OnMouse"/>
        public event MouseEventHandler OnMouse;

        /// <inheritdoc cref="ICloudSync.OnKeyboard"/>
        public event KeyboardEventHandler OnKeyboard;

        /// <inheritdoc cref="ICloudSync.OnTouch"/>
        [Obsolete("已废弃! Use event `ICloudSync.OnTouches` instead. (with param `RemoteTouchesEvent`)", true)]
        public event TouchEventHandler OnTouch;

        /// <inheritdoc cref="ICloudSync.OnTouches"/>
        public event TouchesEventHandler OnTouches;

        /// <inheritdoc cref="ICloudSync.OnSeatPlayerJoined"/>
        public event SeatEventHandler OnSeatPlayerJoined;

        /// <inheritdoc cref="ICloudSync.OnSeatPlayerLeaving"/>
        public event SeatEventHandler OnSeatPlayerLeaving;

        internal event ClientStateEventHandler OnClientStateChanged;

        internal event ClientEventHandler OnClientConnected;

        internal event ClientEventHandler OnClientDisconnected;

        /// <inheritdoc cref="ICloudSync.OnWillDestroy"/>
        public event WillDestroyHandler OnWillDestroy;

        private IInitializeFactory _defaultInitializeFactory;
        private IInitializeFactory _initializeFactory;
        private CloudClientManager _clientManager;
        private ICloudMatchManagerEx _matchManager;
        private ICloudSwitchManagerEx _switchManager;
        private IAnchorPlayerInfoProvider _playerInfoProvider;
        private IMultiAnchorPlayerInfoProvider _nonHostPlayerInfoProvider;
        private readonly List<ISafeActionsUpdatable> _updatables = new();
        private readonly FpsUpdatable _fpsUpdatable = new();
        private readonly ResolutionUpdatable _resolutionUpdatable = new();
        private const int SDK_INIT_MAX_RETRY = 3;
        private static readonly SdkDebugLogger Debug = new("CloudSync");

        internal IInitializeFactory InitializeFactory => _initializeFactory;

        public ICloudClientManager ClientManager => _clientManager;

        public ICloudSeatManager SeatManager => _clientManager;

        public ICloudMatchManager MatchManager => _matchManager;

        public ICloudSwitchManager SwitchManager => _switchManager;

        public IAnchorPlayerInfo AnchorPlayerInfo { get; private set; }

        public void UpdateAnchorPlayerInfo(IAnchorPlayerInfo playerInfo)
        {
            if (playerInfo == null)
                return;
            CGLogger.Log($"更新主播用户信息 AnchorPlayerInfo: roomId: {playerInfo.LiveRoomId} openId: {playerInfo.OpenId}, nickName: {playerInfo.NickName}");
            AnchorPlayerInfo = playerInfo;
            var host = SeatManager?.GetSeat(SeatIndex.Index0);
            host?.Client?.UpdatePlayerInfo(playerInfo);
        }

        internal static CancellationToken PreCancelledToken => CancelUtil.PreCancelledToken;
        internal static ICloudSyncUsageEditor CloudSyncUsageEditor { get; set; }
        private static bool HasCheckCloudSyncUsageEditor { get; set; }

        private static CloudSyncSdk Load()
        {
            if (_current)
                return _current;

            TeaReport.Report_cloudgamesystem_load_start();
            CGLogger.Log("-------- Load --------");
            CGLogger.Log($"CloudSyncSdk Load, frame: {Time.frameCount}");
            CGLogger.LogDebug($"CloudSyncSdk Load, stack: {StackTraceUtility.ExtractStackTrace()}");
            var cloudGame = Instantiate(Resources.Load<GameObject>("Prefabs/CloudGameSystem"));
            cloudGame.name = Tag;
            cloudGame.hideFlags |= HideFlags.DontSave;
            DontDestroyOnLoad(cloudGame);
            _current = cloudGame.AddComponent<CloudSyncSdk>();
            Debug.Assert(_newInputSystem != null, "Assert NewInputSystem component");
            InternalInvokeEvent(nameof(OnLoad), () => OnLoad?.Invoke(_current));
            TeaReport.Report_cloudgamesystem_load_end();
            return _current;
        }

        private void Awake()
        {
            // CGLogger.Log("云同步-CloudSync Awake"); // local debug only
            _newInputSystem = GetComponent<NewInputSystem>();
            Debug.Assert(_newInputSystem != null, "Assert NewInputSystem component");
            NotifyUsingCloudSync(true);
        }

        private void OnValidate()
        {
            // CGLogger.Log("云同步-CloudSync OnValidate"); // local debug only
        }

        internal bool IsMock() => _initializeFactory is IMockInitializeFactory;

        /// <summary>
        /// MOCK 下使用：设置 Mock 实现
        /// </summary>
        internal void SetMock(IMockInitializeFactory factory)
        {
            _initializeFactory = factory ?? _defaultInitializeFactory;
            CloudGameSdk.SetupMockApi(factory?.CreateCloudGameAPI());
            CGLogger.Log($"CloudSyncSdk SetMock: {factory != null} {factory}");
        }

        /// <summary>
        /// MOCK 下使用：覆盖 Env 设置
        /// </summary>
        /// <param name="env"></param>
        internal void OverrideEnv(IWritableEnv env)
        {
            InternalEnv.OverrideWith(env);
        }

        public async Task<InitResult> Init(string appId,
            IVirtualDeviceFactory deviceFactory,
            IAnchorPlayerInfoProvider playerInfoProvider = null,
            ISplashScreen splash = null)
        {
            CGLogger.Log("-------- Init --------");
            CGLogger.Log($"CloudSyncSdk Init 云同步-初始化 version: {Version}{PatchVersion}, frame: {Time.frameCount}");
            NotifyUsingCloudSync(true);
            AddUpdatable(_fpsUpdatable.Init());
            AddUpdatable(_resolutionUpdatable.Init());
            TeaReportBase.UpdateCommonParams("appid", appId);
            TeaReportBase.UpdateCommonParams("version", Version);
            TeaReportBase.UpdateCommonParams("app_version", Application.version);
            TeaReport.Report_cloudgamesystem_init_start();
            InitCloudGameResult initCloudGameResult = await InitSdk(SDK_INIT_MAX_RETRY);
            if (!initCloudGameResult.IsSuccess())
            {
                CGLogger.LogError($"CloudSyncSdk Init 云同步-初始化失败！ {initCloudGameResult.ToStr()}");
                TeaReport.Report_cloudgamesystem_init_end(false);
                return new InitResult().Accept(initCloudGameResult);
            }

            InternalEnv.AppId = appId;
            CloudGameConfigLoader.TryOverrideEnv(InternalEnv);
            _defaultInitializeFactory ??= new DefaultInitializeFactory();
            _initializeFactory ??= _defaultInitializeFactory;
            _current = this;
            if (_newInputSystem != null)
                _newInputSystem.OnInitSdk();

            await RunInitWorker(InitPhase.AfterSdk);
            await RunInitWorker(SplashScreenWorker.Create(splash), InitPhase.AfterSdk);
            await RunInitWorker(InitPhase.AfterSplash);

            Debug.Log($"CloudSyncSdk deviceId: >>>: {SystemInfo.deviceUniqueIdentifier}");
            await RunInitWorker(InitPhase.BeforeDevice);
            TeaReport.Report_cloudclientmanager_init_start();
            _clientManager = new CloudClientManager(deviceFactory);
            _clientManager.Initialize();
            TeaReport.Report_cloudclientmanager_init_end();
            await RunInitWorker(InitPhase.AfterDevice);

            await RunInitWorker(InitPhase.BeforeManagers);
            InitManagers();
            await RunInitWorker(InitPhase.AfterManagers);

            // 初始化主播用户信息
            {
                TeaReport.Report_anchor_wait_join_start();
                var client = await _clientManager.WaitConnected(SeatIndex.Index0, CancellationToken.None);
                UpdateAnchorPlayerInfo(client?.PlayerInfo as IAnchorPlayerInfo);
                TeaReport.Report_anchor_wait_join_end();
            }

            _initialized = true;
            var result = new InitResult { Code = InitResultCode.Success };
            CGLogger.Log($"CloudSyncSdk Init 云同步-初始化成功 {result.ToStr()}");
            InternalInvokeEvent(nameof(OnInitialized), () => OnInitialized?.Invoke(result));
            TeaReport.Report_cloudgamesystem_init_end(true);
            return result;
        }

        private Task RunInitWorker(InitPhase phase)
        {
            return RunInitWorker(_initializeFactory.InitWorker, phase);
        }

        private async Task RunInitWorker(IInitWorker worker, InitPhase phase)
        {
            if (worker == null)
                return;
            if (!worker.IsWorkFor(phase))
                return;
            CGLogger.LogDebug($"Run init worker {worker.GetType()}, phase: {phase}...");
            await worker.WorkOnInit(phase);
            CGLogger.LogDebug($"Run init worker {phase} done.");
        }

        private async Task<InitCloudGameResult> InitSdk(int maxRetry)
        {
            InitCloudGameResult result = default;
            // 自动重试n次
            for (int i = 0; i < maxRetry; i++)
            {
                result = await SdkManager.InitializeSdk();
                if (result.IsSuccess())
                    return result;
                await Task.Yield();
            }

            // 仍然失败，返回result
            return result;
        }

        private void InitManagers()
        {
            if (_initializeFactory is IMockInitializeFactory)
                CGLogger.LogWarning("!!! Mock InitializeFactory is used !!!");

            _switchManager = _initializeFactory.CreateCloudSwitchManager();
            _switchManager.OnEndMatchEvent += OnEndMatch;
            _matchManager = _initializeFactory.CreateCloudMatchManager();
            _playerInfoProvider ??= _initializeFactory.CreateHostPlayerInfoProvider();
            _nonHostPlayerInfoProvider = _initializeFactory.CreateNonHostPlayerInfoProvider();

            _matchManager.Initialize();
            _switchManager.Initialize();
        }

        internal IMultiAnchorPlayerInfoProvider GetOnJoinPlayerInfoProvider(ICloudSeat seat)
        {
            if (seat.IsHost())
                return new SelfAnchorPlayerInfoProvider(_playerInfoProvider);
            if (_nonHostPlayerInfoProvider == null)
                throw new InvalidOperationException("'_nonHostPlayerInfoProvider' is not set!");
            return _nonHostPlayerInfoProvider;
        }

        private void OnEnable()
        {
            if (_newInputSystem != null)
                _newInputSystem.OnEnableSdk();
            CloudGameSystemEarlyUpdateSystem.OnEarlyUpdate += EarlyUpdate;
        }


        private void OnDisable()
        {
            if (_newInputSystem != null)
                _newInputSystem.OnDisableSdk();
            CloudGameSystemEarlyUpdateSystem.OnEarlyUpdate -= EarlyUpdate;
        }

        private void OnDestroy()
        {
            CGLogger.Log("-------- OnDestroy --------");
            _current = null;
            _clientManager?.Dispose();
            _clientManager = null;
            _sdkManager?.Dispose();
            _sdkManager = null;
            _matchManager?.Dispose();
            _switchManager?.Dispose();
            _updatables.Clear();
            VirtualDeviceSystem.DestroyAll();
            VirtualScreenSystem.DestroyAll();
            var cts = _destroyTokenSource;
            _destroyTokenSource = null;
            cts.Cancel();
            cts.Dispose();
        }

        internal void AddUpdatable(ISafeActionsUpdatable updatable)
        {
            if (_updatables.Contains(updatable))
                return;
            CGLogger.LogDebug($"AddUpdatable {updatable}");
            _updatables.Add(updatable);
        }

        internal bool RemoveUpdatable(ISafeActionsUpdatable updatable)
        {
            CGLogger.LogDebug($"RemoveUpdatable {updatable}");
            return _updatables.Remove(updatable);
        }

        private void Update()
        {
            // todo: split Input actions (playerOperate) for UnityThreadListener, and process dequeue in EarlyUpdate.
            _sdkManager?.Update();
            _clientManager?.Update();
            foreach (var updatable in _updatables)
            {
                updatable.Update();
            }
        }

        private void EarlyUpdate()
        {
            _clientManager?.EarlyUpdate();
        }

        internal void OnRemoteMouseEvent(in RemoteMouseEvent e)
        {
            if (OnMouse == null)
                return;
            var seat = GetInputSeat(e);
            if (seat == null)
                return;
            OnMouse.Invoke(seat, e);
        }

        public void OnRemoteKeyboardEvent(in RemoteKeyboardEvent e)
        {
            if (OnKeyboard == null)
                return;
            var seat = GetInputSeat(e);
            if (seat == null)
                return;

            OnKeyboard.Invoke(seat, e);
        }

        public void OnRemoteTouchesEvent(RemoteTouchesEvent e)
        {
            if (OnTouches == null)
                return;
            var seat = GetInputSeat(e);
            if (seat == null)
                return;
            OnTouches.Invoke(seat, e);
        }

        private CloudSeat GetInputSeat(IRemoteInputEvent e) => _clientManager.GetSeat(e.device.Index);

        internal void OnClientStateChange(ICloudClient client, ClientState state)
        {
            var index = client.Index;
            InternalInvokeEvent($"OnClientStateChanged {index} {state}", () => OnClientStateChanged?.Invoke(client, state));
            switch (state)
            {
                case ClientState.Connected:
                    InternalInvokeEvent($"OnClientConnected {index}", () => OnClientConnected?.Invoke(client));
                    break;
                case ClientState.Disconnecting:
                    InternalInvokeEvent($"OnClientDisconnected {index}", () => OnClientDisconnected?.Invoke(client));
                    if (client.IsHost())
                        WillDestroy();
                    break;
            }
        }

        public void OnSeatStateChanged(ICloudSeat seat, SeatState state)
        {
            switch (state)
            {
                case SeatState.InUse:
                    InvokeEvent($"ICloudSync.{nameof(OnSeatPlayerJoined)} {seat.Index}", () => OnSeatPlayerJoined?.Invoke(seat));
                    break;
                case SeatState.Empty:
                    InvokeEvent($"ICloudSync.{nameof(OnSeatPlayerLeaving)} {seat.Index}", () => OnSeatPlayerLeaving?.Invoke(seat));
                    break;
            }
        }

        /// <summary>
        /// Host Client 退出时，触发其它端 OnWillDestroy 事件
        /// </summary>
        private void WillDestroy()
        {
            CGLogger.Log("-------- WillDestroy --------");
            CGLogger.Log($"CloudSyncSdk WillDestroy 云同步-事件：即将销毁（关闭玩法、退出云游戏）, frame: {Time.frameCount}");
            var info = new DestroyInfo
            {
                Time = 10,
                Reason = DestroyReason.HostAnchorLeave
            };

            foreach (var seat in _clientManager.AllSeats)
            {
                seat.WillDestroy(info);
            }

            InvokeEvent($"ICloudSync.{nameof(OnWillDestroy)}", () => OnWillDestroy?.Invoke(info));
        }

        private void OnEndMatch(IEndEvent endEvent)
        {
            var info = endEvent.EndInfo;
            CGLogger.Log($"CloudSyncSdk OnEndMatch, has info: {!string.IsNullOrEmpty(info)}, frame: {Time.frameCount}");
        }

        // note: 给每个重要的抛事件加上，便于排查，便于QA验证
        internal static void InternalInvokeEvent(string name, Action action)
        {
            Debug.Log($"Notify event: {name}");
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception during 事件回调：`{name}` {e}");
            }
        }

        // note: 给每个重要的抛事件加上，便于排查，便于QA验证
        internal static void InvokeEvent(string name, Action action)
        {
            var startTime = DateTime.UtcNow;
            Debug.Log($"事件回调 Notify event: {name}");
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception during 事件回调：`{name}`, 请检查你的接入代码！ {e}");
            }
            finally
            {
                var endTime = DateTime.UtcNow;
                float elapsedMs = (float)(endTime - startTime).TotalMilliseconds;
                Debug.Log($"事件回调完成 event: {name} 执行耗时: {elapsedMs:F2}ms");
            }
        }

        internal void ResetEnv()
        {
            _env = null;
        }

        internal static void NotifyUsingCloudSync(bool value)
        {
            if (!Application.isEditor)
                return;
            if (CloudSyncUsageEditor == null && !HasCheckCloudSyncUsageEditor)
            {
                HasCheckCloudSyncUsageEditor = true;
                CGLogger.LogError("Assert CloudSyncUsageEditor failed!");
            }

            CloudSyncUsageEditor?.SetUsingCloudSync(value);
        }
    }

    internal interface ICloudManager : IDisposable
    {
        void Initialize();
    }
}