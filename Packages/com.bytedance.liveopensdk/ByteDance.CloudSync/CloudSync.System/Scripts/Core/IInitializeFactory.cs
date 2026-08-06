using System.Threading.Tasks;
using ByteDance.CloudSync.MatchManager;

namespace ByteDance.CloudSync
{
    public enum InitPhase
    {
        AfterSdk,
        AfterSplash,
        BeforeDevice,
        AfterDevice,
        BeforeManagers,
        AfterManagers,
    }

    internal interface IInitWorker
    {
        bool IsWorkFor(InitPhase phase);
        Task WorkOnInit(InitPhase phase);
    }

    /// <summary>
    /// InitializeFactory 接口，用于创建 CloudGame 相关的组件
    /// </summary>
    internal interface IInitializeFactory
    {
        IInitWorker InitWorker => null;

        ICloudSwitchManagerEx CreateCloudSwitchManager();

        ICloudMatchManagerEx CreateCloudMatchManager();

        IAnchorPlayerInfoProvider CreateHostPlayerInfoProvider();

        IMultiAnchorPlayerInfoProvider CreateNonHostPlayerInfoProvider();
    }

    /// <summary>
    /// Mock 环境的 InitializeFactory 实现，用于测试环境
    /// </summary>
    internal interface IMockInitializeFactory : IInitializeFactory
    {
        ICloudGameAPI CreateCloudGameAPI();
    }

    /// <summary>
    /// 默认的 InitializeFactory 实现，正式环境中的
    /// </summary>
    internal class DefaultInitializeFactory : IInitializeFactory
    {
        private CloudSwitchManager _switchManager;

        private CloudSwitchManager SwitchManager => _switchManager ??= new();

        private CloudMatchManagerImpl _matchManager;

        private CloudMatchManagerImpl MatchManager
        {
            get
            {
                if (_matchManager == null)
                {
                    var env = CloudSyncSdk.Env;
                    var switchManager = CloudSyncSdk.InternalCurrent.SwitchManager as ICloudSwitchManagerEx;
                    _matchManager = new CloudMatchManagerImpl(env, switchManager);
                }

                return _matchManager;
            }
        }

        public ICloudSwitchManagerEx CreateCloudSwitchManager() => SwitchManager;

        public ICloudMatchManagerEx CreateCloudMatchManager() => MatchManager;

        public IAnchorPlayerInfoProvider CreateHostPlayerInfoProvider() => MatchManager;

        public IMultiAnchorPlayerInfoProvider CreateNonHostPlayerInfoProvider() => SwitchManager;
    }
}