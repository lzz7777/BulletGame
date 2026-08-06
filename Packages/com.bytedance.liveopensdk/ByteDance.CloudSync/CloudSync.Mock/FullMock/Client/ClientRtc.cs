using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ByteDance.CloudSync.Mock.Agent;
using Unity.WebRTC;
using UnityEngine;
using WebSocketSharp;

namespace ByteDance.CloudSync.Mock
{
    /// <summary>
    /// Mock的客户端Rtc，相当于模拟端上云游戏XPlay的Rtc流。连接到 Agent服务器后，最终与云游戏实例Pod的Rtc房间，进行Rtc会话。
    /// 流的主要操作： 1. 从Rtc拉流，流画面帧来源于实例Pod。 2. 将输入操作推到Rtc流，使实例Pod接收输入事件。
    /// </summary>
    /// <remarks>
    /// 在 <see cref="MockPlay"/> 启动时和切流时，会由 <see cref="MockSwitchableRtcStream"/> 创建 <see cref="ClientRtc"/> 对象。 <br/>
    /// Rtc链路关系，参考: <see cref="FullMock"/>
    /// </remarks>
    internal class ClientRtc : IClientRtcStream
    {
        public IInputEventSender InputEventSender { get; private set; }

        private RTCPeerConnection _peerConnection;
        private IMessageChannel _peerDataChannel;
        private IMessageChannel _agentDataChannel;
        private IClientRtcStream _localStream;
        private WebSocket _socket;
        private Texture _rtcVideoTexture;
        private bool _connected;
        /// 使用本地设备。 true: 不需要 Rtc 拉流，直接读取本地设备画面
        private bool _useLocalDevice;
        private readonly IMockLogger _logger = IMockLogger.GetLogger(nameof(ClientRtc));

        public ClientRtc()
        {
            InitPeer();
            InputEventSender = new NoneInputEventSender();
        }

        /// <summary>
        /// 客户端Rtc切流：连接到 Agent服务器（<see cref="Agent.AgentServer"/>） 的 Rtc客户端连接服务 <see cref="ClientRtcService"/>.<see cref="ClientRtcService.OnOpen"/>
        /// </summary>
        public async Task<bool> Connect(RtcConnectOptions options, string rtcUuid, bool isLocalDevice, int index = -1)
        {
            _useLocalDevice = isLocalDevice;
            var queryString = BuildQueryString(options, rtcUuid, index, isLocalDevice);
            // 连接到Agent服务器的Rtc客户端连接服务
            _socket = new WebSocket($"ws://{options.Host}:{options.Port}/client?{queryString}");
            _logger.Log($"connect {options.Host}:{options.Port}, {queryString}, isLocal: {_useLocalDevice}");
            _socket.OnOpen += SocketOnOpen;
            _socket.OnClose += SocketOnClose;
            _socket.ConnectAsync();

            while (_socket.ReadyState == WebSocketState.Connecting)
                await Task.Yield();
            _connected = _socket.ReadyState == WebSocketState.Open;
            _logger.Log($"connect result: {_connected}!");
            return _connected;
        }

        private string BuildQueryString(RtcConnectOptions options, string rtcUuid, int index, bool isLocalDevice)
        {
            var targetHostToken = options.PodToken;
            var settings = RtcMock.MockSettings;
            var myPodToken = settings.PodToken;
            var query = new NameValueCollection
            {
                { "target_host_token", targetHostToken },
                { "target_index", index.ToString() },
                { "rtc_uuid", rtcUuid },
                { "pod_token", myPodToken },
                { "local_device", isLocalDevice.ToString() },
            };

            // index=xxx&roomId=xxx ...
            var queryString = string.Join("&", query.AllKeys.Select(k => $"{k}={query[k]}"));
            return queryString;
        }

        private void SocketOnClose(object sender, CloseEventArgs e)
        {
            _logger.Log("SocketOnClose");
        }

        public async Task Init()
        {
            _logger.Log("Init connect");
            while (!_connected)
                await Task.Yield();

            _logger.Log("Init device");
            if (_localStream != null)
                await _localStream.Init();

            _logger.Log("Init Texture");
            while (GetVideoFrame() == null)
                await Task.Yield();
            _logger.Log("Init done");
        }

        /// <inheritdoc cref="IClientRtcStream.GetVideoFrame"/>
        public Texture GetVideoFrame() => _useLocalDevice ? GetLocalVideoFrame() : GetRtcVideoFrame();

        /// 读取本地设备画面
        private Texture GetLocalVideoFrame() => _localStream?.GetVideoFrame();

        /// 拉Rtc流的画面
        private Texture GetRtcVideoFrame() => _rtcVideoTexture;

        private void HandleAgentMessage(MessageWrapper message)
        {
            var id = message.id;
            if (id == MessageId.Offer)
            {
                var desc = message.To<DescObject>();
                OnReceiveOffer(desc.To(), CancellationToken.None);
            }
            else if (id == MessageId.Candidate)
            {
                var candidate = message.To<CandidateObject>();
                _peerConnection.AddIceCandidate(candidate.To());
            }
            else if (id == MessageId.JoinRoomNotify)
            {
                var joinRoomNotify = message.To<RtcJoinRoomNotify>();
                if (_useLocalDevice)
                {
                    _localStream = new MockLocalStream(joinRoomNotify.index);
                    InputEventSender = _localStream.InputEventSender;
                }
            }
        }

        private async void OnReceiveOffer(RTCSessionDescription desc, CancellationToken token)
        {
            _logger.Log("SetRemoteDesc start");
            var op1 = _peerConnection.SetRemoteDescription(ref desc);
            await op1.Wait(token);
            _logger.Log("SetRemoteDesc end");

            var answerDesc = await CreateAnswer(token);

            _logger.Log("SetLocalDesc start");
            var op2 = _peerConnection.SetLocalDescription(ref answerDesc);
            await op2.Wait(token);
            _logger.Log("SetLocalDesc end");

            _agentDataChannel.Send(MessageId.Answer, JsonUtility.ToJson(DescObject.From(answerDesc)));
        }

        private async Task<RTCSessionDescription> CreateAnswer(CancellationToken token)
        {
            var answer = _peerConnection.CreateAnswer();
            await answer.Wait(token);
            _logger.Log($"CreateAnswer {answer.Desc.sdp}");
            return answer.Desc;
        }

        private void InitPeer()
        {
            var configuration = MockUtils.GetSelectedSdpSemantics();
            _peerConnection = new RTCPeerConnection(ref configuration);
            _peerConnection.OnTrack += OnTrack;
            _peerConnection.OnIceCandidate += OnIceCandidate;
            _peerConnection.OnDataChannel += OnDataChannel;
            RtcUpdater.EnsureUpdate();
        }

        private void OnDataChannel(RTCDataChannel channel)
        {
            _peerDataChannel = new RtcMessageChannel(channel);
            if (_useLocalDevice == false)
                InputEventSender = new RtcInputEventSender(_peerDataChannel);
        }

        private void OnIceCandidate(RTCIceCandidate candidate)
        {
            // _logger.Log($"OnIceCandidate {candidate.Candidate}");
            _agentDataChannel.Send(MessageId.Candidate, JsonUtility.ToJson(CandidateObject.From(candidate)));
        }

        private void OnTrack(RTCTrackEvent e)
        {
            if (e.Track is VideoStreamTrack videoStreamTrack)
            {
                _rtcVideoTexture = videoStreamTrack.Texture;
                videoStreamTrack.OnVideoReceived += renderer =>
                {
                    _rtcVideoTexture = renderer;
                };
            }
        }

        private void SocketOnOpen(object sender, EventArgs e)
        {
            _agentDataChannel = new WebSocketMessageChannel(_socket);
            _agentDataChannel.OnMessageReceive += HandleAgentMessage;
            _connected = true;
            var api = (IRtcMockCloudGameAPIEx)CloudGameSdk.API;
            Debug.Assert(api != null, "Assert RtcMock api != null");
            api.InitChannel(PodInstance.AgentDataChannel, _agentDataChannel);
        }

        public void Dispose()
        {
            _socket?.Close();
            _peerConnection?.Close();
            _peerConnection?.Dispose();
            _peerConnection = null;
            _localStream?.Dispose();
        }
    }

    /// <summary>
    /// Mock环境的Rtc输入事件发送器
    /// </summary>
    internal class RtcInputEventSender : IInputEventSender
    {
        private readonly IMessageChannel _peerDataChannel;

        public RtcInputEventSender(IMessageChannel peerDataChannel)
        {
            _peerDataChannel = peerDataChannel;
        }

        void IInputEventSender.SendMouseEvent(MouseButtonId button, MouseAction actionType, Vector2 point, Vector2Int screenSize, double wheel)
        {
            var data = new RtcMouseData
            {
                action = actionType,
                button = button,
                point = point,
                screenSize = screenSize,
                wheel = wheel
            };
            _peerDataChannel.Send(MessageId.MouseInput, JsonUtility.ToJson(data));
        }

        public void SendTouchesEvent(Touch[] touches, Vector2Int screenSize)
        {
            var data = RtcTouchData.Create(touches, screenSize);
            var json = JsonUtility.ToJson(data);
            Debug.Assert(json != null && json.Length > 2, "assert touches json len > 2");
            _peerDataChannel.Send(MessageId.TouchInput, json);
        }

        void IInputEventSender.SendKeyboardEvent(KeyboardAction actionType, KeyCode keyCode)
        {
            var data = new RtcKeyboardData
            {
                action = actionType,
                keyCode = keyCode
            };
            _peerDataChannel.Send(MessageId.KeyboardInput, JsonUtility.ToJson(data));
        }
    }

    /// <summary>
    /// Dummy的输入事件发送器，实际不发送事件。
    /// </summary>
    internal class NoneInputEventSender : IInputEventSender
    {
        public void SendMouseEvent(MouseButtonId button, MouseAction actionType, Vector2 point, Vector2Int screenSize, double wheel)
        {
        }

        public void SendTouchesEvent(Touch[] touches, Vector2Int screenSize)
        {
        }

        public void SendKeyboardEvent(KeyboardAction actionType, KeyCode keyCode)
        {
        }
    }
}