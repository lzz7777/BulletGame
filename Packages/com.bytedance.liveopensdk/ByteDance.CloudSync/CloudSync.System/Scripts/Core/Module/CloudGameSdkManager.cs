using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace ByteDance.CloudSync
{
    internal interface ICloudGameSdkManager
    {
        /// <summary>
        /// 初始化
        /// </summary>
        Task<InitCloudGameResult> InitializeSdk();

        /// <summary>
        /// 尝试退出。 注意若Sdk未成功Init，那么返回false表示失败
        /// </summary>
        /// <returns>是否成功</returns>
        bool TryQuit();
    }

    public interface ICloudSdkUIInfoProvider
    {
        string DlgText_JoinInitTimeout { get; }
        string DlgText_QueryRoomInfoFail { get; }
        string DlgText_RoomIndexError { get; }
        string DlgTitle_AnchorExit { get; }
        string DlgText_AnchorExit { get; }
    }

    public class DefaultCloudSdkUIInfo : ICloudSdkUIInfoProvider
    {
        public string DlgText_JoinInitTimeout => "对方主播网络异常，无法加入。\n请重新邀请\n(JoinInitTimeout {maxWait})";
        public string DlgText_QueryRoomInfoFail => "网络异常，获取房间信息失败\n(OnQueryRoomInfo {code})";
        public string DlgText_RoomIndexError => "网络异常，用户信息异常\n(RoomIndexError {index})";
        public string DlgTitle_AnchorExit => "游戏结束";
        public string DlgText_AnchorExit => "主播{anchor}退出";
    }

    /// <summary>
    /// SDK 初始化结果
    /// </summary>
    internal struct InitCloudGameResult
    {
        public bool IsSuccess() => State.IsSuccessOrAlready();
        public InitState State;
        public ICloudGameAPI.ErrorCode Code;
        public string Error;

        public string ToStr() => $"{Code} ({(int)Code}) {Error}";
    }

    /// <summary>
    /// 云游戏ByteCloudGameSdk，提供给Sdk回调，接收事件消息
    /// </summary>
    internal partial class CloudGameSdkManager : ICloudGameSdkManager, IMultiplayerListener
    {
        static CloudGameSdkManager()
        {
            Debug.IsMsgTagEnabled = true;
#if CLOUDGAME_ENABLE_STARKLOGS
            Debug.IsMsgTimeEnabled = false;
#else
            Debug.IsMsgTimeEnabled = true;
#endif
            Debug.IsMsgLevelEnabled = true;
        }

        internal CloudGameSdkManager()
        {
            Application.quitting += OnApplicationQuitting;
            DebugInfo.LogDebugVer();
            DebugInfo.LogDebugEnvs();
            DebugInfo.LogDebugCmdArgs();
            MessageHandler = new MessageHandler();
            UIInfoProvider = new DefaultCloudSdkUIInfo();
        }

        internal static readonly SdkDebugLogger Debug = new("CloudGame");
        private const string Tag = "SdkMgr";
        private const string LogTag = "[" + Tag + "] ";
        private const string LibGameSdkLogTag = "[gamesdk] ";

        private static SdkEnv SdkEnv => CloudSyncSdk.InternalEnv.SdkEnv;

        // ReSharper disable once MemberCanBePrivate.Global
        internal static readonly SdkDebugInfo DebugInfo = new SdkDebugInfo();

        internal static string SdkLibVersion => SdkEnv.IsEnvReady ? CloudGameSdk.API.FileVersion : string.Empty;

        /// feature gate
        private static bool SdkFeatureEnabled => true;

        internal MessageHandler MessageHandler { get; }

        public ICloudSdkUIInfoProvider UIInfoProvider { get; set; }

        /// <summary>
        /// 是否连屏主播完成初始进房（且包含得到了InitParam、游戏启动流程所需匹配信息）
        /// </summary>
        private bool HasInitAnchorsJoined { get; set; }

        /// <summary>
        /// 是否阻止进游戏 (初始进房失败超时)。
        /// 当初始进房失败时，变为true，不允许进游戏
        /// </summary>
        private bool IsInitPlayBlocked { get; set; }

        /// <summary>
        /// 连屏初始加房需要几人Join。 单实例 2 = 需要至少2个主播连屏
        /// </summary>
        private static int InitJoinRequiredCount => 2;

        /// <summary>
        /// 连屏主播初始加房的最大等待时间
        /// </summary>
        float InitJoinMaxWaitTime => Application.isEditor ? 10f : 30f;

        public bool TryQuit()
        {
            if (!_initResult.State.IsSuccessOrAlready())
            {
                CGLogger.LogWarning($"{LogTag}TryQuit not available, state: {_initResult.State}");
                return false;
            }

            CGLogger.Log($"{LogTag}TryQuit SendPodQuit");
            CloudGameSdk.API.SendPodQuit();
            return true;
        }

        private void OnApplicationQuitting()
        {
            Application.quitting -= OnApplicationQuitting;
            CGLogger.LogWarning($"{LogTag}OnApplicationQuitting app is quiting.");
        }

        private void SetSdkInitEnvReady()
        {
            if (SdkEnv.IsEnvReady)
                return;
            SdkEnv.SetReady();
        }

        private void NotifyProgress(string info)
        {
        }

        void SDKLog(string log)
        {
            CGLogger.LogDebug($"{LibGameSdkLogTag}{log}");
        }

        void SDKLogError(string log)
        {
            CGLogger.LogError($"{LibGameSdkLogTag}{log}");
        }

        /// <summary>
        /// Sdk回调：游戏开始.
        /// </summary>
        /// <param name="cloudRoomList">房间 id 列表，可根据数组长度判断当前房间数量</param>
        void IMultiplayerListener.OnGameStart(string[] cloudRoomList)
        {
            string roomStr = "";
            if (cloudRoomList != null)
                foreach (var roomId in cloudRoomList)
                    roomStr += roomId + " ";
            CGLogger.Log($"{LogTag}OnGameStart roomList = " + roomStr);
        }

        /// Sdk回调：发生操作指令.
        void IMultiplayerListener.OnPlayerOperate(int roomIndex, string opData)
        {
            if (CloudGameSdk.IsVerboseLogForInput)
                CGLogger.Log($"{LogTag}OnPlayerOperate room: {roomIndex} opData: {opData}");
            if (string.IsNullOrEmpty(opData))
            {
                CGLogger.Log($"{LogTag}OnPlayerOperate room: {roomIndex} opData is empty.");
                return;
            }

            PlayerOperate opDataJson = JsonConvert.DeserializeObject<PlayerOperate>(opData);
            if (opDataJson == null)
            {
                CGLogger.LogWarning($"{LogTag}OnPlayerOperate room: {roomIndex} json deserialize failed, returns null.");
                return;
            }

            var msg = MessagePools.PlayerOperateMessagePool.Get();
            msg.index = (SeatIndex)roomIndex;
            msg.operateData = opDataJson;
            EnqueueMessage(roomIndex, msg);
        }

        /// rtc 侧完成 texture 推流后的回调.
        void IMultiplayerListener.OnTexturePush(long shareHandle)
        {
            //    throw new System.NotImplementedException();
        }

        /// <summary>
        /// 输入法输入.
        /// </summary>
        /// <param name="res"></param>
        void IMultiplayerListener.OnPlayerInput(InputEventResponse res)
        {
        }

        /// <summary>
        /// 云游戏长连链路：接收 &lt;-- 来自端上.
        /// </summary>
        /// <param name="roomIndex"></param>
        /// <param name="msg"></param>
        void IMultiplayerListener.OnCustomMessage(int roomIndex, string msg)
        {
            // note: 例如如果 msg 里有 uid 那么应当避免掉。 open_id 则没问题。
            CGLogger.Log($"{LogTag}OnCustomMessage room: {roomIndex}, msg: {msg}");

            PlayerCustomMessage queueMsg = new PlayerCustomMessage()
            {
                index = (SeatIndex)roomIndex,
                message = msg
            };
            EnqueueMessage(roomIndex, queueMsg);
        }

        public void Update()
        {
            _messageQueue.Process();

            // todo: split Input actions (playerOperate) for UnityThreadListener, and process dequeue in EarlyUpdate.
            _multiplayerListenerProxy?.Update();
        }

        public void Dispose()
        {
        }
    }
}