using System.Collections.Generic;
using cfg;

namespace XN
{
    public class RankInfoComponent : IComponent
    {
        public Dictionary<RankType, RankTimesData> RankInfo = new();
        
        /// <summary>
        /// 各榜单Top100出现之人（兼容 RankType + 勋章榜单） 所以 key ---> string
        /// </summary>
        public Dictionary<string, List<string>> RankTopPlayerShowDic = new();
        public override void OnCreate()
        {
        }

        public override void OnDestroy()
        {
        }
    }
}