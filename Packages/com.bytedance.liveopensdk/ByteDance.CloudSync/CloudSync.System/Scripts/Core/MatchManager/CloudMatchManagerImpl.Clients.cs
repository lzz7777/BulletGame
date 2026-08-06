using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ByteDance.CloudSync.MatchManager
{
    internal partial class CloudMatchManagerImpl
    {
        #region 玩家用户信息

        /// <inheritdoc cref="IAnchorPlayerInfoProvider.FetchPlayerInfo"/>
        public async Task<AnchorPlayerInfo> FetchPlayerInfo(CancellationToken cancelToken)
        {
            Debug.Log("FetchPlayerInfo self");
            if (MyPlayerInfo != null)
                return MyPlayerInfo;

            Debug.Log("FetchPlayerInfo 云同步-获取主播用户信息 ...");
            _playerInfoOp = new FetchPlayerInfoOperation();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken, OnDestroyToken);
            var result = await _playerInfoOp.FetchPlayerInfo(cts.Token);
            var playerInfo = result?.PlayerInfo;
            _playerInfoOp = null;
            var seat = GetHostSeat();
            if (playerInfo != null)
            {
                Debug.Assert(seat != null, "Assert seat");
                Debug.Assert(seat?.Client != null, "Assert hostClient");
                CloudSync.UpdateAnchorPlayerInfo(playerInfo);
            }

            Debug.Log($"FetchPlayerInfo 云同步-获取主播用户信息 result: {playerInfo?.ToStr()}");
            return playerInfo;
        }

        public async Task<AnchorPlayerInfo> FetchOnJoinPlayerInfo(ICloudSeat seat, CancellationToken cancelToken)
        {
            Debug.Log("FetchPlayerInfo multi");
            var index = seat.Index;
            if (seat.IsHost())
            {
                Debug.Log($"FetchPlayerInfo client.IsHost() index: {index}");
                return await FetchPlayerInfo(cancelToken);
            }

            var user = GetPlayerUser(index);
            if (user == null)
            {
                Debug.LogError($"FetchPlayerInfo user not found!  for index: {index}");
                return null;
            }

            Debug.LogDebug($"FetchPlayerInfo index: {index} result: {user.ToStr()}");
            return user.ToPlayerInfo();
        }

        Task<string> ICloudSwitchTokenProvider.GetToken(ICloudSeat seat, CancellationToken cancellationToken)
        {
            var client = seat.Client;
            var index = client.Index;
            Debug.LogDebug($"GetToken client index: {index}");
            Debug.Assert(!client.IsHost(),$"GetToken unexpected client.IsHost() ! index: {index}.");
            var user = GetPlayerUser(index);
            var switchToken = GetSwitchToken(user);
            return Task.FromResult(switchToken);
        }

        /// <summary>
        /// 房主Seat
        /// </summary>
        private ICloudSeat GetHostSeat() => CloudSync.SeatManager?.GetSeat(SeatIndex.Index0);

        /// <summary>
        /// 房主Client
        /// 注意，当初始化拉取直播用户信息失败时，可能返回 client 为 null
        /// </summary>
        private ICloudClient GetHostClient() => CloudSync.ClientManager.GetHostClient();

        private List<ICloudClient> GetNonHostClients()
        {
            var clients = CloudSync.GetClients();
            var nonHosts = clients.Where(s => !s.IsHost()).ToList();
            return nonHosts;
        }

        /// <summary>
        /// 获取 SwitchManager 所需使用的token
        /// </summary>
        internal static string GetSwitchToken(MatchResultUser user)
        {
            var index = user?.RoomIndex;
            var streamInfo = user?.CloudStreamInfo as MatchCloudGameInfo;
            var anchorPlayerInfo = user?.ToPlayerInfo();
            var token = streamInfo?.cloudGameToken;
            var rtcUserId = streamInfo?.rtcUserId;
            var switchToken = CloudSwitchManager.MakeToken(token, anchorPlayerInfo, rtcUserId);
            Debug.Assert(user != null, $"matching user not found! index: {index}");
            Debug.Assert(streamInfo != null, $"user streamInfo is null! index: {index}");
            Debug.LogDebug($"GetSwitchToken index: {index}, result token: {token}");
            return switchToken;
        }

        private MatchResultUser GetPlayerUser(SeatIndex index)
        {
            var users = MatchUsersResult?.AllUsers;
            var user = users?.FirstOrDefault(s => s.RoomIndex == index);
            Debug.Assert(MatchUsersResult != null, $"GetPlayerUser MatchUsersResult is null! index: {index}. Maybe Match not requested or not succeeded.");
            Debug.Assert(users != null, "GetPlayerUser users is null");
            Debug.Assert(user != null, $"GetPlayerUser matching user not found! index: {index}");
            return user;
        }

        #endregion

        #region 连接状态

        private void OnClientState(ICloudClient client, ClientState state)
        {
            if (_waitMyClientConnect && client.IsHost())
                UpdateMyClientData(client, state);

            if (State == CloudMatchState.InGameAsHost)
            {
                Debug.LogDebug($"OnClientState NonHost players states: {GetNonHostStatesDebugInfo()}");
                switch (state)
                {
                    case ClientState.Disconnecting:
                        // 其他玩家退出同玩时，房主侧不强制退出同玩，开发者可以自己调用`结束同玩`的接口。
                        if (IsNoOtherPlayerConnected())
                            Debug.LogDebug("NoOtherPlayerConnected 其他玩家都已退出同玩。房主侧不强制退出，开发者可以在做好合适的展示后，自己调用`结束同玩`接口");
                        break;
                }
            }
        }

        private void OnClientStateChanged(ICloudClient client, ClientState state)
        {
            OnClientState(client, client.State);
        }

        private static bool IsConnect(ICloudClient client)
        {
            return client is { State: ClientState.Connecting or ClientState.Connected };
        }

        private static bool IsNotConnect(ICloudClient client) => !IsConnect(client);

        private void UpdateMyClientData(ICloudClient client, ClientState state)
        {
            Debug.Log($"UpdateMyClientData Index: {client.Index} state: {state} ({(int)state})");
            switch (state)
            {
                case ClientState.Connecting:
                {
                    MyCloudGameInfo.rtcUserId = client.RtcUserId;
                    MyCloudGameInfo.roomToken = Env.IsMockWebcastAuth() ? Env.GetMockLaunchToken() : Env.GetLaunchToken();
                    Debug.Log($"UpdateMyClientData set MyCloudGameInfo.rtcUserId: {MyCloudGameInfo.rtcUserId}");
                    break;
                }
                case ClientState.Connected:
                {
                    _waitMyClientConnect = false;
                    break;
                }
            }
        }

        /// 没有别的玩家在连着房主的流
        private bool IsNoOtherPlayerConnected()
        {
            var nonHosts = GetNonHostClients();
            var noOther = nonHosts.All(IsNotConnect);
            Debug.LogDebug($"IsNoOtherPlayerConnected {noOther}");
            return noOther;
        }

        private string GetNonHostStatesDebugInfo(string infoIfNone = "")
        {
            var nonHosts = GetNonHostClients();
            if (nonHosts.Count == 0)
                return infoIfNone;
            var debugInfo = string.Join(",", nonHosts.Select(s => $"{s.Index}: {s.State}"));
            return debugInfo;
        }

        #endregion
    }
}