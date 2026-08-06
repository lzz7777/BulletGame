using ByteDance.CloudSync.Match;
using ByteDance.CloudSync.MatchManager;

namespace ByteDance.CloudSync
{
    /// <summary>
    /// Mock环境的初始化Context
    /// </summary>
    internal class MockInitContext
    {
        public string Name;
        public ICloudGameAPI CloudGameAPI;
        public ICloudMatchManagerEx MatchManager;
        public ICloudSwitchManagerEx SwitchManager;
        public IMatchService MatchService;
        public IAnchorPlayerInfoProvider PlayerInfoProvider;
        public IMultiAnchorPlayerInfoProvider NonHostPlayerInfoProvider;
        public IInitWorker InitWorker;

        public IMockInitializeFactory AsFactory()
        {
            return new MockInitializeFactory(this);
        }
    }

    /// <summary>
    /// Mock 环境的 InitializeFactory 实现，用于测试环境
    /// </summary>
    internal class MockInitializeFactory : IMockInitializeFactory
    {
        private readonly MockInitContext _context;
        private readonly IInitializeFactory _defaultFactory = new DefaultInitializeFactory();

        public MockInitializeFactory(MockInitContext context)
        {
            _context = context;
        }

        public IInitWorker InitWorker => _context.InitWorker;

        public ICloudSwitchManagerEx CreateCloudSwitchManager()
        {
            return _context.SwitchManager ?? _defaultFactory.CreateCloudSwitchManager();
        }

        public ICloudMatchManagerEx CreateCloudMatchManager()
        {
            return _context.MatchManager ?? _defaultFactory.CreateCloudMatchManager();
        }

        public IAnchorPlayerInfoProvider CreateHostPlayerInfoProvider()
        {
            return _context.PlayerInfoProvider ?? _defaultFactory.CreateHostPlayerInfoProvider();
        }

        public IMultiAnchorPlayerInfoProvider CreateNonHostPlayerInfoProvider()
        {
            return _context.NonHostPlayerInfoProvider ?? _defaultFactory.CreateNonHostPlayerInfoProvider();
        }

        public ICloudGameAPI CreateCloudGameAPI()
        {
            return _context.CloudGameAPI;
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(_context.Name) ? base.ToString() : _context.Name;
        }
    }

    internal static class MockEnv
    {
        public const string ArgMockWebcastAuth = "mock-webcast-auth";
        public const string ArgMockLaunchToken = "mock-launch-token";
    }

    internal static class MockExtensions
    {
        /// <summary>
        /// 设置 Mock 实现
        /// </summary>
        public static void SetMock(this CloudSyncSdk system, MockInitContext settings)
        {
            system.SetMock(settings?.AsFactory());
        }

        public static IMockInitializeFactory GetMockFactory(this CloudSyncSdk system)
        {
            return system.InitializeFactory as IMockInitializeFactory;
        }

        public static void SetCloudGameToken(this ICloudSyncEnv env, string token)
        {
            var modifiableEnv = (IWritableEnv)env;
            modifiableEnv.SetValue(SdkConsts.ArgAppCloudGameToken, token);
        }

        public static void SetMockWebcastAuth(this ICloudSyncEnv env, bool value)
        {
            var modifiableEnv = (IWritableEnv)env;
            modifiableEnv.SetValue(MockEnv.ArgMockWebcastAuth, value ? 1 : 0);
        }

        public static bool IsMockWebcastAuth(this IEnv env)
        {
            return env.GetIntValue(MockEnv.ArgMockWebcastAuth) == 1;
        }

        public static void SetMockLaunchToken(this ICloudSyncEnv env, string value)
        {
            var modifiableEnv = (IWritableEnv)env;
            modifiableEnv.SetValue(MockEnv.ArgMockLaunchToken, value);
        }

        public static string GetMockLaunchToken(this IEnv env)
        {
            return env.GetStringValue(MockEnv.ArgMockLaunchToken);
        }
    }
}