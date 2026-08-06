using System.Linq;
using ByteDance.CloudSync.Mock;
using UnityEngine;
using WebSocketSharp;

namespace ByteDance.CloudSync.Mock.Agent
{
    internal interface IRtcRoomService : IMessageChannel
    {
        string RtcRoomId { get; }
        string PodToken { get; }

        IRtcClientService FindByUserId(string id);

        IRtcClientService FindByToken(string token);

        SeatIndex JoinRoom(IRtcClientService client, SeatIndex index, bool isLocalDevice);

        void ExitRoom(IRtcClientService client);
    }

    /// <summary>
    /// 模拟面向云端Pod <see cref="PodInstance"/> 的Rtc房间服务<br/>
    /// 职责：<br/>
    /// 1. 房间内的 Client 管理，并发送给 <see cref="PodInstance"/> 进房、退房事件 <br/>
    /// 2. 中转 <see cref="PodInstance"/> 与 <see cref="ClientRtc"/> 间的消息
    /// </summary>
    internal class PodRtcRoomService : ServiceBehavior, IRtcRoomService
    {
        private readonly AgentServer _server;
        private readonly IRtcClientService[] _clientsArray = new IRtcClientService[10];
        private readonly IMockLogger _logger = IMockLogger.GetLogger(nameof(PodRtcRoomService));

        /// Pod的Rtc房间Id
        public string RtcRoomId { get; private set; }

        /// Pod的HostToken (云游戏实例PodToken)
        public string PodToken { get; private set; }

        public PodRtcRoomService(AgentServer server)
        {
            _server = server;
            OnMessageReceive += HandleRoomMessage;
        }

        protected override void OnClose(CloseEventArgs e)
        {
        }

        /// <summary>
        /// Pod连接来了。 连接自Pod云游戏实例 <see cref="PodInstance"/> 的 <see cref="PodInstance.Connect"/>。
        /// </summary>
        protected override void OnOpen()
        {
            var roomId = Context.QueryString.Get("pod_room_id");
            var podToken = Context.QueryString.Get("pod_token");
            RtcRoomId = roomId;
            PodToken = podToken;
            _logger.Log($"CloudPod connected, roomId: {roomId}, pod token: {podToken}");
            RegisterPodRoom(roomId, podToken);
        }

        private void RegisterPodRoom(string roomId, string podToken)
        {
            _server.RegisterPodRoom(roomId, this);
        }

        private SeatIndex FindSeat()
        {
            for (var i = 0; i < _clientsArray.Length; i++)
            {
                if (_clientsArray[i] == null)
                    return (SeatIndex)i;
            }

            return SeatIndex.Invalid;
        }

        /// <summary>
        /// Rtc进房，并告知 <see cref="PodInstance"/>
        /// </summary>
        /// <inheritdoc cref="IRtcRoomService.JoinRoom"/>
        public SeatIndex JoinRoom(IRtcClientService client, SeatIndex index, bool isLocalDevice)
        {
            var idx = index >= 0 ? index : FindSeat();
            if (!idx.IsValid())
                return SeatIndex.Invalid;

            var exist = _clientsArray[idx.ToInt()];
            if (exist != null)
                ExitRoom(exist);

            _logger.Log($"CloudPod JoinRoom: {idx}");

            _clientsArray[idx.ToInt()] = client;
            client.OnMessageReceive += w => HandleClientMessage(client, w);

            // send join room
            var joinMessage = new RtcJoinRoom
            {
                rtcUserId = client.RtcUserId,
                index = idx,
                isLocalDevice = isLocalDevice
            };
            var wrapper = MessageWrapper.CreateNotify(MessageId.JoinRoom, JsonUtility.ToJson(joinMessage));
            wrapper.flags |= MessageFlags.Agent;
            Send(wrapper);

            return idx;
        }

        private void HandleClientMessage(IRtcClientService client, MessageWrapper message)
        {
            // if ((message.flags & MessageFlags.Room) != 0)
            {
                // send to room
                message.sessionId = client.RtcUserId;
                Send(message);
            }
        }

        private void HandleRoomMessage(MessageWrapper message)
        {
            if (message.id == MessageId.MatchReq)
            {
                var req = message.To<MatchReq>();
                var target = FindByUserId(req.rtcUserId);
                if (target == null)
                {
                    _logger.LogError($"Client not found: rtc = {req.rtcUserId}");
                    return;
                }

                _server.MatchServer.MatchReq(target, this, req);
            }
            else if (message.id == MessageId.CancelMatchReq)
            {
                var req = message.To<CancelMatchReq>();
                var target = FindByUserId(req.rtcUserId);
                if (target == null)
                {
                    _logger.LogError($"Client not found: rtc = {req.rtcUserId}");
                    return;
                }

                _server.MatchServer.CancelMatch(this, req);
            }
            else if (message.id == MessageId.EndGameReq)
            {
                var req = message.To<EndGameReq>();
                if (req.index <= 0)
                {
                    foreach (var client in _clientsArray)
                    {
                        if (client != null && client.Index != 0)
                        {
                            KickForEndMatchGame(client.Index);
                        }
                    }
                }
                else
                {
                    KickForEndMatchGame(req.index);
                }
            }
            else if (message.id == MessageId.PodMessageReq)
            {
                var req = message.To<RtcPodMessageReq>();
                var target = FindByToken(req.targetToken);
                if (target == null)
                {
                    _logger.LogError($"Client not found: token = {req.targetToken}");
                    return;
                }

                var notify = new RtcPodMessageNotify { extraInfo = req.extraInfo };
                _logger.Log($"OnPodMessageNotify: {req.extraInfo}");
                target.Send(MessageId.PodMessageNotify, notify);
            }
            else
            {
                SendToClient(message.sessionId, message);
            }
        }

        private void SendToClient(string id, MessageWrapper message)
        {
            var target = FindByUserId(id);
            if (target == null)
            {
                _logger.LogError($"Client not found: id = {id}");
                return;
            }
            target.Send(message);
        }

        public IRtcClientService FindByUserId(string id) => _clientsArray.FirstOrDefault(c => c != null && c.RtcUserId == id);

        public IRtcClientService FindByToken(string token) => _clientsArray.FirstOrDefault(c => c != null && c.PodToken == token);

        private void KickForEndMatchGame(SeatIndex index)
        {
            if (index <= 0)
            {
                _logger.LogError($"Host client cant exit. index: {index}");
                return;
            }

            var target = _clientsArray[index.ToInt()];
            if (target == null)
            {
                _logger.LogError($"Client not found: index = {index}");
                return;
            }

            _logger.Log($"OnEndGameReq: {index}");
            _server.MatchServer.EndGameReq(target, this);
            ExitRoom(target);
        }

        /// <summary>
        /// 告知 <see cref="PodInstance"/> 进房事件
        /// </summary>
        /// <inheritdoc cref="IRtcRoomService.ExitRoom"/>
        public void ExitRoom(IRtcClientService client)
        {
            var idx = client.Index;
            if (_clientsArray[idx.ToInt()] != client)
                return;
            _clientsArray[idx.ToInt()] = null;
            _logger.Log($"CloudPod ExitRoom: {client.Index}");

            // send exit room
            var leaveMessage = new RtcExitRoom { rtcUserId = client.RtcUserId };
            var wrapper = MessageWrapper.CreateNotify(MessageId.ExitRoom, JsonUtility.ToJson(leaveMessage));
            wrapper.flags |= MessageFlags.Agent;
            Send(wrapper);

            _server.MatchServer.OnClientExit(client, this);
        }
    }
}