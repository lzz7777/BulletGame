using System.Text;
using System.Threading.Tasks;
using Unity.WebRTC;
using UnityEngine;
using WebSocketSharp;

namespace ByteDance.CloudSync.Mock
{
    internal delegate void ChannelMessageHandler(MessageWrapper message);
    
    /// <summary>
    /// 消息发送接收通道
    /// </summary>
    internal interface IMessageChannel
    {
        event ChannelMessageHandler OnMessageReceive;
        
        void Send(int id, string data) { Send(MessageWrapper.CreateNotify(id, data)); }
        
        void Send<T>(int id, T message) { Send(MessageWrapper.CreateNotify(id, JsonUtility.ToJson(message))); }
        
        void Send(MessageWrapper message);

        void Close() { }
    }

    internal abstract class MessageChannel
    {
        public event ChannelMessageHandler OnMessageReceive;
        public MessageDelayer Delayer { get; set; }
        
        protected async void HandleMessage(MessageWrapper message)
        {
            if (Delayer != null)
                await Delayer.Delay(message.id);
            OnMessageReceive?.Invoke(message);
        }
    }

    internal class MessageDelayer
    {
        public int DelayMs { get; set; }

        /// param: int messageId,
        /// param: bool isDelay
        public System.Func<int, bool> MsgDelayChecker { get; set; }

        private bool IsDelay(int id) => DelayMs > 0 && (MsgDelayChecker == null || MsgDelayChecker.Invoke(id));

        public Task Delay(int id)
        {
            if (IsDelay(id))
                return Delay();
            return Task.CompletedTask;
        }

        public Task Delay() => Task.Delay(DelayMs);
    }

    /// <summary>
    /// WebSocket 客户端消息通道
    /// </summary>
    internal class WebSocketMessageChannel : MessageChannel, IMessageChannel
    {
        private readonly WebSocket _socket;

        public WebSocketMessageChannel(WebSocket socket)
        {
            _socket = socket;
            _socket.OnMessage += OnSocketMessage;
        }

        private void OnSocketMessage(object sender, MessageEventArgs e)
        {
            var w = JsonUtility.FromJson<MessageWrapper>(e.Data);
            Loom.Run(() => HandleMessage(w));
        }

        public async void Send(MessageWrapper message)
        {
            if (Delayer != null)
                await Delayer.Delay(message.id);
            _socket.Send(JsonUtility.ToJson(message));
        }
    }

    /// <summary>
    /// 封装 RTCDataChannel 消息通道
    /// </summary>
    internal class RtcMessageChannel : MessageChannel, IMessageChannel
    {
        private readonly RTCDataChannel _dataChannel;
        private readonly IMockLogger _logger = IMockLogger.GetLogger(nameof(RtcMessageChannel));
        private bool _closed;

        public RtcMessageChannel(RTCDataChannel channel)
        {
            _dataChannel = channel;
            _dataChannel.OnOpen += () => _logger.Log("RTCDataChannel open");
            _dataChannel.OnClose += OnClose;
            _dataChannel.OnError += e => _logger.Log($"RTCDataChannel error: {e}");
            _dataChannel.OnMessage += OnDataChannelMessage;
        }

        private void OnClose()
        {
            _closed = true;
            _logger.Log("RTCDataChannel close");
        }

        private void OnDataChannelMessage(byte[] bytes)
        {
            var data = Encoding.UTF8.GetString(bytes);
            var w = JsonUtility.FromJson<MessageWrapper>(data);
            Loom.Run(() => HandleMessage(w));
        }
        
        public void Send(MessageWrapper message)
        {
            _closed = _dataChannel.ReadyState == RTCDataChannelState.Closed;
            if (_closed)
            {
                // _logger.LogError("Data closed.");
                return;
            }
            _dataChannel.Send(JsonUtility.ToJson(message));
        }
    }

    internal class RtcSessionMessageChannel : IMessageChannel
    {
        private readonly IMessageChannel _inner;
        private readonly string _sessionId;

        public RtcSessionMessageChannel(IMessageChannel inner, string sessionId)
        {
            _inner = inner;
            _inner.OnMessageReceive += InnerOnOnMessageReceive;
            _sessionId = sessionId;
        }

        private void InnerOnOnMessageReceive(MessageWrapper message)
        {
            if (message.sessionId == _sessionId)
            {
                OnMessageReceive?.Invoke(message);
            }
        }

        public event ChannelMessageHandler OnMessageReceive;

        public void Send(MessageWrapper message)
        {
            message.sessionId = _sessionId;
            _inner.Send(message);
        }

        public void Close()
        {
            _inner.OnMessageReceive -= InnerOnOnMessageReceive;
        }
    }
}