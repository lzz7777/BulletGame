using System;
using System.Collections.Generic;
using System.Linq;
using cfg;

namespace XN
{
    public static class SceneHelper
    {
        public static Entity Scene() => EntityManager.Instance.GetEntityById(Main.SceneUnitId);

        public static SceneInfoComponent GetSceneInfoComponent() => Scene().GetComponent<SceneInfoComponent>();
        
        public static Entity GetRankUnit()
        {
            foreach (var child in Scene().GetChildren())
            {
                if (child.HasComponent<RankInfoComponent>())
                {
                    return child;
                }
            }

            return null;
        }

        public static Entity GetTimeUnit()
        {
            foreach (var child in Scene().GetChildren())
            {
                if (child.HasComponent<TimeInfoComponent>())
                {
                    return child;
                }
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rankType"> RankType + 粉丝勋章 </param>
        /// <param name="playerId"></param>
        public static void SetPlayerHadJoin(string rankType, string playerId)
        {
            return;
            var ranUnit = GetRankUnit();
            RankInfoComponent rankInfoComp = ranUnit.GetComponent<RankInfoComponent>();
            if (!rankInfoComp.RankTopPlayerShowDic.TryGetValue(rankType, out var topPlayerIds))
            {
                topPlayerIds = new List<string>();
                rankInfoComp.RankTopPlayerShowDic.TryAdd(rankType, topPlayerIds);
            }

            if (topPlayerIds.Contains(playerId))
            {
                Debug.LogError($"??? 啥子情况{rankType} - {playerId} Top100 本次直播游戏已播入场动画，怎么还能播放？");
                return;
            }

            topPlayerIds.Add(playerId);
        }

        public static bool GetPlayerFirstJion(string rankType, string playerId)
        {
            Debug.Log($"{playerId} SettingTop100Anim:{SaveData.GetInt(SaveData.Key.SettingTop100Anim)}");
            if (SaveData.GetInt(SaveData.Key.SettingTop100Anim, 0) == 1)
            {
                Debug.Log("SettingTop100Anim 百强动画入场设置 为关闭");
                return false;
            }
            
            return true;

            var ranUnit = GetRankUnit();
            RankInfoComponent rankInfoComp = ranUnit.GetComponent<RankInfoComponent>();
            if (!rankInfoComp.RankTopPlayerShowDic.TryGetValue(rankType, out List<string> rankPlayerIds))
            {
                return true; // 无这个榜单Top加入过
            }

            return !rankPlayerIds.Contains(playerId); // 这个榜单无这个TopPlayerId加入过
        }

        public static string GetVideoRes(RankType rankType, int index, SexType sex = SexType.Male)
        {
            if (index <= 0)
            {
                return string.Empty; // 不在榜单
            }

            var videoShowCc = TotalConfigManager.ConfigManager.VideoShowConfigCategory;
            var item = videoShowCc.DataList.FirstOrDefault(x =>
                x.RankId == rankType && x.RankNumber[0] <= index && index <= x.RankNumber[1]);
            string videoRes = item?.VideoRes ?? string.Empty;

            if (!string.IsNullOrEmpty(videoRes) && sex == SexType.Female)
            {
                videoRes += "_1";
            }

            return videoRes;
        }

        public static string GetFansBadgeRes(int index)
        {
            string res = String.Empty; //ResHelper.GetIconOrNone();    // 不在榜单
            var badgeCc = TotalConfigManager.ConfigManager.BadgeInfoConfigCategory;

            foreach (var item in badgeCc.DataList)
            {
                if (index >= item.FansNumbers)
                {
                    res = item.BadgeImage;
                }
                else if (index < item.FansNumbers)
                {
                    break;
                }
            }

            return res;
        }

        /// <summary>
        /// 获取场景层级信息
        /// </summary>
        /// <returns></returns>
        public static LayerInfo GetLayerInfo(SceneLayerType sceneLayerType)
        {
            int sceneId = GetSceneInfoComponent().SceneId;
            int sceneChildId =  RoomHelper.GetRoomInfoComponent().ScenePlanning.FightScene;
            var conf = TotalConfigManager.ConfigManager.SceneInfoConfigCategory.Get(sceneChildId, sceneId);
            LayerInfo layerInfo = null;

            switch (sceneLayerType)
            {
                case SceneLayerType.Bg:
                    layerInfo = conf.LayerBG;
                    break;
                case SceneLayerType.Layer0:
                    layerInfo = conf.Layer0;
                    break;
                case SceneLayerType.Layer1:
                    layerInfo = conf.Layer1;
                    break;
                case SceneLayerType.Layer2:
                    layerInfo = conf.Layer2;
                    break;
                case SceneLayerType.Layer3:
                    layerInfo = conf.Layer3;
                    break;
                case SceneLayerType.Layer4:
                    layerInfo = conf.Layer4;
                    break;
                case SceneLayerType.Layer5:
                    layerInfo = conf.Layer5;
                    break;
                case SceneLayerType.Layer6:
                    layerInfo = conf.Layer6;
                    break;
                case SceneLayerType.RoadLine1:
                    layerInfo = conf.RoadLine1;
                    break;
                case SceneLayerType.RoadLine2:
                    layerInfo = conf.RoadLine2;
                    break;
                case SceneLayerType.RoadLine3:
                    layerInfo = conf.RoadLine3;
                    break;
            }

            return layerInfo;
        }
        
        public static string GetLayerInfoRandomResName(LayerInfo layerInfo)
        {
            int weightSum = 0;
            string resName = layerInfo.LayerWeight[0].LayerRes;

            foreach (var layerWeight in layerInfo.LayerWeight)
            {
                weightSum += layerWeight.Weight;
            }

            int randomWeight = UnityEngine.Random.Range(1, weightSum + 1);

            int tempSum = 0;
            foreach (var layerWeight in layerInfo.LayerWeight)
            {
                tempSum += layerWeight.Weight;
                if (tempSum >= randomWeight)
                {
                    resName = layerWeight.LayerRes;
                    break;
                }
            }

            return resName;
        }
    }
}