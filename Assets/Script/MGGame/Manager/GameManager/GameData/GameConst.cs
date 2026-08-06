/// <summary>
/// 游戏常量
/// </summary>

using YooAsset;

namespace XN
{
    public class GameConst
    {
        public const int ScreenWidth = 1080;
        public const int CarLayer = 150;

        public const int ScoreId = 1;
        public const int FansId = 2;
        public const int OilDrum = 3;
        public const int Mileage = 4;

        public const int Star = 5;

        //月榜积分
        public const int MonthScore = 6;

        /// <summary>
        /// 秒榜
        /// </summary>
        public const int KillCount = 7;

        public const int CarAniType = 1; //0:DOtween动画+X参照组计算 1:插值动画+由第一名和最后一名决定

        public const float FirstCarScale = 1.3f;

        /// <summary>
        /// 最大里程逻辑开关
        /// </summary>
        public static bool IsOpenMaximumRange = false;

        public static int DebugInt;
        public static bool DebugType => DebugInt == 1; // 用于显示调试

        public static EPlayMode PlayMode = EPlayMode.EditorSimulateMode;

        //优化测试
        public static bool IsOptimized = true;
        
        /// <summary>
        /// BaseUrl + GameUrl  ---> https://app.apifox.com/project/7361514
        /// </summary>
        public static class Url
        {
            /// <summary>
            /// 上报结算数据
            /// </summary>
            public const string Post_BattleResult = "/api/combat/result";

            /// <summary>
            /// 获取玩家信息
            /// </summary>
            public const string Post_GetPlayerInfo = "/api/unit/getUnitInfo";

            /// <summary>
            /// 获取玩家道具
            /// </summary>
            public const string Post_GetBagDataRequest = "/api/bag/getBagData";

            /// <summary>
            /// 指令操作
            /// </summary>
            public const string Post_CmdOperationRequest = "/api/unit/cmdOperation";

            /// <summary>
            /// 排行榜 - 查询排行榜数据 从A到B
            /// </summary>
            public const string Post_RankQueryByNum = "/api/rank/query";

            /// <summary>
            /// 排行榜 - 根据PlayerIds查询榜单
            /// </summary>
            public const string Post_RankQueryByIds = "/api/rank/querybyplayerId";

            /// <summary>
            /// 获取服务器时间
            /// </summary>
            public const string Get_GetServerTime = "/api/server/getServerTime";

            /// <summary>
            /// 获取奖池
            /// </summary>
            public const string Post_GetPrizePool = "/api/game/getPrizePool";

            /// <summary>
            /// 设置奖池
            /// </summary>
            public const string Post_SetPrizePool = "/api/game/setPrizePool";
        }
    }
}