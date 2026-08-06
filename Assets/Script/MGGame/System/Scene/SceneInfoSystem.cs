using System;
using Cysharp.Threading.Tasks;

namespace XN
{
    public static class SceneInfoSystem
    {
        /// <summary>
        /// 保存池子数据
        /// </summary>
        /// <param name="self"></param>
        public static void SavePoolData(this SceneInfoComponent self)
        {
            self.LastScorePool = Math.Max(0, RoomHelper.GetRoomInfoComponent().ScorePool);
            self.LastFansPool = Math.Max(0, RoomHelper.GetRoomInfoComponent().FansPool);
        }

        /// <summary>
        /// 清理池子数据
        /// </summary>
        /// <param name="self"></param>
        public static void ClearPoolData(this SceneInfoComponent self)
        {
            self.LastScorePool = 0;
            self.LastFansPool = 0;
        }
        
        public static void SetSceneId(this SceneInfoComponent self, int sceneId)
        {
            // TODO 当前不用检测合法性，viewMain存储的目前都合法
            self.SceneId = sceneId;
        }

        /// <summary>
        /// 初始化池子数据
        /// </summary>
        /// <param name="self"></param>
        public static async UniTask InitPoolData(this SceneInfoComponent self)
        {
            var response = await PlayerMessage.SendGetPrizePool(self.AnchorOpenId);
            if (response.Code != 0)
            {
                Debug.LogError($"SendGetPrizePool:{response.Msg}");
                return;
            }
            
            //隔天刷新，判断积分池粉丝池保底
            var timeInfoComp = SceneHelper.GetTimeUnit().GetComponent<TimeInfoComponent>();
            long zeroTimeMs = TimeHelper.GetZeroTimeMs(timeInfoComp.ServerClientTime);

            var minimumGoldPool = TotalConfigManager.ConfigManager.ConstConfigCategory.MinimumGoldPool;
            var minimumFortunePool = TotalConfigManager.ConfigManager.ConstConfigCategory.MinimumFortunePool;
            
            if (response.SaveTime == 0)
            {
                self.LastScorePool = minimumGoldPool;
                self.LastFansPool = minimumFortunePool;
                return;
            }

            if (response.SaveTime < zeroTimeMs)
            {
                //隔天
                self.LastScorePool = Math.Max(minimumGoldPool, response.GoldPool);
                self.LastFansPool = Math.Max(minimumFortunePool, response.FortunePool);
                return;
            }
            
            self.LastScorePool = Math.Max(0, response.GoldPool);
            self.LastFansPool = Math.Max(0, response.FortunePool);
        }
    }
}