using System;
using Google.Protobuf;
using MatchPb;
using StarkNetwork;

namespace ByteDance.CloudSync.Match
{
    public class MessageManager: IMessageManager
    {
        private NetworkClient _client;
        private static DebugLogger Debug { get; } = new();

        public event Action<long, MsgID, IMessage> OnMessage; 

        public MessageManager(NetworkClient client)
        {
            Debug.Log("MessageManager ctor");
            _client = client;
            _client.OnPayloadReceived += OnPayloadReceived;
            _client.SetPayloadHandlerEnable(true);
        }

        private long _requestId = 1;

        private long GenerateRequestId()
        {
            return _requestId++;
        }
        
        private long SendMessage(MsgID msgType, IMessage msg, ulong targetModule = 0, ulong targetId = 0)
        {
            if (_client.State != ClientState.RUNNING)
            {
                // todo: cache message
                return 0;
            }
            
            var package = new MatchMsgPackage
            {
                RequestId = GenerateRequestId(),
                MsgId = msgType,
                MsgData = msg.ToByteString()
            };
            var bytes = package.ToByteArray();
            _client.SendPayload(bytes, targetModule, targetId);
            Debug.Log($"Send message: [RequestId]={package.RequestId}, [MsgId]={package.MsgId}, [bytes.Length]={bytes.Length}, [targetModule]={targetModule}, [targetId]={targetId}");
            Debug.Log($">>>[MsgData]={msg}");
            return package.RequestId;
        }

        private void OnPayloadReceived(byte[] bytes)
        {
            Debug.Log($"OnPayloadReceived: [bytes.Length]={bytes.Length}");
            try
            {
                var package = MatchMsgPackage.Parser.ParseFrom(bytes);
                Debug.Log($"Package parse success: [RequestId]={package.RequestId}, [MsgId]={package.MsgId}, [LogId]={package.LogId}");
                IMessage msg = package.MsgId switch
                {
                    MsgID.NoneMessage => null,
                    MsgID.StartMatchReq => StartMatchReq.Parser.ParseFrom(package.MsgData),
                    MsgID.StartMatchResp => StartMatchResp.Parser.ParseFrom(package.MsgData),
                    MsgID.CancelMatchReq => CancelMatchReq.Parser.ParseFrom(package.MsgData),
                    MsgID.CancelMatchResp => CancelMatchResp.Parser.ParseFrom(package.MsgData),
                    MsgID.MatchErrorNty => MatchErrorNty.Parser.ParseFrom(package.MsgData),
                    MsgID.MatchResultNty => MatchResultNty.Parser.ParseFrom(package.MsgData),
                    MsgID.HeatbeatReq => HeartbeatReq.Parser.ParseFrom(package.MsgData),
                    MsgID.HeatbeatResp => HeartbeatResp.Parser.ParseFrom(package.MsgData),
                    MsgID.GetWebCastInfoReq => GetWebCastInfoReq.Parser.ParseFrom(package.MsgData),
                    MsgID.GetWebCastInfoResp => GetWebCastInfoResp.Parser.ParseFrom(package.MsgData),
                    MsgID.InitStarkMatchReq => InitStarkMatchReq.Parser.ParseFrom(package.MsgData),
                    MsgID.InitStarkMatchResp => InitStarkMatchResp.Parser.ParseFrom(package.MsgData),
                    _ => null
                };

                Debug.Log($"<<<[MsgData]={msg}");
                OnMessage?.Invoke(package.RequestId, package.MsgId, msg);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        public long GetWebCastInfo()
        {
            var getWebCastInfoReq = new GetWebCastInfoReq();
            return SendMessage(MsgID.GetWebCastInfoReq, getWebCastInfoReq);
        }
        
        public long InitStartMatch(MatchInfo matchInfo)
        {
            var initStartMatchReq = new InitStarkMatchReq
            {
                MatchInfo = matchInfo
            };
            return SendMessage(MsgID.InitStarkMatchReq, initStartMatchReq);
        }

        public long StartMatch(MatchInfo matchInfo, string matchParamJson, string extraInfo, ulong targetModule = 0, ulong targetId = 0)
        {
            var startMatchReq = new StartMatchReq
            {
                MatchInfo = matchInfo,
                MatchJsonParams = matchParamJson,
                DeviceId = 0,
                AppId = 1128,
                ExtraInfo = extraInfo,
            };
            return SendMessage(MsgID.StartMatchReq, startMatchReq, targetModule, targetId);
        }

        public long CancelMatch(MatchInfo matchInfo, ulong targetModule = 0, ulong targetId = 0)
        {
            var cancelMatchReq = new CancelMatchReq()
            {
                MatchInfo = matchInfo,
            };
            return SendMessage(MsgID.CancelMatchReq, cancelMatchReq, targetModule, targetId);
        }

        public void HeartBeat(ulong targetModule = 0, ulong targetId = 0)
        {
            if (_client.State != ClientState.RUNNING)
            {
                return;
            }
            SendMessage(MsgID.HeatbeatReq, new HeartbeatReq(), targetModule, targetId);
        }

        public void Dispose()
        {
            _client.OnPayloadReceived -= OnPayloadReceived;
            // _client.SetPayloadHandlerEnable(false);
            OnMessage = null;
        }
    }
}