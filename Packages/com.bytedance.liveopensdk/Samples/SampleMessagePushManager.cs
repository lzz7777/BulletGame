// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using System.Text;
using System.Threading.Tasks;
using ByteDance.Live.Foundation.Logging;
using ByteDance.LiveOpenSdk.AudienceLinkmic;
using ByteDance.LiveOpenSdk.Push;
using ByteDance.LiveOpenSdk.Report;
using ByteDance.LiveOpenSdk.Room;
using ByteDance.LiveOpenSdk.Runtime.Utilities;
using ByteDance.LiveOpenSdk.Utilities;

namespace Douyin.LiveOpenSDK.Samples
{
    /// <summary>
    /// 直播开放 SDK 指令直推能力的接入示例代码。
    /// </summary>
    public static class SampleMessagePushManager
    {
        private static IRoomInfoService RoomInfoService => SampleLiveOpenSdkManager.Sdk.GetRoomInfoService();

        private static IAudienceLinkmicService AudienceLinkmicService =>
            SampleLiveOpenSdkManager.Sdk.GetAudienceLinkmicService();

        private static IMessagePushService MessagePushService => SampleLiveOpenSdkManager.Sdk.GetMessagePushService();
        private static IMessageAckService MessageAckService => SampleLiveOpenSdkManager.Sdk.GetMessageAckService();

        private static readonly LogWriter Log = new LogWriter(SdkUnityLogger.LogSink, "SampleMessagePushManager");

        public static async Task Init()
        {
            // 必须等待直播间信息可用后才能进行后续操作。
            await RoomInfoService.WaitForRoomInfoAsync();

            // 注册事件监听
            MessagePushService.OnConnectionStateChanged -= OnConnectionStateChanged;
            MessagePushService.OnConnectionStateChanged += OnConnectionStateChanged;

            MessagePushService.OnMessage -= OnMessage;
            MessagePushService.OnMessage += OnMessage;
        }

        public static async Task<ILinkInfo> QueryLinkmicInfo()
        {
            try
            {
                var linkInfo = await AudienceLinkmicService.QueryLinkmicInfoAsync();
                Log.Info($"QueryLinkmicInfo 成功");
                return linkInfo;
            }
            catch (Exception)
            {
                Log.Error($"QueryLinkmicInfo 失败");
            }

            return null;
        }

        public static async Task InviteAudienceJoinGame(string openId)
        {
            try
            {
                await AudienceLinkmicService.InviteAudienceJoinGameAsync(openId);

                Log.Info($"InviteAudienceJoinGame 成功");
            }
            catch (Exception)
            {
                Log.Error($"InviteAudienceJoinGame 失败");
            }
        }

        public static async Task RequestAudienceLeaveGame(string openId)
        {
            try
            {
                await AudienceLinkmicService.RequestAudienceLeaveGameAsync(openId);

                Log.Info($"RequestAudienceLeaveGame 成功");
            }
            catch (Exception)
            {
                Log.Error($"RequestAudienceLeaveGame 失败");
            }
        }

        // 开启推送任务，开启成功后才能收到指定类型的消息
        // 每场对局结束后建议停止推送任务
        public static async Task StartPush(string msgType)
        {
            try
            {
                await MessagePushService.StartPushTaskAsync(msgType);
                Log.Info($"开启 {msgType} 消息推送任务：成功");
            }
            catch (Exception)
            {
                Log.Error($"开启 {msgType} 消息推送任务：失败");
            }
        }

        private static void OnConnectionStateChanged(ConnectionState state)
        {
            Log.Info($"指令推送网络连接状态：{state}");
        }

        private static void OnMessage(IPushMessage message)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"收到推送消息：{message.MsgId} {message.MsgType}");
            switch (message)
            {
                case ICommentMessage data:
                    sb.Append($"{data.Sender.Nickname} 说：{data.Content}");
                    break;
                case ILikeMessage data:
                    sb.Append($"{data.Sender.Nickname} 点了 {data.LikeCount} 个赞");
                    break;
                case IGiftMessage data:
                    sb.Append($"{data.Sender.Nickname} 送了 {data.GiftCount} 个礼物，价值 {data.GiftValue} 分");
                    break;
                case IFansClubMessage data:
                    if (data.FansClubMessageType == IFansClubMessage.MessageType.Join)
                    {
                        sb.Append($"{data.Sender.Nickname} 加入了粉丝团");
                    }
                    else if (data.FansClubMessageType == IFansClubMessage.MessageType.LevelUp)
                    {
                        sb.Append($"{data.Sender.Nickname} 的粉丝团等级升到了 {data.FansClubLevel} 级");
                    }
                    break;
                case ITeamMessage data:
                    // 调用上报阵营接口
                    SampleRoundManager.Instance.JoinGroup(data.Sender.OpenId, data.GroupId);
                    sb.Append($"{data.Sender.Nickname} 通过小摇杆加入了 {data.GroupId} 队伍");
                    // 玩法的其他处理
                    // ......
                    break;
            }

            Log.Info(sb.ToString());

            // 完成指令渲染后发送履约
            MessageAckService.ReportAck(message);
        }
    }
}