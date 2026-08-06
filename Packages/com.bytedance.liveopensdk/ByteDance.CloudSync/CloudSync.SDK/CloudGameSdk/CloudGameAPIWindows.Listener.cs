namespace ByteDance.CloudSync
{
    internal partial class CloudGameAPIWindows
    {
        private readonly MultiplayerCallbacksAdapter _multiplayerCallbacks = new();
        private readonly MatchAPICallbacksAdapter _matchAPICallbacks = new();

        // note: Adapater做两种数据格式差异、命名差异的转换适配

        internal class MultiplayerCallbacksAdapter : ByteCloudGameSdk.IMessageChannelCallbacks
        {
            public IMultiplayerListener Listener { get; private set; }

            public void SetListener(IMultiplayerListener value) => Listener = value;

            public void OnGameStart(string[] cloudRoomList)
            {
                Listener?.OnGameStart(cloudRoomList);
            }

            public void OnPlayerJoin(int roomIndex, ByteCloudGameSdk.JoinRoomParam param)
            {
                var joinRoomParam = new JoinRoomParam();
                joinRoomParam.AcceptParam(param);
                Listener?.OnPlayerJoin(roomIndex, joinRoomParam);
            }

            public void OnPlayerExit(int roomIndex, ByteCloudGameSdk.ExitRoomParam param)
            {
                Listener?.OnPlayerExit(roomIndex,
                    new ExitRoomParam((ExitRoomReason)param.Reason, param.RTCUserId));
            }

            public void OnQueryRoomInfo(int roomIndex, ByteCloudGameSdk.JoinRoomParam param)
            {
                var joinRoomParam = new JoinRoomParam();
                joinRoomParam.AcceptParam(param);
                Listener?.OnQueryRoomInfo(roomIndex, joinRoomParam);
            }

            public void OnPlayerOperate(int roomIndex, string opData)
            {
                Listener?.OnPlayerOperate(roomIndex, opData);
            }

            public void OnTexturePush(long shareHandle)
            {
                Listener?.OnTexturePush(shareHandle);
            }

            public void OnPlayerInput(ByteCloudGameSdk.InputEventResponse res)
            {
                Listener?.OnPlayerInput(new InputEventResponse(res.roomIndex, res.input));
            }

            public void OnCustomMessage(int roomIndex, string msg)
            {
                Listener?.OnCustomMessage(roomIndex, msg);
            }
        }

        // note: 目前 ThreadSafe 是 Manager 里管理，他能被System调用到Update，而且多平台统一
        internal class MatchAPICallbacksAdapter : ByteCloudGameSdk.IMatchServiceCallbacks
        {
            public IMatchAPIListener Listener { get; private set; }

            public void SetListener(IMatchAPIListener value) => Listener = value;

            public void OnPodCustomMessage(ByteCloudGameSdk.PodMessage msg)
            {
                Listener?.OnPodCustomMessage(new ApiPodMessageData().Accept(msg));
            }

            public void OnCommandMessage(ByteCloudGameSdk.MatchCommandMessage msg)
            {
                Listener?.OnCommandMessage(new ApiMatchCommandMessage().Accept(msg));
            }
        }
    }
}