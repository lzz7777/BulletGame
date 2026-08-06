using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ByteDance.CloudSync.Match;
using ByteDance.CloudSync.MatchManager;
using ByteDance.CloudSync.Mock;
using UnityEngine;

namespace ByteDance.CloudSync
{
    internal static class SimpleMock
    {
        public static void Setup()
        {
            var system = CloudSyncSdk.GetInstance();
            system.SetMock(new MockInitContext
            {
                Name = nameof(SimpleMock),
                CloudGameAPI = SimpleMockCloudGameAPI.Instance,
                MatchManager = new SimpleMockMatchManager(),
                PlayerInfoProvider = new SimpleMockPlayerInfoProvider(),
                NonHostPlayerInfoProvider = SimpleMockCloudGameAPI.Instance,
                SwitchManager = new CloudSwitchManager(),
            });
        }

        /// <summary>
        /// 请不要主动调用此接口，直接打开 Screen - 2 可自动 MockJoin
        /// </summary>
        internal static async Task MockJoin(SeatIndex index)
        {
            var client = CloudSyncSdk.InternalCurrent.SeatManager.GetSeat(index);
            if (client.State == SeatState.InUse)
                return;
            SimpleMockCloudGameAPI.Instance.OnJoin(index, MockUtils.MockRandomPlayerInfo());
            await client.WaitJoin(CancellationToken.None);
        }

        internal static void MockExit(SeatIndex index)
        {
            SimpleMockCloudGameAPI.Instance.OnExit(index);
        }
    }

    internal class SimpleMockPlayerInfoProvider : IAnchorPlayerInfoProvider
    {
        public Task<AnchorPlayerInfo> FetchPlayerInfo(CancellationToken token)
        {
            var info = MockUtils.MockRandomPlayerInfo();
            return Task.FromResult(info);
        }
    }

    internal class SimpleMockMatchManager : BaseMockMatchManager
    {
    }

    internal class BaseMockMatchManager : ICloudMatchManagerEx
    {
        public virtual void Dispose()
        {
        }

        public virtual void Initialize()
        {
        }

        public IHostRoom HostRoom => null;

        public void SetInners(ICloudSwitchManagerEx switchManager, IMatchService customService, ICloudGameAPI customSdkAPI, ICloudSyncEnv customEnv)
        {
            if (customService != null)
            {
                BaseMatchOperation.MatchService = customService;
            }

            if (customSdkAPI != null)
            {
                BaseMatchOperation.MatchAPI = customSdkAPI;
            }
        }

#pragma warning disable CS0067 // Event is never used
        public event MatchUsersHandler OnMatchUsers;
        public event EndMatchEventHandler OnEndMatchEvent;
        [System.Obsolete("已废弃! Use new event: `OnEndMatchEvent` instead.", true)]
        public event EndMatchGameHandler OnEndMatchGame;
#pragma warning restore CS0067

        public CloudMatchState State => CloudMatchState.None;

        public virtual CloudMatchOptionsEx OptionsEx { get; } = new();

        private static NotSupportedException NotSupported() => new("MockType.Simple 模式下不支持此操作");

        public virtual Task<IMatchResult> RequestMatch(SimpleMatchConfig config, CancellationToken cancelToken = default) => Task.FromResult(ErrorMatchResult());

        public virtual Task<IMatchResult> RequestMatch(MatchConfig config, string matchParamJson) => Task.FromResult(ErrorMatchResult());

        public virtual Task<IMatchResult> RequestMatch(MatchConfig config, string matchParamJson, CancellationToken cancelToken) => Task.FromResult(ErrorMatchResult());

        public virtual Task<IEndResult> EndMatchGame(string endInfo = "") => throw NotSupported();

        public Task<IEndResult> EndMatchGame(InfoMapping infoMapping) => throw NotSupported();

        public virtual Task<IEndResult> EndMatchGame(SeatIndex seatIndex, string endInfo = "") => throw NotSupported();

        public Task<string> GetToken(ICloudSeat seat, CancellationToken cancellationToken) => throw NotSupported();

        static IMatchResult ErrorMatchResult()
        {
            var message = "error: MockType.Simple 模式下不支持此操作";
            Debug.LogError(message);
            return new BaseMockMatchResult
            {
                Code = MatchResultCode.Error,
                Message = message
            };
        }
    }

    internal class BaseMockMatchResult : IMatchResult
    {
        public bool IsSuccess => Code == MatchResultCode.Success;
        public MatchResultCode Code { get; internal set; }
        public string Message { get; internal set; }
        public string MatchId { get; }
        public bool IsHost { get; }
        public SeatIndex MyIndex { get; }
        public MatchResultUser HostUser { get; }
        public List<MatchResultTeam> Teams { get; }
    }
}