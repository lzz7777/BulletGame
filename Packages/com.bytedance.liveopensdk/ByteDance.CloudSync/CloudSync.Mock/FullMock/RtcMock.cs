using System.Threading.Tasks;
using ByteDance.CloudSync.MatchManager;

namespace ByteDance.CloudSync.Mock
{
    internal static class RtcMock
    {
        public static RtcMockSettings MockSettings { get; } = new();

        internal static readonly RtcMockCloudGameAPI CloudGameAPI = new();
        internal static readonly RtcMockMatchService MatchService = new();
        internal static CloudMatchManagerImpl MatchManager => _matchManager ??= CreateMatchManager();
        internal static CloudSwitchManager SwitchManager => _switchManager ??= new CloudSwitchManager();
        private static readonly IInitWorker InitWorker = new RtcMockInitWorker();

        private static CloudMatchManagerImpl _matchManager;
        private static CloudSwitchManager _switchManager;

        internal static void Setup()
        {
            var context = GetInitContext();
            var system = CloudSyncSdk.GetInstance();
            system.SetMock(context);
        }

        private static CloudMatchManagerImpl CreateMatchManager()
        {
            var env = CloudSyncSdk.Env;
            var switchManager = SwitchManager;
            var sdkApi = CloudGameAPI;
            var matchService = MatchService;
            var matchManager = new CloudMatchManagerImpl(env, switchManager, matchService, sdkApi);
            return matchManager;
        }

        internal static MockInitContext GetInitContext()
        {
            return new MockInitContext
            {
                Name = nameof(RtcMock),
                CloudGameAPI = CloudGameAPI,
                MatchManager = MatchManager,
                SwitchManager = SwitchManager,
                MatchService = MatchService,
                PlayerInfoProvider = MatchManager,
                NonHostPlayerInfoProvider = SwitchManager,
                InitWorker = InitWorker
            };
        }

        internal static void OnPodInstanceReady()
        {
            MatchService.OnPodInstanceReady(PodInstance.AgentDataChannel);
        }

        internal static void OnAgentClosed()
        {
            if (MockPlay.Instance != null)
                MockPlay.Instance.OnDisconnected();
        }
    }

    internal class RtcMockInitWorker : IInitWorker
    {
        public bool IsWorkFor(InitPhase phase)
        {
            return phase == InitPhase.AfterSdk;
        }

        public async Task WorkOnInit(InitPhase phase)
        {
            while (!RtcMock.MockSettings.IsInitialized)
                await Task.Yield();
        }
    }
}