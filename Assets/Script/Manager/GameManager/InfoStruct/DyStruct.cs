using System;
using System.Collections.Generic;
using System.Linq;
using Apifox;
using BestHTTP.JSON.LitJson;
using GameMain;

namespace InfoStruct
{
    public static class DyDeserialize
    {
        private const int PageSize = 100;
        private static readonly Dictionary<string, HashSet<string>> ConsumerDic = new();

        private static long _dt;
        private static int _page = 1;
        private static CmdManager Cmd => CmdManager.Instance;

        public static void Deserialize(string receivedMsg)
        {
            var item = JsonMapper.ToObject<DyPushBaseListVo<DyStruct>>(receivedMsg);
            DeserializeItem(item);
        }

        public static void UpdateFailStatus()
        {
            if (++_dt % (60 * 10) == 0) GetFailStatus();
        }

        private static void GetFailStatus()
        {
            DataManager.GetFailStatus<DyPushBaseFailListVo<DyStruct>>(DeserializeFailItem, _page);
        }

        public static void DeserializeFailItem(string data)
        {
            DeserializeFailItem(JsonMapper.ToObject<RespRet<DyPushBaseFailListVo<DyStruct>>>(data));
        }

        private static void DeserializeFailItem(RespRet<DyPushBaseFailListVo<DyStruct>> data)
        {
            try
            {
                if (data.code != 0) return;
                var item = data.data;
                foreach (var info in item.data_list)
                {
                    var d = info.Payload();
                    if (d.All(dyStruct =>
                            !(MathF.Abs(dyStruct.timestamp - (long)DateTimeHelper.TimestampMs) > 1800000)))
                    {
                        var i = new DyPushBaseListVo<DyStruct>
                        {
                            data = d,
                            msgType = info.msg_type
                        };
                        DeserializeItem(i, true);
                    }
                }

                _page = item.page_num;
                var maxPage = item.total_count / PageSize + 1;
                if (maxPage > _page)
                {
                    _page++;
                    //马上请求下一页
                    GetFailStatus();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        private static void DeserializeItem(DyPushBaseListVo<DyStruct> item, bool isFail = false)
        {
            //记录用户信息
            foreach (var info in item.data)
                try
                {
                    if (!ConsumerDic.ContainsKey(info.sec_openid))
                        ConsumerDic.Add(info.sec_openid, new HashSet<string>());

                    if (!ConsumerDic[info.sec_openid].Contains(info.msg_id))
                    {
                        ConsumerDic[info.sec_openid].Add(info.msg_id);
                    }
                    else
                    {
                        if (!isFail) Debug.LogError($"ID重复,丢弃操作{info.msg_id}");

                        continue;
                    }

                    if (Cmd == null) throw new Exception("场景未实例化好");

                    Cmd.UpdateUserInfo(info.sec_openid, info.nickname, info.avatar_url);
                    switch (item.msgType)
                    {
                        case "live_comment": //聊天消息
                        {
                            Cmd.ChatMessage(info.content, info.sec_openid, info.timestamp);
                            break;
                        }
                        case "live_gift": //送礼
                        {
                            Cmd.GiftMessage("", info.sec_gift_id, info.gift_num, info.sec_openid, info.timestamp);
                            break;
                        }
                        case "live_like": //点赞 无人机
                        {
                            Cmd.LikeMessage(info.sec_openid, info.like_num, info.timestamp);
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    throw;
                }
        }
    }

    public struct DyStruct
    {
        #region 通用

        /// <summary>
        /// string类型id
        /// </summary>
        public string msg_id;

        /// <summary>
        /// 用户的加密openid，当前其实没有加密
        /// </summary>
        public string sec_openid;

        /// <summary>
        /// 用户头像
        /// </summary>
        public string avatar_url;

        /// <summary>
        /// 用户昵称(不加密)
        /// </summary>
        public string nickname;

        /// <summary>
        /// 毫秒级时间戳
        /// </summary>
        public long timestamp;

        #endregion

        #region 评论

        /// <summary>
        /// 评论内容
        /// </summary>
        public string content;

        #endregion

        #region 礼物

        /// <summary>
        /// 加密的礼物id
        /// </summary>
        public string sec_gift_id;

        /// <summary>
        /// 送出的礼物数量
        /// </summary>
        public int gift_num;

        /// <summary>
        /// 礼物总价值，单位分
        /// </summary>
        public int gift_value;

        #endregion

        #region 点赞

        /// <summary>
        /// 点赞数量，上游2s合并一次数据
        /// </summary>
        public int like_num;

        #endregion
    }

    public struct DyPushBaseListVo<T>
    {
        public string msgType;

        public T[] data;
    }

    public struct DyPushBaseFailListVo<T>
    {
        public int page_num;
        public int total_count;
        public DyPushBaseFailPayLoad<T>[] data_list;
    }

    public struct DyPushBaseFailPayLoad<T>
    {
        public string room_id;
        public string msg_type;
        public string payload;

        public T[] Payload()
        {
            return JsonMapper.ToObject<T[]>(payload);
        }
    }
}