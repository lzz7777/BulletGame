using System.Collections.Generic;
using Apifox;
using cfg;
using cfg.Fight;
using Cysharp.Threading.Tasks;
using Unity.Mathematics.Geometry;
using Math = System.Math;
using Random = System.Random;

namespace XN
{
    public static class RoomInfoSystem
    {
        [UpdateSystem]
        public static void Update(this RoomInfoComponent self, float deltaTime)
        {
            if (!GameStateCtrl.IsGaming)
                return;

            if (self.Time >= self.EndTime)
            {
                if (GameStateCtrl.IsGaming)
                {
                    GameStateCtrl.UpdateState(MGGameState.到达终点);
                    GameStateCtrl.UpdateState(MGGameState.游戏结束);
                }

                return;
            }

            self.CheckMaximumRange();
            
            self.Time += deltaTime;

            self.UpdateSceneInfo(deltaTime);
        }

        /// <summary>
        /// 判断秒榜
        /// </summary>
        /// <param name="self"></param>
        private static void CheckMaximumRange(this RoomInfoComponent self)
        {
            var roomConf = RoomHelper.GetFightRoomConfig();
            var maximumRange = roomConf.MaximumRange;
            if (maximumRange == 0)
                return;

            if (!GameConst.IsOpenMaximumRange)
            {
                return;
            }

            var carInfoComp = EntityManager.Instance.GetEntityById(RoomHelper.GetCars()[0]).GetComponent<CarInfoComponent>();
            var mileage = carInfoComp.Mileage;

            if (!self.IsShowMaximumRange && mileage >= maximumRange * roomConf.MaximumRangeShow)
            {
                self.IsShowMaximumRange = true;
                EventsManager.BroadCast(GameEnum.ViewMaximumRangeNodeShowEvent);
            }
            
            if (mileage >= maximumRange)
            {
                //添加玩家秒榜数据
                if (carInfoComp.PlayerIds.Count > 0)
                {
                    var playerId = carInfoComp.PlayerIds[0];
                    RoomHelper.GetRoomInfoComponent().GetPlayerItemComponent(playerId).AddMaximumRangeData();
                }

                //游戏结束
                GameStateCtrl.UpdateState(MGGameState.到达终点);
                GameStateCtrl.UpdateState(MGGameState.游戏结束);
            }
        }
        
        /// <summary>
        /// 更新场景信息
        /// </summary>
        /// <param name="self"></param>
        /// <param name="deltaTime"></param>
        private static void UpdateSceneInfo(this RoomInfoComponent self, float deltaTime)
        {
            if (self.Time < self.NextTimePlanning)
            {
                return;
            }

            var timeSceneInfo = self.GetFightRoomConfig().TimeSceneInfo;
            if (self.TimeSceneInfoIndex >= timeSceneInfo.Count - 1)
            {
                return;
            }

            EventsManager.BroadCast(GameEnum.UpdateSceneInfo, self.TimeSceneInfoIndex + 1);
        }

        /// <summary>
        /// 检查是否加入玩家
        /// </summary>
        /// <param name="self"></param>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public static async UniTask<bool> CheckAddPlayer(this RoomInfoComponent self, string playerId)
        {
            if (self.PlayerIds.ContainsKey(playerId))
            {
                return false;
            }

            var playUnit = self.Entity.AddChild(EntityType.Player);
            self.PlayerIds[playerId] = playUnit.Id;
            var playerInfoComp = playUnit.AddComponent<PlayerInfoComponent>();
            var playerItemComp = playUnit.AddComponent<PlayerItemComponent>();

            //获取玩家服务器信息
            //上报玩家
            var respLoginRequest = await PlayerMessage.SendLoginRequest(playerId);
            playerInfoComp.PlayerId = playerId;
            playerInfoComp.Name = respLoginRequest.Nickname;
            playerInfoComp.AvatarUrl = respLoginRequest.AvatarUrl;
            playerInfoComp.CustomVideoId = respLoginRequest.VideoId;
            playerInfoComp.SetSkinData((int)respLoginRequest.SkinId, respLoginRequest.EffectsId);
            playerInfoComp.SetSex((SexType)respLoginRequest.Sex);
            
            // 获取各种排行榜
            RoomManager.Instance.UpdatePlayerRank(respLoginRequest.PlayerId, respLoginRequest.Ranks);
            
            //获取玩家标签
            playerInfoComp.SetPlayerTitle(respLoginRequest.SkinName);
            
            //获取道具
            var respBagRequest = await PlayerMessage.SendBagRequest(playerId);
            playerItemComp.SetItemData(respBagRequest.BagDataList);

            playerInfoComp.OrigScore = playerItemComp.GetItemNum(GameConst.ScoreId);
            playerInfoComp.OrigFans = playerItemComp.GetItemNum(GameConst.FansId);

            return true;
        }

        public static Entity GetPlayerUnit(this RoomInfoComponent self, string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
            {
                return null;
            }

            self.PlayerIds.TryGetValue(playerId, out var playerLongId);

            return EntityManager.Instance.GetEntityById(playerLongId);
        }

        public static PlayerInfoComponent GetPlayerInfoComponent(this RoomInfoComponent self, string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
            {
                return null;
            }

            self.PlayerIds.TryGetValue(playerId, out var playerLongId);

            var playerUnit = EntityManager.Instance.GetEntityById(playerLongId);

            return playerUnit?.GetComponent<PlayerInfoComponent>();
        }

        public static PlayerItemComponent GetPlayerItemComponent(this RoomInfoComponent self, string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
            {
                return null;
            }

            self.PlayerIds.TryGetValue(playerId, out var playerLongId);

            var playerUnit = EntityManager.Instance.GetEntityById(playerLongId);

            return playerUnit?.GetComponent<PlayerItemComponent>();
        }

        /// <summary>
        /// 检查玩家是否加入车队
        /// </summary>
        /// <param name="self"></param>
        /// <param name="playerId"></param>
        /// <param name="inputId"></param>
        /// <param name="targetCarId"></param>
        /// <returns></returns>
        public static async UniTask CheckPlayerAddCar(this RoomInfoComponent self, string playerId, int inputId,
            long targetCarId = 0)
        {
            if (!self.PlayerIds.ContainsKey(playerId))
            {
                Debug.LogError($"playerId {playerId} is not exist");
                return;
            }

            var playerInfoComp = self.GetPlayerInfoComponent(playerId);
            if (playerInfoComp.CarId != 0)
            {
                return;
            }

            if (targetCarId == 0)
            {
                targetCarId = RoomHelper.GetJoinCar();
            }

            playerInfoComp.CarId = targetCarId;

            var carUnit = EntityManager.Instance.GetEntityById(targetCarId);
            var carInfoComp = carUnit.GetComponent<CarInfoComponent>();
            carInfoComp.PlayerJoinCar(playerId);
            RoomHelper.GetRoomInfoComponent().CalculateData(playerId);
            playerInfoComp.ScoreTime = TimeHelper.GetTimeStampMs();

            //显示层
            var carViewComp = carUnit.GetComponent<CarViewComponent>();
            carViewComp.RefreshCarScaleX();
            carViewComp.ViewCarInfoItem.RefreshInfo();
            await carViewComp.SwitchSkin();
            carViewComp.RefreshCarTitle();
            
            //玩家加入车队 
            EventsManager.BroadCast(GameEnum.PlayerJoinCar, playerId, inputId, targetCarId);
        }

        /// <summary>
        /// 玩家加入车队计算积分粉丝
        /// </summary>
        /// <param name="self"></param>
        /// <param name="playerId"></param>
        public static void CalculateData(this RoomInfoComponent self, string playerId)
        {
            var playerItemComp = self.GetPlayerItemComponent(playerId);
            var playerInfoComp = self.GetPlayerInfoComponent(playerId);
            
            //积分计算，单人加入超过280，加120，不够280就加120*x/280
            // float ownScore = playerItemComp.GetItemNum(GameConst.ScoreId);
            // var pointJoinAdd = TotalConfigManager.ConfigManager.ConstConfigCategory.PointJoinAdd;
            // var pointJoinPass = TotalConfigManager.ConfigManager.ConstConfigCategory.PointJoinPass;
            // if (ownScore >= pointJoinPass)
            // {
            //     playerItemComp.SetItemNum(GameConst.ScoreId, (int)(ownScore - pointJoinPass));
            //     self.ScorePool += pointJoinAdd;
            // }
            // else
            // {
            //     self.ScorePool += pointJoinAdd * ownScore / pointJoinPass;
            //     playerItemComp.SetItemNum(GameConst.ScoreId, 0);
            // }

            //粉丝计算,去除x进入粉丝池
            double ownFans = playerItemComp.GetItemNum(GameConst.FansId);
            var fansJoinPct = TotalConfigManager.ConfigManager.ConstConfigCategory.FansJoinPct;
            double addFansPool = ownFans * fansJoinPct;

            playerInfoComp.LoseFans = addFansPool;
            playerItemComp.SetItemNum(GameConst.FansId, ownFans * (1 - fansJoinPct));
            self.FansPool += addFansPool;
        }

        /// <summary>
        /// 添加积分
        /// </summary>
        /// <param name="self"></param>
        /// <param name="score"></param>
        public static void AddScore(this RoomInfoComponent self, float score)
        {
            self.ScorePool += score;
        }

        /// <summary>
        /// 添加粉丝
        /// </summary>
        /// <param name="self"></param>
        /// <param name="value"></param>
        public static void AddFans(this RoomInfoComponent self, double value)
        {
            self.FansPool += value;
        }
        
        /// <summary>
        /// 房间数据结算
        /// </summary>
        /// <param name="self"></param>
        public static void SettleData(this RoomInfoComponent self)
        {
            self.SettleScore();
            self.SettleFans();
        }

        /// <summary>
        /// 结算积分
        /// </summary>
        /// <param name="self"></param>
        public static void SettleScore(this RoomInfoComponent self)
        {
            var playerCars = RoomHelper.GetPlayerCars();
            if (playerCars.Count == 0)
            {
                //没人玩，积分不扣除
                var pointStartAdd = TotalConfigManager.ConfigManager.ConstConfigCategory.PointStartAdd;
                self.ScorePool /= pointStartAdd;
                return;
            }

            /*
            玩家赢得积分=（玩家积分贡献比*55%可分配积分+总积分*前3名的配比*队员贡献比)(1+排行加成)
            TOP1-3 可分配比例45%
            TOP1=20%
            TOP2=15%
            TOP3=10%
            */
            
            //所有玩家贡献积分
            double ScoreSum = 0;
            var roomInfoComp = RoomHelper.GetRoomInfoComponent();
            var pids = RoomHelper.GetPlayerIdsInCar();
            foreach (var playerId in pids)
            {
                ScoreSum += roomInfoComp.GetPlayerInfoComponent(playerId).Score;
            }

            //可分配积分
            var addPoolScore = self.ScorePool * TotalConfigManager.ConfigManager.ConstConfigCategory.PointShareOut;
            
            var constConf = TotalConfigManager.ConfigManager.ConstConfigCategory;
            //车队加成
            List<float> carRatioList = new()
                { constConf.PointFirstGet, constConf.PointSecondGet, constConf.PointThirdGet };
            
            //有人的前三车队 carId 名次
            Dictionary<long, int> playerCarDic = new();
            
            //前三车队玩家贡献积分
            List<double> carScoreList = new();
            for (int i = 0; i < playerCars.Count; i++)
            {
                var carId = playerCars[i];
                playerCarDic[carId] = i;
                
                //前三车队所占积分
                if (i < 3)
                {
                    double score = 0;
                    foreach (var playerId in EntityManager.Instance.GetEntityById(carId).GetComponent<CarInfoComponent>()
                                 .PlayerIds)
                    {
                        score += RoomHelper.GetRoomInfoComponent().GetPlayerInfoComponent(playerId).Score;
                    }

                    carScoreList.Add(score);
                }
            }
            
            foreach (var playerId in pids)
            {
                double scoreRatio = 0;
                var playerInfoComp = roomInfoComp.GetPlayerInfoComponent(playerId);
                var curScore = playerInfoComp.Score;
                var curCarId = playerInfoComp.CarId;
                
                scoreRatio = ScoreSum != 0 ? curScore / ScoreSum : 0;
                
                //排行加成
                var rankData = RoomManager.Instance.GetPlayerRank(RankType.FansRank, playerId);
                var rankRewardConf =
                    TotalConfigManager.ConfigManager.RankRewardConfigCategory.GetOrDefault(rankData.rankIndex);
                float rankRatio = rankRewardConf?.FansRankPointAdd ?? 0;

                //前三名车队加成 总积分*前3名的配比*队员贡献比
                double carAddScore = 0;
                if (playerCarDic.TryGetValue(curCarId, out int carIndex) && carIndex < 3)
                {
                    var carRatio = carRatioList[carIndex];
                    var carScore = carScoreList[carIndex];
                    var playerCarScoreRatio = carScore != 0 ? curScore / carScore : 0;

                    carAddScore = self.ScorePool * carRatio * playerCarScoreRatio;
                }
                
                //玩家赢得积分=（玩家积分贡献比*55%可分配积分+总积分*前3名的配比*队员贡献比)(1+排行加成)
                playerInfoComp.WinScore = (scoreRatio * addPoolScore + carAddScore) * (1 + rankRatio);
            }
        }

        /// <summary>
        /// 结算粉丝
        /// </summary>
        /// <param name="self"></param>
        public static void SettleFans(this RoomInfoComponent self)
        {
            var playerCars = RoomHelper.GetPlayerCars();
            
            //粉丝计算：第一队赢所有，按贡献分
            if (playerCars.Count == 0)
            {
                //没人玩，粉丝池保留
                return;
            }
            
            var carPlayerIds = EntityManager.Instance.GetEntityById(playerCars[0])
                .GetComponent<CarInfoComponent>().PlayerIds;

            //在第一车队的玩家返还粉丝数，瓜分其他车队的玩家粉丝
            foreach (var playerId in carPlayerIds)
            {
                var playerInfoComp = self.GetPlayerInfoComponent(playerId);
                var playerItemComp = self.GetPlayerItemComponent(playerId);
                
                var fans = playerItemComp.GetItemNum(GameConst.FansId) + playerInfoComp.LoseFans;
                playerItemComp.SetItemNum(GameConst.FansId, fans);
                
                self.FansPool = Math.Max(0, self.FansPool - playerInfoComp.LoseFans);
            }

            //第一名车队玩家贡献积分
            double carScore = 0;
            foreach (var playerId in EntityManager.Instance.GetEntityById(playerCars[0]).GetComponent<CarInfoComponent>()
                         .PlayerIds)
            {
                carScore += RoomHelper.GetRoomInfoComponent().GetPlayerInfoComponent(playerId).Score;
            }
            
            foreach (var playerId in carPlayerIds)
            {
                var playerInfoComp = self.GetPlayerInfoComponent(playerId);
                var curScore = playerInfoComp.Score;
                
                //积分贡献比
                double scoreRatio = carScore != 0 ? curScore / carScore : 0;

                playerInfoComp.WinFans = self.FansPool * scoreRatio;;
            }

            self.FansPool = 0;
        }
        
        /// <summary>
        /// 旧版本结算
        /// </summary>
        /// <param name="self"></param>
        public static void SettleDataOld(this RoomInfoComponent self)
        {
            //胜者积分计算：第一名阵营60%，第二名阵营30%，第三名阵营10%
            var constConf = TotalConfigManager.ConfigManager.ConstConfigCategory;
            List<float> ratioList = new()
                { constConf.PointFirstGet, constConf.PointSecondGet, constConf.PointThirdGet };
            List<double> scoreList = new();
            var carIds = RoomHelper.GetCars();
            for (int i = 0; i < 3; i++)
            {
                long carId = carIds[i];
                double score = 0;
                foreach (var playerId in EntityManager.Instance.GetEntityById(carId).GetComponent<CarInfoComponent>()
                             .PlayerIds)
                {
                    score += RoomHelper.GetRoomInfoComponent().GetPlayerInfoComponent(playerId).Score;
                }

                scoreList.Add(score);
            }

            for (int i = 0; i < 3; i++)
            {
                long carId = carIds[i];
                var playerIds = EntityManager.Instance.GetEntityById(carId).GetComponent<CarInfoComponent>()
                    .PlayerIds;

                //车队赢得积分*均分比例/人数+车队赢得积分*个人贡献占比
                double allCarScore = self.ScorePool * ratioList[i];
                var pointShareOut = TotalConfigManager.ConfigManager.ConstConfigCategory.PointShareOut;
                //均分积分
                double baseCarScore = playerIds.Count == 0 ? 0 : allCarScore * pointShareOut / playerIds.Count;
                //剩下的积分按贡献值分
                double carScore = allCarScore * (1 - pointShareOut);

                foreach (var playerId in playerIds)
                {
                    var playerInfoComp = RoomHelper.GetRoomInfoComponent().GetPlayerInfoComponent(playerId);

                    //积分贡献比
                    double scoreRatio = 0;

                    if (scoreList[i] == 0)
                    {
                        scoreRatio = 1.0f / playerIds.Count;
                    }
                    else
                    {
                        scoreRatio = playerInfoComp.Score / scoreList[i];
                    }

                    //排行加成
                    var rankData = RoomManager.Instance.GetPlayerRank(RankType.FansRank, playerId);
                    var rankRewardConf =
                        TotalConfigManager.ConfigManager.RankRewardConfigCategory.GetOrDefault(rankData.rankIndex);
                    float rankRatio = rankRewardConf?.FansRankPointAdd ?? 0;

                    playerInfoComp.WinScore = (baseCarScore + carScore * scoreRatio) * (1 + rankRatio);
                }
            }

            //粉丝计算：第一队赢所有，按贡献分
            var carPlayerIds = EntityManager.Instance.GetEntityById(carIds[0])
                .GetComponent<CarInfoComponent>().PlayerIds;

            //在第一车队的玩家返还粉丝数，瓜分其他车队的玩家粉丝
            foreach (var playerId in carPlayerIds)
            {
                var playerInfoComp = self.GetPlayerInfoComponent(playerId);
                var playerItemComp = self.GetPlayerItemComponent(playerId);
                
                var fans = playerItemComp.GetItemNum(GameConst.FansId) + playerInfoComp.LoseFans;
                playerItemComp.SetItemNum(GameConst.FansId, fans);
                
                self.FansPool = Math.Max(0, self.FansPool - playerInfoComp.LoseFans);
            }

            foreach (var playerId in carPlayerIds)
            {
                var playerInfoComp = self.GetPlayerInfoComponent(playerId);

                //积分贡献比
                double scoreRatio = 0;

                if (scoreList[0] == 0)
                {
                    scoreRatio = 1.0f / carPlayerIds.Count;
                }
                else
                {
                    scoreRatio = playerInfoComp.Score / scoreList[0];
                }

                playerInfoComp.WinFans = self.FansPool * scoreRatio;;
            }

            self.FansPool = 0;
        }

        /// <summary>
        /// 获取房间最终时刻
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static int GetGameLastTime(this RoomInfoComponent self)
        {
            var fightRoomConf = TotalConfigManager.ConfigManager.FightRoomConfigCategory.Get(self.RoomID);

            return fightRoomConf.GameTime - fightRoomConf.TimeSceneInfo[^1].TimePlanning;
        }

        /// <summary>
        /// 更新场景信息
        /// </summary>
        /// <param name="self"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static void SetTimeSceneInfo(this RoomInfoComponent self, int index)
        {
            var timeSceneInfo = self.GetFightRoomConfig().TimeSceneInfo;
                
            self.TimeSceneInfoIndex = index;
            self.ScenePlanning = timeSceneInfo[self.TimeSceneInfoIndex];

            int nextIndex = index + 1;
            if (nextIndex >= timeSceneInfo.Count)
            {
                return;
            }

            self.NextTimePlanning = timeSceneInfo[nextIndex].TimePlanning;
        }

        /// <summary>
        /// 获取场景信息
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static Planning GetScenePlanning(this RoomInfoComponent self) => self.ScenePlanning;

        /// <summary>
        /// 获取房间配置
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static FightRoomConfig GetFightRoomConfig(this RoomInfoComponent self) =>
            TotalConfigManager.ConfigManager.FightRoomConfigCategory.GetOrDefault(self.RoomID);
    }
}