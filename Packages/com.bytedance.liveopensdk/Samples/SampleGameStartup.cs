using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ByteDance.Live.Foundation.Logging;
using ByteDance.LiveOpenSdk.DebugUtils;
using ByteDance.LiveOpenSdk.Push;
using ByteDance.LiveOpenSdk.Runtime.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Douyin.LiveOpenSDK.Samples
{
    /// <summary>
    /// <para>
    /// 负责控制示例场景 SampleGameScene 的行为。
    /// </para>
    /// <para>
    /// 使用方法：点击“指令直推模式”或“抖音云模式”二者之一，然后观察日志窗口。
    /// 两种模式互斥，无法同时生效。
    /// </para>
    /// <para>
    /// 需要配置参数的类：
    /// <list type="bullet">
    /// <item><see cref="SampleLiveOpenSdkManager"/></item>
    /// <item><see cref="SampleDyCloudManager"/></item>
    /// </list>
    /// </para>
    /// </summary>
    public class SampleGameStartup : MonoBehaviour
    {
        public Button Button_Init;
        public Button Button_Mode1;
        public Button Button_Mode1_1;
        public Button Button_Mode1_RoundStart;
        public Button Button_Mode1_RoundEnd;
        public Button Button_Mode1_UploadLog;
        public Button Button_Mode2;
        public InputField AppIdInputField;
        public Text LogText;

        private readonly LogConsole _logConsole = new LogConsole();
        private LogWriter Log { get; } = new LogWriter(SdkUnityLogger.LogSink, "SampleGameStartup");

        private void Awake()
        {
            // 把 SDK 的日志输出放到场景上
            _logConsole.Text = LogText;
            SdkUnityLogger.OnRichLog -= _logConsole.WriteLog;
            SdkUnityLogger.OnRichLog += _logConsole.WriteLog;
        }

        private void Start()
        {
            InitEvents();

            // 可设置日志等级，调试时可使用较详细的日志
            SdkUnityLogger.MinSeverity = Severity.Verbose;
        }

        private void OnDestroy()
        {
            // 销毁直播开放 SDK
            SampleLiveOpenSdkManager.Uninitialize();
        }

        private void InitEvents()
        {
            Button_Init.onClick.AddListener(Init);
            Button_Mode1.onClick.AddListener(StartDirectPushMode);
            Button_Mode2.onClick.AddListener(StartDyCloudMode);
            Button_Mode1_1.onClick.AddListener(TestLinkmic);
            Button_Mode1_RoundStart.onClick.AddListener(StartRound);
            Button_Mode1_RoundEnd.onClick.AddListener(EndRound);
            Button_Mode1_UploadLog.onClick.AddListener(UploadLog);
        }

        private void Init()
        {
            // 配置相关参数，请修改为实际的值
            // 使用抖音云需设置
            SampleDyCloudManager.EnvId = "your envId";
            SampleDyCloudManager.ServiceId = "your serviceId";
            // 任何玩法需设置
            string appId = AppIdInputField.text;
            if (string.IsNullOrEmpty(appId))
            {
                Log.Info($"请输入AppId！");
                return;
            }
            // 初始化直播开放 SDK
            Log.Info($"开始LiveOpenSdk初始化 AppId: {appId}");
            SampleLiveOpenSdkManager.Initialize(appId);

            if (string.IsNullOrEmpty(SampleLiveOpenSdkManager.Token))
            {
                Log.Warning("警告：SDK 未能从命令行获得 token，请从直播伴侣启动 exe 或手动提供 token");
            }
        }
        // 指令直推模式
        private async void StartDirectPushMode()
        {
            if (string.IsNullOrEmpty(SampleLiveOpenSdkManager.Token))
            {
                Log.Warning("警告：SDK 未能从命令行获得 token，指令直推不可用");
            }

            Log.Info("开始：指令直推模式");

            // 初始化指令直推链路。
            try
            {
                await SampleMessagePushManager.Init();
            }
            catch (Exception e)
            {
                Log.Error($"由于超过重试次数或 SDK 被销毁导致不再能自动获取房间信息, e: {e}");
                return;
            }

            // 开启想要接收的消息类型的推送任务，表示对局开始。
            var msgTypes = new[] { PushMessageTypes.LiveComment, PushMessageTypes.LiveGift, PushMessageTypes.LiveLike, PushMessageTypes.LiveTeam };
            await Task.WhenAll(msgTypes.Select(SampleMessagePushManager.StartPush));

            // 若收到消息，会打印日志。
            Log.Info("结束：指令直推模式");
        }
        // 指令直推-Linkmic
        private async void TestLinkmic()
        {
            Log.Info("获取麦序信息");
            var linkInfo = await SampleMessagePushManager.QueryLinkmicInfo();
            if (linkInfo != null)
            {
                Log.Info($"UpdateLinkmic FreeCount: {linkInfo?.BaseInfo.FreeCount}\nTotalCount: {linkInfo?.BaseInfo.TotalCount}\nLinkerId: {linkInfo?.BaseInfo.LinkerId}");
                for (int i = 0; i < linkInfo.UserList.Length; i++)
                {
                    var userInfo = linkInfo.UserList[i];
                    Log.Info("OpenId: " + userInfo.OpenId + "\n"
                             + "CameraState: " + userInfo.CameraState + "\n"
                             + "DisableCamera: " + userInfo.DisableCamera + "\n"
                             + "NickName: " + userInfo.NickName + "\n"
                             + "MicrophoneState: " + userInfo.MicrophoneState + "\n"
                             + "DisableMicrophone: " + userInfo.DisableMicrophone + "\n"
                             + "LinkPosition: " + userInfo.LinkPosition + "\n"
                             + "HostAppStartAppAvailable: " + userInfo.AppInfo.HostAppStartAppAvailable + "\n"
                             + "LinkState: " + userInfo.LinkState + "\n"
                             + "AvatarUrl: " + userInfo.AvatarUrl + "\n");
                }
                if (linkInfo.UserList.Length > 1)
                {
                    var openId = linkInfo.UserList[linkInfo.UserList.Length - 1].OpenId;
                    Log.Info("有人在麦上，邀请最后一个位置观众同玩");
                    await SampleMessagePushManager.InviteAudienceJoinGame(openId);
                    Log.Info("等待30s，查看观众是否启动了玩法");
                    await Task.Delay(30000); // 等待 30 秒
                    Log.Info("关闭观众的玩法");
                    await SampleMessagePushManager.RequestAudienceLeaveGame(openId);
                }
            }
            else
            {
                Log.Warning("QueryLinkmicInfo return null");
            }
        }

        private async void StartRound()
        {
            var result = await SampleRoundManager.Instance.StartRoundAsync();
        }

        private async void EndRound()
        {
            var result = await SampleRoundManager.Instance.EndRoundAsync();
        }

        private static int traceId;
        public static async void UploadLog()
        {
            SampleDebugUtilsManager.UploadLogWithTags(UploadLogLevel.Debug,
                new string[]{
                    "SampleGameStartup",
                    "ClickBtn"},
                $"上报日志，当前时间：{DateTime.Now},",
                    (traceId++).ToString());
        }


        // 抖音云模式
        private async void StartDyCloudMode()
        {
            Log.Info("开始：抖音云模式");

            // 初始化抖音云。
            await SampleDyCloudManager.Init();

            // 短连接能力演示。
            await SampleDyCloudManager.StartTasks();

            // 长连接能力演示。若收到消息，会打印日志。
            await SampleDyCloudManager.ConnectWebSocket();

            Log.Info("结束：抖音云模式");
        }
    }

    internal class LogConsole
    {
        private const int MaxCount = 15;
        private readonly Queue<string> _messageQueue = new Queue<string>();

        public Text Text { get; set; }

        public Severity MinSeverity = Severity.Info;

        public void WriteLog(Severity severity, string richText)
        {
            if (!severity.IsAtLeast(MinSeverity)) return;

            while (_messageQueue.Count >= MaxCount)
            {
                _messageQueue.Dequeue();
            }

            _messageQueue.Enqueue(richText);

            if (Text == null) return;

            Text.text = string.Join("\n", _messageQueue);
        }
    }
}