//====================================================
//Author:lixin
//Time  :2025/11/24 11:36
//Desc  :
//====================================================

using System;
using System.Collections.Generic;
using System.Linq;
using BestHTTP.JSON;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

namespace XN
{
    #region 同步后端数据结构
        
    public class CombatRequest
    {
        /// <summary>
        /// 战斗请求玩家的openid
        /// </summary>
        public string OpenId { get; set; }
        /// <summary>
        /// 战斗请求玩家的房间id
        /// </summary>
        public long RoomId { get; set; }
        
        /// <summary>
        /// 游戏名称
        /// </summary>
        public string GameName {get;set; }
        
        /// <summary>
        /// 战斗结果数据
        /// </summary>
        public List<CombatResultData> CombatResultDataList { get; set; } = new List<CombatResultData>();
    }

    /// <summary>
    /// 战斗结果数据
    /// </summary>
    public class CombatResultData
    {
        public string PlayerId { get; set; }
        /// <summary>
        /// 战斗结果玩家的昵称
        /// </summary>
        public string Nickname { get; set; }
        /// <summary>
        /// 战斗结果玩家的头像
        /// </summary>
        public string AvatarUrl { get; set; }
        /// <summary>
        /// 战斗结果玩家的奖励数据
        /// </summary>
        public List<BagData> RewardDataList { get; set; } = new ();
        /// <summary>
        /// 结算时间
        /// </summary>
        public long ResultTime { get; set; }
        /// <summary>
        /// 索引
        /// </summary>
        public int Index { get; set; }
    }
    
    #endregion

    public static partial class DataManager
    {
        public static async UniTask<string> SendRoomData(Action<string> callback)
        {
            List<CombatResultData> CombatResultDataList = new();
            var scenInfoComp = SceneHelper.GetSceneInfoComponent();
            // TODO 完善sdk参数上报
            var param = new Dictionary<string, object>
            {
                { "GameName", TotalConfigManager.ConfigManager.ConstConfigCategory.GameName },
                { "RoomId", scenInfoComp.RoomId },          // 房间Id - 会变化新建开播
                { "OpenId", scenInfoComp.AnchorOpenId },    // 主播Id
                {"CombatResultDataList",CombatResultDataList}
            };

            //根据玩家积分，贡献里程时间排序
            List<long> playerLongIds = RoomHelper.GetPlayers().Values.ToList();
            playerLongIds.Sort((a, b) =>
            {
                var aPlayerUnit = EntityManager.Instance.GetEntityById(a);
                var aPlayerInfoComp =  aPlayerUnit.GetComponent<PlayerInfoComponent>();
                var aPlayerItemComp = aPlayerUnit.GetComponent<PlayerItemComponent>();
                
                var bPlayerUnit = EntityManager.Instance.GetEntityById(b);
                var bPlayerInfoComp =  bPlayerUnit.GetComponent<PlayerInfoComponent>();
                var bPlayerItemComp = bPlayerUnit.GetComponent<PlayerItemComponent>();
            
                double aScore = aPlayerItemComp.GetFinalScoreNum();
                double bScore = bPlayerItemComp.GetFinalScoreNum();
            
                if (!UIManagerHelper.IsEqual(aScore, bScore))
                {
                    return bScore.CompareTo(aScore);
                }
            
                return aPlayerInfoComp.ScoreTime.CompareTo(bPlayerInfoComp.ScoreTime);
            });

            int sortIndex = 0;
            var minimumFortune = TotalConfigManager.ConfigManager.ConstConfigCategory.MinimumFortune;
            for (int i = 0; i < playerLongIds.Count; i++)
            {
                long playerLongId = playerLongIds[i];
                
                var playerUnit = EntityManager.Instance.GetEntityById(playerLongId);
                var playerInfoComp =  playerUnit.GetComponent<PlayerInfoComponent>();
                var playerItemComp = playerUnit.GetComponent<PlayerItemComponent>();
                
                var SendItemDataList = new List<BagData>();
                // 1	积分 2	粉丝 3	油桶 4	里程 5	星星

                double score = playerItemComp.GetFinalScoreNum();
                // 积分
                SendItemDataList.Add(new BagData()
                {
                    ItemId = GameConst.ScoreId,
                    ItemNum = score,
                });
                
                // 粉丝
                double finalFansNum = playerItemComp.GetFinalFansNum();
                if (finalFansNum < minimumFortune)
                {
                    //触发保底
                    finalFansNum = minimumFortune;
                    playerInfoComp.IsBaoDiFans = true;
                    playerItemComp.SetItemNum(GameConst.FansId, finalFansNum);
                    playerInfoComp.WinFans = 0;
                }
                SendItemDataList.Add(new BagData()
                {
                    ItemId = GameConst.FansId,
                    ItemNum = finalFansNum,
                });
                
                // 里程
                SendItemDataList.Add(new BagData()
                {
                    ItemId = GameConst.Mileage,
                    ItemNum = playerItemComp.GetItemNum(GameConst.Mileage) + playerInfoComp.Mileage,
                });
                // 月榜积分
                SendItemDataList.Add(new BagData()
                {
                    ItemId = GameConst.MonthScore,
                    ItemNum = playerItemComp.GetItemNum(GameConst.MonthScore) + playerInfoComp.WinScore,
                });

                //秒榜
                SendItemDataList.Add(new BagData()
                {
                    ItemId = GameConst.KillCount,
                    ItemNum = playerItemComp.GetItemNum(GameConst.KillCount),
                });
                
                if (i == playerLongIds.Count - 1)
                {
                    if (sortIndex != 0)
                    {
                        sortIndex++;
                    }
                }
                else
                {
                    var nextScore = EntityManager.Instance.GetEntityById(playerLongIds[i + 1])
                        .GetComponent<PlayerItemComponent>().GetFinalScoreNum();

                    if (UIManagerHelper.IsEqual(nextScore, score))
                    {
                        sortIndex++;
                    }
                    else
                    {
                        if (sortIndex != 0)
                        {
                            sortIndex++;
                        }
                    }
                }
                
                // 背包东西
                CombatResultData oneData = new CombatResultData()
                {
                    PlayerId = playerInfoComp.PlayerId,
                    Nickname = playerInfoComp.Name,
                    AvatarUrl = playerInfoComp.AvatarUrl,
                    RewardDataList = SendItemDataList,
                    Index = sortIndex,
                };
                CombatResultDataList.Add(oneData);
            }

            var resp = await AsyncSendPost(GameConst.Url.Post_BattleResult, body: param);
            string cbStr = JsonUtility.ToJson(param);
            callback?.Invoke(cbStr);
            return resp;
        }
    }
}