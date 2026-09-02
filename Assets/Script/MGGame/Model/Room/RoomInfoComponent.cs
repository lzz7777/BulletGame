using System.Collections.Generic;
using cfg;

namespace XN
{
    public class RoomInfoComponent : ComponentBase
    {
        public int RoomID { get; set; }
        public float Time { get; set; }
        public int EndTime { get; set; }

        public List<long> CarIds = new();
        public Dictionary<long, int> CarRankDic = new();

        public Dictionary<string, long> PlayerIds = new();

        public Dictionary<string, UserInfo> UserInfos = new();

        /// <summary>
        /// 加入了当局玩法的玩家排行数据
        /// </summary>
        public Dictionary<RankType, Dictionary<string, RankNode>> RankTypeDic = new();

        public double ScorePool { get; set; }
        public double FansPool { get; set; }

        /// <summary>
        /// 子场景索引
        /// </summary>
        public int TimeSceneInfoIndex { get; set; }

        /// <summary>
        /// 子场景信息
        /// </summary>
        public Planning ScenePlanning { get; set; }

        /// <summary>
        /// 下个场景出发时间
        /// </summary>
        public float NextTimePlanning { get; set; }
        
        /// <summary>
        /// 显示秒榜信息
        /// </summary>
        public bool IsShowMaximumRange { get; set; }
        
        public override void OnCreate()
        {
        }

        public override void OnDestroy()
        {
            RoomID = default;
            Time = default;
            EndTime = default;
            CarIds.Clear();
            CarRankDic.Clear();
            PlayerIds.Clear();
            UserInfos.Clear();
            RankTypeDic.Clear();
            ScorePool = default;
            FansPool = default;
            TimeSceneInfoIndex = default;
            ScenePlanning = default;
            NextTimePlanning = default;
            IsShowMaximumRange = default;
        }
    }
}