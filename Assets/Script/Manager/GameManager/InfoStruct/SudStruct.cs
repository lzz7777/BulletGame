using System.Collections.Generic;
using BestHTTP.JSON.LitJson;

namespace InfoStruct
{
    public static class SudDeserialize
    {
        private static readonly HashSet<string> ConsumerDic = new();
        private static CmdManager Cmd => CmdManager.Instance;

        public static void Deserialize(string receivedMsg)
        {
            var item = JsonMapper.ToObject<SudPushBaseListVo>(receivedMsg);
            if (!ConsumerDic.Contains(item.message_id))
            {
                ConsumerDic.Add(item.message_id);
                foreach (var info in item.data.payload)
                {
                    var userInfo = info.user_info;
                    switch (item.@event)
                    {
                        // case "live_comment": //聊天消息
                        // {
                        //     if (!Cmd.HasUser(userInfo.UserId)) {
                        //         Cmd.AddUser(userInfo.UserId, userInfo.UserName, userInfo.HeadUrl);
                        //     }
                        //
                        //     Cmd.ChatMessage(info.Content, userInfo.UserId);
                        //     break;
                        // }
                        // case "gift_send": //送礼
                        // {
                        //     Cmd.GiftMessage(info.GiftName, info.Type.ToString(), info.Count, userInfo.UserId);
                        //     break;
                        // }
                        // case "live_like": //点赞 无人机
                        // {
                        //     Cmd.LikeMessage(userInfo.UserId);
                        //     break;
                        // }
                        //case "MemberMessage": //进入房间
                        // break;
                    }
                }
            }
            else
            {
                Debug.LogError($"MsgID重复,丢弃操作{item.message_id}");
            }
        }
    }

    public class UserInfo
    {
        public string head_url;
        public string user_id;
        public string user_name;
    }

    public class Payload
    {
        #region 聊天

        public string content;

        #endregion

        /// <summary>
        /// 礼物数量 | 点赞数量
        /// </summary>
        public int gift_count;

        /// <summary>
        /// 礼物校举id
        /// </summary>
        public int gift_id;

        /// <summary>
        /// 助威火炬
        /// </summary>
        public string gift_name;

        public UserInfo user_info;
    }

    public class SudData
    {
        /// <summary>
        /// 实际信息
        /// </summary>
        public Payload[] payload;

        /// <summary>
        /// 房间号
        /// </summary>
        public string room_code;
    }

    public class SudPushBaseListVo
    {
        public SudData data;

        /// <summary>
        /// 消息类型
        /// </summary>
        public string @event;

        public string message_id;

        /// <summary>
        /// 消息推送时间戳
        /// </summary>
        public long timestamp;
    }
}