using System;
using System.Collections.Generic;
using ByteDance.CloudSync;
using ByteDance.LiveOpenSdk.Push;

namespace InfoStruct
{
    public static class DySdkDeserialize
    {
        private static readonly Dictionary<string, HashSet<string>> ConsumerDic = new();
        private static XN.CmdManager Cmd => XN.CmdManager.Instance;

        /// <summary>
        /// 接受指令推送
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="seatIndex">玩家的index,联机模式的时候有用</param>
        /// <exception cref="Exception"></exception>
        public static void Deserialize(IPushMessage message, SeatIndex seatIndex = SeatIndex.Index0)
        {
            //记录用户信息
            try
            {
                if (Cmd == null)
                {
                    throw new Exception("场景未实例化好");
                }

                switch (message)
                {
                    case ICommentMessage data:
                        Cmd.SDKMessageUpdateUserInfo(data.Sender.OpenId, data.Sender.Nickname, data.Sender.AvatarUrl);
                        Cmd.SDKMessageChat(data.Sender.OpenId, data.Content, data.Timestamp);
                        break;
                    case ILikeMessage data:
                        Cmd.SDKMessageUpdateUserInfo(data.Sender.OpenId, data.Sender.Nickname, data.Sender.AvatarUrl);
                        Cmd.SDKMessageLike(data.Sender.OpenId, (int)data.LikeCount, data.Timestamp);
                        break;
                    case IGiftMessage data:
                        Cmd.SDKMessageUpdateUserInfo(data.Sender.OpenId, data.Sender.Nickname, data.Sender.AvatarUrl);
                        Cmd.SDKMessageGift(data.Sender.OpenId, data.SecGiftId, (int)data.GiftCount, data.Timestamp);
                        break;
                    case IEnterRoomMessage data:
                        Debug.Log($"IEnterRoomMessage : {data}");
                        // data.SecOpenId
                        Cmd.SDKMessageUpdateUserInfo(data.SecOpenId, data.NickName, data.AvatarUrl);
                        if (data.EnterRoomType == 1)
                        {
                            Cmd.SDKMessageInviteFriend(data.InviterGatherOpenid, data.SecOpenId, data.Timestamp);
                        }
                        break;
                    case IFansClubMessage data:
                        Cmd.SDKMessageUpdateUserInfo(data.Sender.OpenId, data.Sender.Nickname, data.Sender.AvatarUrl);
                        Debug.Log($"IFansClubMessage : {data}");
                        break;
                    case ITeamMessage data:
                        Cmd.SDKMessageUpdateUserInfo(data.Sender.OpenId, data.Sender.Nickname, data.Sender.AvatarUrl);
                        Debug.Log($"ITeamMessage : {data}");
                        break;
                    default:
                        Debug.Log(message);
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
        }
    }
}