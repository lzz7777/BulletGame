using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using ByteDance.CloudSync.Mock;
using ByteDance.CloudSync.Mock.Agent;
using WebSocketSharp;

namespace ByteDance.CloudSync.Mock
{
    /// <summary>
    /// Mock的云游戏实例（云端Pod）。 <br/>
    /// 与Agent服务器（<see cref="Agent.AgentServer"/>） 的 Rtc房间服务连接，接收进房、退房事件。 <br/>
    /// 有用户进房时，创建Rtc房间会话 <see cref="PodRtcRoom"/>，与 <see cref="ClientRtc"/> 端上通信。
    /// </summary>
    /// <remarks>
    /// Rtc链路关系，参考: <see cref="FullMock"/>
    /// </remarks>
    internal class PodInstance
    {
        private static readonly PodInstance _instance = new();

        /// <summary>
        /// 模拟启动云游戏实例（云端Pod），将连接至 AgentServer 注册为房间
        /// </summary>
        public static async Task<bool> Start()
        {
            return await _instance.Connect(new RtcConnectOptions
            {
                RoomId = RtcMock.MockSettings.RoomId,
                PodToken = RtcMock.MockSettings.PodToken
            });
        }

        public static IMessageChannel AgentDataChannel => _instance._agentDataChannel;

        private RtcConnectOptions RtcOptions { get; set; }
        private WebSocket _socket;
        private IMessageChannel _agentDataChannel;
        private readonly List<IPodRtcRoom> _roomSessions = new();
        private readonly IMockLogger _logger = IMockLogger.GetLogger("RtcGamePod");

        /// <summary>
        /// Pod连接Rtc服务：连接到 Agent服务器（<see cref="Agent.AgentServer"/>） 的 Rtc房间服务 <see cref="PodRtcRoomService"/>.<see cref="PodRtcRoomService.OnOpen"/>，准备接收进房、退房事件。
        /// </summary>
        private async Task<bool> Connect(RtcConnectOptions options)
        {
            RtcOptions = options;
            var queryString = BuildQueryString();
            // 连接到Agent服务器的Rtc房间服务
            _socket = new WebSocket($"ws://{options.Host}:{options.Port}/pod_room?{queryString}");
            _logger.Log($"connect {options.Host}:{options.Port}, {queryString}");
            _socket.OnClose += OnClose;
            _socket.ConnectAsync();
            _agentDataChannel = new WebSocketMessageChannel(_socket)
            {
                Delayer = AgentServer.GetMessageDelayer()
            };
            _agentDataChannel.OnMessageReceive += HandleAgentMessage;

            while (_socket.ReadyState == WebSocketState.Connecting)
                await Task.Yield();
            var connected = _socket.ReadyState == WebSocketState.Open;
            _logger.Log($"connect result: {connected}!");
            if (connected)
                RtcMock.OnPodInstanceReady();
            return connected;
        }

        private string BuildQueryString()
        {
            var roomId = RtcMock.MockSettings.RoomId;
            var podToken = RtcMock.MockSettings.PodToken;
            var query = new NameValueCollection
            {
                { "pod_room_id", roomId },
                { "pod_token", podToken },
            };
            var queryString = string.Join("&", query.AllKeys.Select(k => $"{k}={query[k]}"));
            return queryString;
        }

        private void OnClose(object sender, CloseEventArgs e)
        {
            Loom.Run(RtcMock.OnAgentClosed);
        }

        private void HandleAgentMessage(MessageWrapper message)
        {
            if (message.id == MessageId.JoinRoom)
            {
                var joinRoomMessage = message.To<RtcJoinRoom>();
                _logger.Log($"handle JoinRoom index: {joinRoomMessage.index}, rtcUserId: {joinRoomMessage.rtcUserId}");
                var sessionChannel = new RtcSessionMessageChannel(_agentDataChannel, joinRoomMessage.rtcUserId);
                var roomSession = new PodRtcRoom(joinRoomMessage.index, joinRoomMessage.rtcUserId, sessionChannel);
                roomSession.StartRtcStream(joinRoomMessage.isLocalDevice);
                _roomSessions.Add(roomSession);
                RtcMock.CloudGameAPI.OnJoin(roomSession);
            }
            else if (message.id == MessageId.ExitRoom)
            {
                var leaveRoomMessage = message.To<RtcExitRoom>();
                var roomSession = _roomSessions.Find(c => c.RTCUserId == leaveRoomMessage.rtcUserId);
                _logger.Log($"handle ExitRoom index: {roomSession?.Index}, rtcUserId: {leaveRoomMessage.rtcUserId}");
                if (roomSession != null)
                {
                    _roomSessions.Remove(roomSession);
                    RtcMock.CloudGameAPI.OnExit(roomSession);
                    roomSession.Dispose();
                }
            }
        }
    }
}