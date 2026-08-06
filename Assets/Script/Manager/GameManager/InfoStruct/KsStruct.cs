using System.Collections.Generic;
using BestHTTP.JSON.LitJson;

namespace InfoStruct
{
    public static class KsDeserialize
    {
        private static readonly Dictionary<string, HashSet<long>> ConsumerDic = new();
        private static CmdManager Cmd => CmdManager.Instance;

        public static void Deserialize(string receivedMsg)
        {
            var item = JsonMapper.ToObject<KsPushBaseListVo<KsStruct>>(receivedMsg);
            if (!ConsumerDic.ContainsKey(item.userId)) ConsumerDic.Add(item.userId, new HashSet<long>());

            if (!ConsumerDic[item.userId].Contains(item.timestamp))
            {
                ConsumerDic[item.userId].Add(item.timestamp);
                //记录用户信息
                Cmd.UpdateUserInfo(item.userId, item.userName, item.headUrl);
                foreach (var info in item.data)
                    switch (item.type)
                    {
                        case "Comment": //聊天消息
                        {
                            Cmd.ChatMessage(info.content, item.userId, item.timestamp);
                            break;
                        }
                        case "Gift": //送礼
                        {
                            Cmd.GiftMessage(info.giftName, info.type.ToString(), info.count, item.userId,
                                item.timestamp);
                            break;
                        }
                        case "Like": //点赞 无人机
                        {
                            Cmd.LikeMessage(item.userId, info.count, item.timestamp);
                            break;
                        }
                    }
            }
            else
            {
                Debug.LogError($"时间戳重复,丢弃操作{item.timestamp}");
            }
        }
    }

    public struct KsStruct
    {
        /// <summary>
        /// 礼物校举id
        /// </summary>
        public int type;

        /// <summary>
        /// 礼物名称
        /// </summary>
        public string giftName;

        /// <summary>
        /// 礼物数量 | 点赞数量
        /// </summary>
        public int count;

        #region 聊天

        public string content;

        #endregion
    }

    public class KsPushBaseListVo<T>
    {
        /// <summary>
        /// 房间code码 *
        /// </summary>
        public string code;

        public T[] data;
        public string headUrl;

        /// <summary>
        /// 消息推送时间戳
        /// </summary>
        public long timestamp;

        public string type;
        public string userId;
        public string userName;
    }
}