using System;
using System.Threading.Tasks;
using ByteCloudGameSdk;
using ByteDance.CloudSync.Mock;

namespace ByteDance.CloudSync.Mock
{
    // note: RtcMock 上额外的API，需要扩展出的Mock类能统一使用。
    internal interface IRtcMockCloudGameAPIEx
    {
        void InitChannel(IMessageChannel agentDataChannel, IMessageChannel messageChannel);
    }

    internal class RtcMockCloudGameAPI : ICloudGameAPI, ICloudGameAPIEx, IRtcMockCloudGameAPIEx
    {
        private IMultiplayerListener _listener;
        private IMatchAPIListener _matchAPIListener;
        private IMessageChannel _agentChannel;
        private IMessageChannel _clientChannel;
        private readonly IMockLogger _logger = IMockLogger.GetLogger(nameof(RtcMockCloudGameAPI));
        public int NetDelayMs { get; set; } = 100;

        private Task NetDelay()
        {
            return Task.Delay(NetDelayMs);
        }

        public void OnJoin(IPodRtcRoom session)
        {
            _listener.OnPlayerJoin(session.Index.ToInt(), new JoinRoomParam
            {
                RTCUserId = session.RTCUserId,
                Code = ICloudGameAPI.ErrorCode.Success
            });
        }

        public void OnExit(IPodRtcRoom session)
        {
            _listener.OnPlayerExit(session.Index.ToInt(), new ExitRoomParam
            {
                RTCUserId = session.RTCUserId
            });
        }

        string ICloudGameAPI.FileVersion => "1.2024.0521.1";

        void ICloudGameAPI.SetMultiplayerListener(IMultiplayerListener listener)
        {
            _listener = listener;
        }

        async Task<ICloudGameAPI.Response> ICloudGameAPI.Init()
        {
            await NetDelay();
            return new ICloudGameAPI.Response(ICloudGameAPI.ErrorCode.Success, "");
        }

        async Task<ICloudGameAPI.Response> ICloudGameAPI.InitMultiplayer()
        {
            await NetDelay();
            return new ICloudGameAPI.Response(ICloudGameAPI.ErrorCode.Success, "");
        }

        public void InitChannel(IMessageChannel agentChannel, IMessageChannel channel)
        {
            _agentChannel = agentChannel;
            _clientChannel = channel;
            _clientChannel.OnMessageReceive += OnClientMessageReceive;
        }

        private void OnClientMessageReceive(MessageWrapper message)
        {
            if (message.id == MessageId.PodMessageNotify)
            {
                var notify = message.To<RtcPodMessageNotify>();
                _logger.Log($"RtcPodMessageNotify: {notify.extraInfo}");
                _matchAPIListener.OnPodCustomMessage(new ApiPodMessageData
                {
                    from = null,
                    message = notify.extraInfo
                });
            }
            else if (message.id == MessageId.EndGameNotify)
            {
                var notify = message.To<EndGameNotify>();
                _logger.Log($"EndGameNotify: {notify}");
                _matchAPIListener.OnCommandMessage(new ApiMatchCommandMessage
                {
                    command = MatchCommand.ReportSwitchBack,
                    code = MatchErrorCode.Success,
                });
                MockPlay.Instance.Pop();
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
            _matchAPIListener = listener;
        }

        async Task<ApiMatchStreamResponse> ICloudGameMatchAPI.SendMatchBegin(ApiMatchParams matchParam)
        {
            _logger.Log($"send match begin param: {matchParam.ToStr()}");
            // 在 RtcMock 下，hostToken 代表房间 id
            await MockPlay.Instance.SwitchByToken(matchParam.hostToken, matchParam.roomIndex);

            return new ApiMatchStreamResponse
            {
                code = MatchErrorCode.Success,
                message = null,
                roomIndex = matchParam.roomIndex,
            };
        }

        async Task<ApiMatchStreamResponse[]> ICloudGameMatchAPI.SendMatchEnd()
        {
            _logger.Log("send match end");
            var req = new EndGameReq { index = SeatIndex.Invalid };
            _agentChannel.Send(MessageId.EndGameReq, req);

            // todo: should await for response
            await NetDelay();
            // todo: give results list according to current connected users, instead of empty
            return Array.Empty<ApiMatchStreamResponse>();
        }

        public async Task<ApiMatchStreamResponse> SendMatchEnd(int roomIndex)
        {
            _logger.Log($"send match end: {roomIndex}");
            var req = new EndGameReq { index = (SeatIndex)roomIndex };
            _agentChannel.Send(MessageId.EndGameReq, req);

            // todo: should await for response
            await NetDelay();
            var response = new ApiMatchStreamResponse
            {
                code = MatchErrorCode.Success,
                message = null,
                roomIndex = roomIndex,
            };
            return response;
        }

        public async Task<ICloudGameAPI.Response> SendPodCustomMessage(string token, ApiPodMessageData msgData)
        {
            _logger.Log($"send pod message: {msgData.message}");
            var req = new RtcPodMessageReq { targetToken = token, extraInfo = msgData.message };
            _agentChannel.Send(MessageId.PodMessageReq, req);
            // todo: should await for response
            await NetDelay();
            return new ICloudGameAPI.Response(ICloudGameAPI.ErrorCode.Success, "");
        }


        public ISdkEnv SdkEnv { get; set; }
        public IMultiplayerListener MultiplayerListener => _listener;
    }
}