using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.WebRTC;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ByteDance.CloudSync.Mock
{
    interface IPodRtcRoom
    {
        /// <summary>
        /// 开始Rtc流
        /// </summary>
        public void StartRtcStream(bool isLocalDevice);

        string RTCUserId { get; }
        SeatIndex Index { get; }
        void Dispose();
    }

    /// <summary>
    /// Pod上的Rtc房间会话。 <br/>
    /// 当Pod实例 <see cref="PodInstance"/> 上收到用户进房时，会创建Rtc房间会话 <see cref="PodRtcRoom"/>。 <br/>
    /// 会通过Pod实例 <see cref="PodInstance.Connect"/> 连接的 Agent服务器 通道，进而与端上 <see cref="ClientRtc"/> Rtc通信。 <br/>
    /// 会引用 VirtualScreenSystem, IVirtualScreen 并通过 RTC 推流； <br/>
    /// 会接收从Rtc流来的输入消息。
    /// </summary>
    internal class PodRtcRoom : IPodRtcRoom, IDisposable
    {
        private readonly SeatIndex _index;
        private readonly IMessageChannel _agentDataChannel;
        private readonly CancellationTokenSource _cancellation = new();
        private readonly MockInputEventSender _eventSender;
        private IMessageChannel _rtcDataChannel;
        private VideoStreamTrack _screenTrack;
        private RTCPeerConnection _peerConnection;
        private IVirtualScreen _screen;
        private RenderTexture _tempRt;
        private readonly IMockLogger _logger = IMockLogger.GetLogger(nameof(PodRtcRoom));

        public string RTCUserId { get; }

        public SeatIndex Index => _index;

        public string OpenId => RTCUserId;

        public PodRtcRoom(SeatIndex index, string id, IMessageChannel agentChannel)
        {
            RTCUserId = id;
            _index = index;
            _agentDataChannel = agentChannel;
            _agentDataChannel.OnMessageReceive += HandleAgentMessage;
            _eventSender = new MockInputEventSender(index);
        }

        /// <summary>
        /// 开始Rtc流
        /// </summary>
        /// <inheritdoc cref="IPodRtcRoom.StartRtcStream"/>
        public void StartRtcStream(bool isLocalDevice)
        {
            InitPeer();
            if (isLocalDevice == false)
                DoStream(_cancellation.Token);
        }

        private void HandleAgentMessage(MessageWrapper message)
        {
            var id = message.id;
            if (id == MessageId.Answer)
            {
                var desc = message.To<DescObject>();
                HandleAnswer(desc.To(), _cancellation.Token);
            }
            else if (id == MessageId.Candidate)
            {
                var candidate = message.To<CandidateObject>();
                // _logger.Log($"GetCandidate: {candidate.candidate}");
                _peerConnection.AddIceCandidate(candidate.To());
            }
        }

        /// <summary>
        /// 推流
        /// </summary>
        private async void DoStream(CancellationToken token)
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                    return;

                var screen = VirtualScreenSystem.Find(_index);
                var texture = screen?.RenderTexture;
                if (texture == null)
                {
                    await Task.Yield();
                }
                else
                {
                    _screen = screen;
                    AddTrack(texture);
                    break;
                }
            }
        }

        /// <summary>
        /// 推流发送画面帧
        /// </summary>
        private void AddTrack(Texture texture)
        {
            _logger.Log("Add track!");
            var format = WebRTC.GetSupportedGraphicsFormat(SystemInfo.graphicsDeviceType);
            // 确保 Texture 是受支持的格式
            if (texture.graphicsFormat == format)
            {
                _screenTrack = new VideoStreamTrack(texture, CopyTextureHelper.VerticalFlipCopy);
            }
            else
            {
                _tempRt = new RenderTexture(texture.width, texture.height, 0, format);
                _screenTrack = new VideoStreamTrack(_tempRt, VerticalFlipCopy);
            }
            _peerConnection.AddTrack(_screenTrack);
        }

        private void VerticalFlipCopy(Texture source, RenderTexture dest)
        {
            var src = _screen.RenderTexture;
            CopyTextureHelper.VerticalFlipCopy(src, dest);
        }

        private void InitPeer()
        {
            var configuration = MockUtils.GetSelectedSdpSemantics();
            _peerConnection = new RTCPeerConnection(ref configuration);
            _peerConnection.OnNegotiationNeeded += OnNegotiationNeeded;
            _peerConnection.OnIceCandidate += OnIceCandidate;
            var dataChannel = _peerConnection.CreateDataChannel("data");
            _rtcDataChannel = new RtcMessageChannel(dataChannel);
            _rtcDataChannel.OnMessageReceive += HandleClientMessage;
            RtcUpdater.EnsureUpdate();
        }

        private void HandleClientMessage(MessageWrapper wrapper)
        {
            HandleClientInput(wrapper);
        }

        /// <summary>
        /// 处理端上 RtcClient 的输入消息
        /// </summary>
        /// <param name="w"></param>
        private void HandleClientInput(MessageWrapper w)
        {
            switch (w.id)
            {
                case MessageId.MouseInput:
                {
                    var input = JsonUtility.FromJson<RtcMouseData>(w.data);
                    _eventSender.SendMouseEvent(input.button, input.action, input.point, input.screenSize, input.wheel);
                    break;
                }
                case MessageId.TouchInput:
                {
                    var data = JsonUtility.FromJson<RtcTouchData>(w.data);
                    _eventSender.SendTouchesEvent(data.ToUnityTouches(), data.screenSize);
                    break;
                }
                case MessageId.KeyboardInput:
                {
                    var input = JsonUtility.FromJson<RtcKeyboardData>(w.data);
                    _eventSender.SendKeyboardEvent(input.action, input.keyCode);
                    break;
                }
            }
        }

        private void OnIceCandidate(RTCIceCandidate candidate)
        {
            // _logger.Log($"OnIceCandidate {candidate.Candidate}");
            _agentDataChannel.Send(MessageId.Candidate, JsonUtility.ToJson(CandidateObject.From(candidate)));
        }

        private async Task<RTCSessionDescription> CreateOffer(CancellationToken token)
        {
            var offer = _peerConnection.CreateOffer();
            await offer.Wait(token);
            _logger.Log($"offer: {offer.Desc.sdp}");
            return offer.Desc;
        }

        private async void SendOffer(CancellationToken token)
        {
            var offer = await CreateOffer(token);

            _logger.Log("SetLocalDesc start");
            var op1 = _peerConnection.SetLocalDescription(ref offer);
            await op1.Wait(token);
            _logger.Log("SetLocalDesc end");

            _agentDataChannel.Send(MessageId.Offer, JsonUtility.ToJson(DescObject.From(offer)));
        }

        private async void HandleAnswer(RTCSessionDescription answer, CancellationToken token)
        {
            _logger.Log("SetRemoteDesc start");
            var op2 = _peerConnection.SetRemoteDescription(ref answer);
            await op2.Wait(token);
            _logger.Log("SetRemoteDesc end");
        }

        private void OnNegotiationNeeded()
        {
            SendOffer(_cancellation.Token);
        }

        public void Dispose()
        {
            _cancellation.Cancel();
            _screenTrack?.Dispose();
            _peerConnection.Close();
            _peerConnection.Dispose();
            _agentDataChannel.Close();

            Object.DestroyImmediate(_tempRt);
        }

        void IPodRtcRoom.Dispose() => Dispose();

        void IDisposable.Dispose() => Dispose();
    }
}