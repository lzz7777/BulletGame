using System;
using System.Linq;
using System.Threading.Tasks;
using ByteDance.Live.Foundation.Logging;
using ByteDance.LiveOpenSdk.Push;
using ByteDance.LiveOpenSdk.Runtime;
using ByteDance.LiveOpenSdk.Runtime.Utilities;
using cfg.Global;
using cfg.Net;
using Cysharp.Threading.Tasks;
using Douyin.LiveOpenSDK.Samples;
using Sirenix.OdinInspector;
using UnityEngine;

namespace XN
{
    public class DySdkManager : MonoSingleton<DySdkManager>
    {
        private static ConstConfigCategory ConstConfig => TotalConfigManager.ConfigManager.ConstConfigCategory;
        /// <summary>
        /// AppID
        /// </summary>
        private string AppID => TotalConfigManager.ConfigManager.LoginInfoConfigCategory.GetOrDefault(ConstConfig.CurrChannel).AppID;

        /// <summary>
        /// 调试用token
        /// </summary>
        public string Token;
        public static SampleMessagePushManager SampleMessagePushManager { get; private set; }

        
        [SerializeField] [LabelText("是否调试SDK")]
        public bool DebugDySDK;
        [SerializeField] [LabelText("Editor调试下手动抖音/快手token")]
        public string TestToken;
        public bool IsInitialized { get; private set; }
        
        protected override void OnInit()
        {
            Init();
        }

        public async void Init()
        {
            await UniTask.WaitUntil(() => XN.YooAssetManager.Instance.IsInitialized);
            await UniTask.WaitUntil(() => TotalConfigManager.Instance.IsLoadOver);
            
            SdkUnityLogger.MinSeverity = Severity.Verbose;
            Log("config loadOver ... PushMessageInit...");

            SampleMessagePushManager ??= gameObject.AddComponent<SampleMessagePushManager>();

            //1.0
            Log($"开始LiveOpenSdk初始化 AppId: {AppID}");
            SampleLiveOpenSdkManager.Initialize(AppID);

#if UNITY_EDITOR
            if (string.IsNullOrEmpty(Token))
            {
                if (DebugDySDK && !string.IsNullOrEmpty(TestToken))
                {
                    Token = TestToken;
                    SampleMessagePushManager.UploadLog(new[]{"Token","Unity测试"} ,$"AppId: {AppID} | Token： {SampleLiveOpenSdkManager.Token}");
                }
                else
                {
                    LogError("调试token未设置,无法开启命令直推直接推");
                    IsInitialized = true;   // TODO Editor 模式下的编辑好了
                    return;
                }
            }

            SampleLiveOpenSdkManager.Token = Token;
#else
            CommandLine.Init();
            if (CommandLine.TryGetArg(CommandKey.DyToken, out var token)) {
                SampleLiveOpenSdkManager.Token = token;
            }
            else {
                LogError("游戏token获取失败,无法开启礼物直接推");
            }

            SampleMessagePushManager.UploadLog(new[]{"Token","LiveOpenSdk初始化"} ,$"AppId: {AppID} | Token {SampleLiveOpenSdkManager.Token}");
            
#endif
            // 1.1 指令直推
            StartDirectPushMode();
            
            if (string.IsNullOrEmpty(SampleLiveOpenSdkManager.Token))
            {
                LogWarning("警告：SDK 未能从命令行获得 token，请从直播伴侣启动 exe 或手动提供 token");
            }
            
            IsInitialized = true;
        }

        // 指令直推模式
        public async void StartDirectPushMode()
        {
            if (string.IsNullOrEmpty(SampleLiveOpenSdkManager.Token))
            {
                LogWarning("警告：SDK 未能从命令行获得 token，指令直推不可用");
                return;
            }

            Log("开始：指令直推模式");

            // 初始化指令直推链路。
            try
            {
                await SampleMessagePushManager.Init();
            }
            catch (Exception e)
            {
                LogError($"由于超过重试次数或 SDK 被销毁导致不再能自动获取房间信息, e: {e}");
                return;
            }

            // 开启想要接收的消息类型的推送任务，表示对局开始。
            // lixin : 因为只是进入游戏，没那么快进行接受推送。后续其他地方会引用。同封装函数：SampleMessagePushManager.StartPush()
            
            // 若收到消息，会打印日志。
            Log("结束：指令直推模式");
        }

        protected override void OnRemove()
        {
        }

        #region Log

        

        private const string tag = "<color=#0000FF>[LiveOpenSdk]</color> ";

        public static void LogWarning(object obj)
        {
            Debug.LogWarning($"{tag}{obj}");
        }

        public static void LogError(object obj)
        {
            Debug.LogError($"{tag}{obj}");
        }

        public static void Log(object obj)
        {
            Debug.Log($"{tag}{obj}");
        }
        #endregion

        public static bool IsCloudGame()
        {
            return LiveOpenSdk.CloudGameApi.IsCloudGame();
        }
    }
}
