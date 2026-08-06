// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ByteDance.CloudSync;
using ByteDance.Live.Foundation.Logging;
using ByteDance.LiveOpenSdk;
using ByteDance.LiveOpenSdk.AudienceLinkmic;
using ByteDance.LiveOpenSdk.DebugUtils;
using ByteDance.LiveOpenSdk.Push;
using ByteDance.LiveOpenSdk.Report;
using ByteDance.LiveOpenSdk.Room;
using ByteDance.LiveOpenSdk.Runtime;
using ByteDance.LiveOpenSdk.Utilities;
using Cysharp.Threading.Tasks;
using Douyin.LiveOpenSDK.Samples;
using InfoStruct;
using Sirenix.OdinInspector;
using UnityEngine;

namespace XN
{
    /// <summary>
    /// 直播开放 SDK 指令直推能力的接入示例代码。
    /// </summary>
    public class SampleMessagePushManager : MonoBehaviour
    {
        public ILiveOpenSdk Sdk => LiveOpenSdk.Instance;
        public IRoomInfoService RoomInfoService => SampleLiveOpenSdkManager.Sdk.GetRoomInfoService();
        private IAudienceLinkmicService AudienceLinkmicService => SampleLiveOpenSdkManager.Sdk.GetAudienceLinkmicService();
        private IMessagePushService MessagePushService => SampleLiveOpenSdkManager.Sdk.GetMessagePushService();
        private IMessageAckService MessageAckService => SampleLiveOpenSdkManager.Sdk.GetMessageAckService();

        [Button]
        public void Debug()
        {
            UnityEngine.Debug.Log(MessagePushService);
        }

        public async Task Init()
        {
            // 必须等待直播间信息可用后才能进行后续操作。
            var r = await RoomInfoService.WaitForRoomInfoAsync();
            DySdkManager.Log($"注册直推事件监听 RoomId:{r.RoomId} AnchorOpenId:{r.Anchor.OpenId}");
            UploadLog(new[]{"RoomInfo","注册直推事件监听"} ,$"RoomId:{r.RoomId} AnchorOpenId:{r.Anchor.OpenId} , ");

            // 注册事件监听
            MessagePushService.OnConnectionStateChanged -= OnConnectionStateChanged;
            MessagePushService.OnConnectionStateChanged += OnConnectionStateChanged;

            MessagePushService.OnMessage -= OnMessage;
            MessagePushService.OnMessage += OnMessage;
            await StartPush();
        }

        public async Task<ILinkInfo> QueryLinkmicInfo()
        {
            try
            {
                var linkInfo = await AudienceLinkmicService.QueryLinkmicInfoAsync();
                DySdkManager.Log($"QueryLinkmicInfo 成功");
                return linkInfo;
            }
            catch (Exception)
            {
                DySdkManager.LogError($"QueryLinkmicInfo 失败");
            }

            return null;
        }

        public async Task InviteAudienceJoinGame(string openId)
        {
            try
            {
                await AudienceLinkmicService.InviteAudienceJoinGameAsync(openId);

                DySdkManager.Log($"InviteAudienceJoinGame 成功");
            }
            catch (Exception)
            {
                DySdkManager.LogError($"InviteAudienceJoinGame 失败");
            }
        }

        public async Task RequestAudienceLeaveGame(string openId)
        {
            try
            {
                await AudienceLinkmicService.RequestAudienceLeaveGameAsync(openId);

                DySdkManager.Log($"RequestAudienceLeaveGame 成功");
            }
            catch (Exception)
            {
                DySdkManager.LogError($"RequestAudienceLeaveGame 失败");
            }
        }

        // 开启推送任务，开启成功后才能收到指定类型的消息
        // 每场对局结束后建议停止推送任务
        public async Task StartPush(string msgType)
        {
            try
            {
                await MessagePushService.StartPushTaskAsync(msgType,MultiPushType.HTTPWithSDK);
                DySdkManager.Log($"开启 {msgType} 消息推送任务：成功 || 服务器双推模式( {Sdk.Version} >=2.7.4 )");
            }
            catch (Exception)
            {
                DySdkManager.LogError($"开启 {msgType} 消息推送任务：失败 ... 等待重新尝试");
                await UniTask.DelayFrame(60);
                //再次尝试
                StartPush(msgType);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state">Disconnected = 0未连接 | Connected = 1已连接</param>
        private void OnConnectionStateChanged(ConnectionState state)
        {
            DySdkManager.Log($"指令推送网络连接状态：{state}");
            StartPush();
        }

        /// <summary>
        /// 获取当前的连接状态
        /// </summary>
        public ConnectionState GetConnectionState => MessagePushService.ConnectionState;
        
        // 每场对局结束后建议停止推送任务
        private void OnStopPushTask()
        {
            foreach (var msgType in _msgTypes)
            {
                MessagePushService.StopPushTaskAsync(msgType);
            }
        }

        private async UniTask StartPush()
        {
            await Task.WhenAll(_msgTypes.Select(StartPush));
        }

        // 开启推送任务 =====> 开启成功后才能收到指定类型的消息
        private readonly string[] _msgTypes = 
        {
            PushMessageTypes.LiveComment, 
            PushMessageTypes.LiveGift, 
            PushMessageTypes.LiveLike,
            PushMessageTypes.LiveEnterRoom,
        };
        public void OnMessage(IPushMessage message)
        {
            OnMessage(message, SeatIndex.Index0);
            // 完成指令渲染后发送履约
            MessageAckService.ReportAck(message);
            // UploadLog(new[]{"ReportAck"} ,$"{message.MsgType} {message.MsgId}");
        }

        public void OnMessage(IPushMessage message, SeatIndex seatIndex)
        {
            var sb = new StringBuilder();
            sb.Append($"收到推送消息：{message.MsgId} {message.MsgType} {seatIndex} ---- ");
            switch (message)
            {
                case ICommentMessage data:
                    UploadLog(new []{"OnMessage","ICommentMessage"} ,$"{message}");
                    sb.Append($"{data.Sender.Nickname}-{data.Sender.OpenId} 说：{data.Content}");
                    break;
                case ILikeMessage data:
                    // UploadLog(tags ,$"{message}");
                    sb.Append($"{data.Sender.Nickname}-{data.Sender.OpenId} 点了 {data.LikeCount} 个赞");
                    break;
                case IGiftMessage data:
                    UploadLog(new []{"OnMessage","IGiftMessage"} ,$"{message}");
                    sb.Append($"{data.Sender.Nickname}-{data.Sender.OpenId} 送了 {data.GiftCount} 个礼物，价值 {data.GiftValue} 分");
                    break;
                case IEnterRoomMessage data:
                    UploadLog(new []{"OnMessage","IEnterRoomMessage"} ,$"{message}");
                    sb.Append($"{data.InviterGatherNickname} 摇人了 {data.NickName}[{data.SecOpenId}] 进1离2:{data.EnterRoomType}");
                    break;
                case IFansClubMessage data:
                    UploadLog(new []{"OnMessage","IFansClubMessage"} ,$"{message}",level: UploadLogLevel.Warn);
                    if (data.FansClubMessageType == IFansClubMessage.MessageType.Join)
                    {
                        sb.Append($"{data.Sender.Nickname}-{data.Sender.OpenId} 加入了粉丝团");
                    }
                    else if (data.FansClubMessageType == IFansClubMessage.MessageType.LevelUp)
                    {
                        sb.Append($"{data.Sender.Nickname}-{data.Sender.OpenId} 的粉丝团等级升到了 {data.FansClubLevel} 级");
                    }
                    break;
                case ITeamMessage data:
                    UploadLog(new []{"OnMessage","ITeamMessage"} ,$"{message}",level: UploadLogLevel.Warn);
                    // 调用上报阵营接口
                    SampleRoundManager.Instance.JoinGroup(data.Sender.OpenId, data.GroupId);
                    sb.Append($"{data.Sender.Nickname} 通过小摇杆加入了 {data.GroupId} 队伍");
                    // 玩法的其他处理
                    // ......
                    break;
                default:
                    UploadLog(new []{"OnMessage","OtherMessage"} ,$"{message}",level: UploadLogLevel.Warn);
                    break;
            }

            DySdkManager.Log(sb.ToString());
            DySdkDeserialize.Deserialize(message, seatIndex);
        }
        
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tags"></param>
        /// <param name="content"></param>
        /// <param name="traceId">在控制台检索日志时，日志会根据traceId进行聚合。不允许包含空格，字符上限32，超过会被截断。输入traceId，可进行检索。建议一个完整的请求链路用同一个traceId，方便调用链的排查。</param>
        public static async void UploadLog( string[] tags, string content, string traceId = null, UploadLogLevel level = UploadLogLevel.Debug)
        {
            SampleDebugUtilsManager.UploadLogWithTags(level, tags, content, traceId);
        }
    }
}