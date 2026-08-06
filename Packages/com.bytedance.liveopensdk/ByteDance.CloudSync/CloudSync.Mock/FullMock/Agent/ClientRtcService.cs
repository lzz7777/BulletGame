using System.Linq;
using ByteDance.CloudSync.Mock;
using UnityEngine;
using WebSocketSharp;

namespace ByteDance.CloudSync.Mock.Agent
{
    internal interface IRtcClientService : IMessageChannel
    {
        SeatIndex Index { get; }

        /// <summary>
        /// 对应 RtcUserId, 同一个端的 RtcUserId 固定
        /// </summary>
        string RtcUserId { get; }

        /// <summary>
        /// 对应 HostToken (云游戏实例PodToken)
        /// </summary>
        string PodToken { get; }
    }

    /// <summary>
    /// 模拟面向客户端 <see cref="ClientRtc"/> 的Rtc服务
    /// </summary>
    internal class ClientRtcService : ServiceBehavior, IRtcClientService
    {
        private readonly AgentServer _server;
        private string _rtcUuid;
        private string _podToken;
        private IRtcRoomService _room;
        private readonly IMockLogger _logger = IMockLogger.GetLogger(nameof(ClientRtcService));

        public SeatIndex Index { get; private set; }

        public string RtcUserId => _rtcUuid;

        public string PodToken => _podToken;

        public ClientRtcService(AgentServer server)
        {
            _server = server;
        }

        /// <summary>
        /// 客户端Rtc连接来了。 连接自客户端Rtc <see cref="ClientRtc"/> 的 <see cref="ClientRtc.Connect"/>
        /// </summary>
        protected override void OnOpen()
        {
            _logger.Log($"OnOpen, queryString: {Context.QueryString}");
            var targetHostToken = Context.QueryString.Get("target_host_token");
            var indexString = Context.QueryString.Get("target_index");
            var rtcUuid = Context.QueryString.Get("rtc_uuid");
            var myPodToken = Context.QueryString.Get("pod_token");
            var isLocalDevice = Context.QueryString.Get("local_device") == true.ToString();
            Loom.Run(() =>
            {
                _rtcUuid = rtcUuid;
                _podToken = myPodToken;
                var index = (SeatIndex)(int.TryParse(indexString, out var i) ? i : -1);
                _logger.Log($"On ws connected, index = {(int)index}, target hostToken = {targetHostToken}, from hostToken = {myPodToken}, rtcUuid = {rtcUuid}, ws id = {ID}");
                Debug.Assert(_server != null, "Assert _server != null");
                _room = _server.GetRoomByHostToken(targetHostToken);
                Debug.Assert(_room != null, "Assert _room != null");
                if (_room == null)
                {
                    var msg = $"错误：请求连接不存在的rtc房间! rtc room not found! hostToken: {targetHostToken}";
                    var rooms = _server.GetRooms();
                    Debug.LogError($"{msg}. Should be inside of list: [\n" +
                                   $"{string.Join(",\n", rooms.Select(s => $"{{ roomId: {s.RtcRoomId}, hostToken: {s.PodToken} }}"))}" +
                                   "\n]");
                    throw new System.Exception(msg);
                }

                Index = _room.JoinRoom(this, index, isLocalDevice);

                var notify = new RtcJoinRoomNotify { index = Index };
                var message = MessageWrapper.CreateNotify(MessageId.JoinRoomNotify, JsonUtility.ToJson(notify));
                Send(message);
            });
        }

        protected override void OnClose(CloseEventArgs e)
        {
            _logger.Log($"On ws closed, index = {(int)Index}, from hostToken = {_podToken}, rtcUuid = {_rtcUuid}, ws id = {ID}");
            _room.ExitRoom(this);
        }
    }
}