using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ByteDance.CloudSync
{
    internal class SimpleMockCloudGameAPI : ICloudGameAPI, IMultiAnchorPlayerInfoProvider
    {
        public static SimpleMockCloudGameAPI Instance { get; } = new();

        private IMultiplayerListener _listener;
        private readonly Dictionary<SeatIndex, AnchorPlayerInfo> _playerInfos = new();

        public void OnJoin(SeatIndex roomIndex, AnchorPlayerInfo anchorPlayerInfo)
        {
            var client = CloudSyncSdk.InternalCurrent.ClientManager.GetClient(roomIndex);
            if (client?.State == ClientState.Connected)
            {
                Debug.LogError($"MockJoin roomIndex: {roomIndex} already connected");
                return;
            }

            var rtcUserId = Guid.NewGuid().ToString();
            _listener.OnPlayerJoin(roomIndex.ToInt(), new JoinRoomParam
            {
                RTCUserId = rtcUserId,
                Code = ICloudGameAPI.ErrorCode.Success
            });

            if (anchorPlayerInfo != null)
                _playerInfos[roomIndex] = anchorPlayerInfo;
        }

        public void OnExit(SeatIndex roomIndex)
        {
            var client = CloudSyncSdk.InternalCurrent.ClientManager.GetClient(roomIndex);
            if (client == null)
                return;
            _listener.OnPlayerExit(roomIndex.ToInt(), new ExitRoomParam
            {
                RTCUserId = client.RtcUserId
            });
        }

        public void MockOperate(SeatIndex index)
        {
            PlayerOperate opData = new PlayerOperate
            {
                op_type = OperateType.MOUSE,
                event_data = null
            };
            CloudMouseData mouseData = new CloudMouseData
            {
                x = 0,
                y = 0,
                action = MouseAction.UP,
                axis_v = 0,
                axis_h = 0,
                button = MouseButtonId.LEFT,
                state = MousePositionState.ABSOLUTE
            };
            opData.event_data = JToken.FromObject(mouseData);
            var opDataStr = JsonUtil.ToJson(opData);
            _listener.OnPlayerOperate(index.ToInt(), opDataStr);
        }

        string ICloudGameAPI.FileVersion => "1.0.0";
        void ICloudGameAPI.SetMultiplayerListener(IMultiplayerListener listener)
        {
            _listener = listener;
        }

        Task<ICloudGameAPI.Response> ICloudGameAPI.Init()
        {
            return Task.FromResult(new ICloudGameAPI.Response(0, ""));
        }

        Task<ICloudGameAPI.Response> ICloudGameAPI.InitMultiplayer()
        {
            MockJoinHostClient();
            return Task.FromResult(new ICloudGameAPI.Response(0, ""));
        }

        private async void MockJoinHostClient()
        {
            try
            {
                await Task.Delay(1);
                OnJoin(0, null);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        void ICloudGameAPI.SetLogFunction(Action<string> sdkLog, Action<string> sdkLogError)
        {
        }

        Task<ICloudGameAPI.Response> ICloudGameAPI.SendOpenServiceCustomMessage(SeatIndex roomIndex, string msg)
        {
            throw new NotImplementedException();
        }

        ICloudGameAPI.ErrorCode ICloudGameAPI.SendVideoFrame(SeatIndex roomIndex, long textureId)
        {
            return ICloudGameAPI.ErrorCode.Success;
        }

        void ICloudGameAPI.SendPodQuit()
        {
            throw new NotImplementedException();
        }

        void ICloudGameAPI.SetAudioEnabled(SeatIndex roomIndex, bool enabled)
        {
            throw new NotImplementedException();
        }

        public void InitMatchAPI(IMatchAPIListener listener)
        {
        }

        Task<ApiMatchStreamResponse> ICloudGameMatchAPI.SendMatchBegin(ApiMatchParams matchParam)
        {
            throw new NotImplementedException();
        }

        Task<ApiMatchStreamResponse[]> ICloudGameMatchAPI.SendMatchEnd()
        {
            throw new NotImplementedException();
        }

        Task<ApiMatchStreamResponse> ICloudGameMatchAPI.SendMatchEnd(int roomIndex)
        {
            throw new NotImplementedException();
        }

        public Task<ICloudGameAPI.Response> SendPodCustomMessage(string token, ApiPodMessageData msgData)
        {
            throw new NotImplementedException();
        }

        public Task<AnchorPlayerInfo> FetchOnJoinPlayerInfo(ICloudSeat seat, CancellationToken token)
        {
            return Task.FromResult(_playerInfos[seat.Index]);
        }
    }
}