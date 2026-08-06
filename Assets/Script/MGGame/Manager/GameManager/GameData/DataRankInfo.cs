using System;
using System.Collections.Generic;
using System.Linq;
using Apifox;
using cfg;
using Cysharp.Threading.Tasks;
using UnityEngine.Events;
using XN;

namespace XN
{
    #region 同步后端数据结构

    public class QueryRankResponse
    {
        /// <summary>
        /// 排行榜数据
        /// </summary>
        public List<RankDataRet> RankList;
    }

    public class RankDataRet
    {
        /// <summary>
        /// 玩家id
        /// </summary>
        public string PlayerId;

        /// <summary>
        /// 玩家名称
        /// </summary>
        public string Nickname;

        /// <summary>
        /// 玩家头像url
        /// </summary>
        public string AvatarUrl;

        /// <summary>
        /// 玩家分数
        /// </summary>
        public double Score;

        /// <summary>
        /// 玩家排名
        /// </summary>
        public int Rank;

        /// <summary>
        /// 粉丝，七天榜要额外使用
        /// </summary>
        public double Fans;
    }

    public class ServerResponse
    {
        public long ServerId;
        public long ServerTime;
        public long ClientTime;
        public Dictionary<string, RankTimesData> RankTimes = new();
    }
    
    public class RankTimesData
    {
        public long StartTime;
        public long EndTime;
    }
    
    #endregion

    public static partial class DataManager
    {
        /// <summary>
        /// 获取传入玩家列表排名
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="iDs"></param>
        public static async UniTask<List<RankDataRet>> GetRankIndexInfo(
            RankType rankType, string[] Ids,
            UnityAction<List<RankDataRet>> callback = null)
        {
            Ids = Ids.Where(b => b != null).ToArray();
            var param = new Dictionary<string, object>
            {
                { "GameName", TotalConfigManager.ConfigManager.ConstConfigCategory.GameName },
                { "RankType", rankType.ToString() },    // 按照服务器协议调整 string
                { "PlayerIDs", Ids },
            };

            var resp = await AsyncSendPost<QueryRankResponse>(GameConst.Url.Post_RankQueryByIds, body: param);
            callback?.Invoke(resp.RankList);
            return resp.RankList;
        }

        /// <summary>
        /// 获取排名区间的玩家
        /// </summary>
        /// <param name="rankType"> 排行榜类型</param>
        /// <param name="start">从第几名开始</param>
        /// <param name="end">到第几名</param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static async UniTask<List<RankDataRet>> GetRankIndexInfo(
            RankType rankType, int start = 0, int end = 49,
            UnityAction<List<RankDataRet>> callback = null)
        {
            var param = new Dictionary<string, object>
            {
                { "GameName", TotalConfigManager.ConfigManager.ConstConfigCategory.GameName },
                { "RankType", rankType.ToString() },    // 按照服务器协议调整 string
                { "Start", start },
                { "End", end },
            };
            var resp = await AsyncSendPost<QueryRankResponse>(GameConst.Url.Post_RankQueryByNum, body: param);
            callback?.Invoke(resp.RankList);
            return resp.RankList;
        }

        public static async UniTask<ServerResponse> GetRankOverTimes()
        {
            var resp = await AsyncSendGet<ServerResponse>(GameConst.Url.Get_GetServerTime);
            // callback?.Invoke(rankOverTimes);
            return resp;
        }
    }
}