using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using ByteDance.CloudSync.Mock.Agent;
using UnityEngine;
using UnityEngine.UI;

namespace ByteDance.CloudSync.Mock
{
    /// <summary>
    /// Mock玩法窗口。 模拟了端上（直播伴侣客户端）的云游戏XPlay直播玩法窗口。 <br/>
    /// 启动时可修改Rtc房间设置，然后会启动各个Mock模块：Agent服务器、Rtc客户端、云游戏实例Pod。 <br/>
    /// 在Mock运行、连接了Agent服务器后，负责：1. Mock模拟拉流，显示画面到本地窗口中。 2. 将本地输入操作，发送到Mock模拟的云游戏实例Pod。
    /// </summary>
    /// <remarks>
    /// Rtc链路关系，参考: <see cref="FullMock"/>
    /// </remarks>
    [DllMonoBehaviour]
    public partial class MockPlay : MonoBehaviour
    {
        [SerializeField] private RawImage screen;

        [SerializeField] private GameObject panel;

        [SerializeField] private Text textError;
        [SerializeField] private Toggle startAsServer;
        [SerializeField] private Text toggleServerDesc;
        [SerializeField] private InputField inputHost;
        [SerializeField] private InputField inputPort;
        [SerializeField] private Slider inputNetDelay;
        [SerializeField] private Text textNetDelay;
        [SerializeField] private Toggle toggleSimTouchByMouse;

        // InputLiveRoomId
        [SerializeField] private InputField inputRoomId;
        [SerializeField] private InputField inputOpenId;
        [SerializeField] private InputField inputNickname;

        [SerializeField] private Button buttonConnect;

        [SerializeField] private MockAlertPanel alertPanel;
        [SerializeField] private Button buttonShowCtrl;
        [SerializeField] private MockCtrlPanel ctrlPanel;

        [SerializeField] private bool simulateTouchByMouse;
        [SerializeField] private bool simulateTouchStationary;

        public bool SimulateTouchByMouse
        {
            get => simulateTouchByMouse;
            set => simulateTouchByMouse = value;
        }

        private int NetDelayValue { get; set; }
        private MessageDelayer Delayer { get; set; }

        private readonly MockSwitchableRtcStream _rtcStream = new();

        private bool IsConnected { get; set; }
        private string RtcUuid => RtcMockSettings.RtcUserId;
        private RtcMockSettings RtcMockSettings => RtcMock.MockSettings;

        private static MockPlay _instance;
        public static MockPlay Instance => _instance;

        private RtcMockCloudGameAPI MockAPI => RtcMock.CloudGameAPI;

        public static void Setup()
        {
            if (_instance)
                return;

            var prefab = Resources.Load<GameObject>("MockPlayer");
            var go = Instantiate(prefab);
            DontDestroyOnLoad(go);
            go.hideFlags |= HideFlags.DontSave;
            go.name = nameof(MockPlay);
            _instance = go.GetComponent<MockPlay>();
        }

        private void Awake()
        {
            textError.text = null;
            inputPort.text = "19001"; // 代码控制 mock rtc 默认端口。
            buttonShowCtrl.onClick.AddListener(() => ctrlPanel.Show());
            ctrlPanel.Hide();
            alertPanel.Hide();
            SetCtrlArrowVisible(false);
        }

        private void Start()
        {
            OnInputPortUpdate();
            inputPort.onValueChanged.AddListener((v) => OnInputPortUpdate());
            RtcMockSettings.IsInitialized = false;
            var intRoomId = MockUtils.RandomIntId();
            var roomId = MockUtils.MockRoomId(intRoomId);
            var userId = MockUtils.MockUserId(intRoomId);
            var playerInfo = MockUtils.MockPlayerInfo(intRoomId);
            playerInfo.liveRoomId = roomId;
            RtcMockSettings.RtcUserId = userId;
            RtcMockSettings.PlayerInfo = playerInfo;
            RtcMockSettings.RoomId = roomId;
            inputRoomId.text = roomId;
            NetDelayValue = AgentServer.DefaultDelayMS;
            inputNetDelay.minValue = 1;
            inputNetDelay.maxValue = 2000;
            inputNetDelay.value = NetDelayValue;
            inputNetDelay.onValueChanged.AddListener(_ => UpdateNetDelay());
            UpdateNetDelay();
            buttonConnect.onClick.AddListener(OnClickConnect);
            startAsServer.onValueChanged.AddListener(OnServerModeToggle);
            toggleSimTouchByMouse.onValueChanged.AddListener(v => simulateTouchByMouse = v);
            toggleSimTouchByMouse.isOn = simulateTouchByMouse;
            inputOpenId.text = playerInfo.openId;
            inputNickname.text = playerInfo.nickName;
            var serverConfig = AgentServer.Config();
            startAsServer.isOn = serverConfig.IsHost;
            UpdateServerModeDisplay();
        }

        private void OnInputPortUpdate()
        {
            int port;
            try
            {
                port = int.Parse(inputPort.text);
            }
            catch (Exception e)
            {
                Debug.Log($"input port error: {e}");
                return;
            }

            AgentServer.Port = port;
        }

        private void OnServerModeToggle(bool v)
        {
            AgentServer.Config();
            UpdateServerModeDisplay();
        }

        private void UpdateServerModeDisplay()
        {
            inputHost.gameObject.SetActive(!startAsServer.isOn);
            UpdateToggleServerDesc();
        }

        private void UpdateToggleServerDesc()
        {
            var serverConfig = AgentServer.GetCurrentConfig();
            if (toggleServerDesc == null || serverConfig == null)
            {
                toggleServerDesc.text = "";
                return;
            }

            var detectedConfig = AgentServer.GetDetectedConfig();
            if (!serverConfig.IsHost && detectedConfig != null && detectedConfig.started)
                toggleServerDesc.text = startAsServer.isOn ? $"<color=yellow>注意：本地Mock服务已存在 Port: {detectedConfig.port}</color>" : "";
            else
                toggleServerDesc.text = "";
        }

        /// <summary>
        /// 切流，到本地模拟的 RTC 房间
        /// </summary>
        private async Task<bool> SwitchToLocal()
        {
            int index = 0;
            // 目标为本地
            RtcConnectOptions options = new RtcConnectOptions
            {
                RoomId = RtcMockSettings.RoomId,
                PodToken = RtcMockSettings.PodToken
            };
            var success = await _rtcStream.SwitchRtc(options, index, RtcUuid, true);
            screen.texture = _rtcStream.GetVideoFrame();
            return success;
        }

        /// <summary>
        /// 切流，到目标房主的Rtc房间
        /// </summary>
        /// <param name="hostToken">目标房间房主的 HostToken</param>
        /// <param name="index">目标座位 index</param>
        /// <returns></returns>
        public async Task<bool> SwitchByToken(string hostToken, int index)
        {
            // 目标为另一个实例，服务器会通过HostToken找到他
            RtcConnectOptions options = new RtcConnectOptions
            {
                RoomId = null,
                PodToken = hostToken
            };
            var success = await _rtcStream.SwitchRtc(options, index, RtcUuid, false);
            screen.texture = _rtcStream.GetVideoFrame();
            return success;
        }

        public async void Pop()
        {
            await _rtcStream.Pop();
            screen.texture = _rtcStream.GetVideoFrame();
        }

        private void SetCtrlArrowVisible(bool visible)
        {
            buttonShowCtrl.gameObject.SetActive(visible);
        }

        public void MockDisconnect()
        {
            SetCtrlArrowVisible(false);
            _rtcStream.Clean();
            alertPanel.Show("信息", "已断开连接", AlertButtons.None);
        }

        public void OnDisconnected()
        {
            ShowErrorMessage("与Mock服务断开链接");
            panel.SetActive(true);
            buttonConnect.interactable = true;
            _rtcStream.Clean();
        }

        private void ShowErrorMessage(string message)
        {
            CleanErrorMessage();
            textError.text = message;
        }

        private void CleanErrorMessage()
        {
            textError.text = null;
        }

        private void OnClickConnect()
        {
            ConfirmSettings();
            Connect();
        }

        private void ConfirmSettings()
        {
            CleanErrorMessage();
            buttonConnect.interactable = false;

            RtcMockSettings.PlayerInfo.nickName = inputNickname.text;
            RtcMockSettings.PlayerInfo.openId = inputOpenId.text;
            RtcMockSettings.RoomId = inputRoomId.text;
            AgentServer.Port = int.Parse(inputPort.text);
            AgentServer.Host = inputHost.text;
            AgentServer.NetDelayMs = NetDelayValue;
            MockAPI.NetDelayMs = NetDelayValue;
            Delayer = AgentServer.GetMessageDelayer();
            CloudSyncSdk.Env.SetCloudGameToken(RtcMockSettings.HostToken);
            CloudSyncSdk.Env.SetMockWebcastAuth(true);
            if (string.IsNullOrEmpty(CloudSyncSdk.Env.GetMockLaunchToken()))
                CloudSyncSdk.Env.SetMockLaunchToken(Guid.NewGuid().ToString());
            RtcMockSettings.IsInitialized = true;
        }

        private async void Connect()
        {
            try
            {
                var startServer = startAsServer.isOn;
                IsConnected = await Launch(startServer);

                if (!IsConnected)
                {
                    buttonConnect.interactable = true;
                    ShowErrorMessage("连接失败");
                    return;
                }

                panel.SetActive(false);
                SetCtrlArrowVisible(true);
            }
            catch (SocketException)
            {
                buttonConnect.interactable = true;
                ShowErrorMessage("连接失败");
            }
            catch (Exception e)
            {
                var message = $"{e.GetType().Name}: {e.Message}";
                Debug.LogException(e);
                panel.SetActive(true);
                buttonConnect.interactable = true;
                ShowErrorMessage(message);
            }
        }

        private async Task<bool> Launch(bool startAgentServer)
        {
            if (startAgentServer)
                AgentServer.StartServer();

            var ok = await PodInstance.Start();
            if (ok)
                ok = await SwitchToLocal();
            return ok;
        }

        /// <summary>
        /// 模拟端上拉流
        /// </summary>
        private void UpdateTexture()
        {
            screen.texture = _rtcStream.GetVideoFrame();
        }

        private void Update()
        {
            if (!IsConnected || _rtcStream == null)
                return;
            UpdateTexture();
            SyncInputEvents();
        }

        private void OnDestroy()
        {
            IsConnected = false;
            _rtcStream.Dispose();
        }

        private void OnApplicationQuit()
        {
            Destroy(gameObject);
            _instance = null;
        }

        private void UpdateNetDelay()
        {
            var value = inputNetDelay.value;
            NetDelayValue = (int)Math.Round(value);
            textNetDelay.text = $"{value}";
        }
    }
}