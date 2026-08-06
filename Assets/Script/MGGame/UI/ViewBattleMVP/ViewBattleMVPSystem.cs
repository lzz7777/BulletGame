using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace XN
{
    public static class ViewBattleMVPSystem
    {
        #region CircleLife

        public static void OnOpenSystem(this ViewBattleMVP self, UIWindowData uIWindowData)
        {
            // SoundManager.Instance.PlayMusic(MGGameState.未进入游戏);
            self.RefreshAll();
        }

        public static void OnCloseSystem(this ViewBattleMVP self)
        {
        }

        #endregion

        #region UIEvents

        public static void UIBgButtonOnClick(this ViewBattleMVP self)
        {
            self.Close();
            if (self.Mvp1Item != null)
            {
                ObjectPoolManager.Instance.ReturnToPool(self.Mvp1Item);
                self.Mvp1Item = null;
            }

            if (self.Mvp2Items.Count > 0)
            {
                ObjectPoolManager.Instance.ReturnToPool(self.Mvp2Items);
                self.Mvp2Items.Clear();
            }
            UIManager.Instance.OpenWindow<ViewWorldRankMain>(new UIWindowData() { StringArgs1 = "Room2Rank", }).ToCoroutine();
        }

        #endregion

        #region GlobalEvents

        #endregion

        #region Logics

        public static async UniTask RefreshAll(this ViewBattleMVP self)
        {
            // var roomPlayersMap = RoomHelper.GetPlayers();
            // string[] playerIDsold = RoomHelper.GetPlayers().Keys.ToArray();
            string[] playerIDs = RoomHelper.GetPlayerIdsInCar();
            // Debug.Log($"{playerIDsold.Length} ===> {playerIDs.Length}");

            List<RankDataRet> DatRankList = await DataManager.GetRankIndexInfo(cfg.RankType.WeekRank, playerIDs);	// 服务器 周榜排名

            // 车队Top3
            List<long> carIds = RoomHelper.GetCars();
            List<long> carTop3 = carIds
                .OrderByDescending(carId => EntityManager.Instance.GetEntityById(carId).GetComponent<CarInfoComponent>().Mileage)
                .Take(3)
                .ToList();
            // 车队Top1 的 Mvp、最佳副驾之类的
            List<string> top1CarPlayerIds = EntityManager.Instance.GetEntityById(carTop3[0]).GetComponent<CarInfoComponent>().PlayerIds;
            List<string> MvpTop3 = top1CarPlayerIds
                // .OrderByDescending(playerId => EntityManager.Instance.GetEntityById(roomPlayersMap[playerId]).GetComponent<PlayerInfoComponent>().Score)
                .OrderByDescending(playerId => RoomHelper.GetRoomInfoComponent().GetPlayerInfoComponent(playerId).Score)
                .Take(3)
                .ToList();
            
            // MVP.1
            self.UIIMVPtem1RectTransform.parent.gameObject.SetActive(MvpTop3.Count >= 1);
            if (MvpTop3.Count >= 1)
            {
                ObjectPoolManager.Instance.ReturnToPool(self.Mvp1Item);
                self.Mvp1Item = null;
                var obj = await ObjectPoolManager.Instance.GetFromPool<ViewBattleMVPPlayerItem>(self.UIIMVPtem1RectTransform);
                self.Mvp1Item = obj;
                // var onePlayerData = DatRankList.Find(x => x.PlayerId == MvpTop3[0]);
                // long playerInstId = roomPlayersMap[MvpTop3[0]];
                // var onePlayerUnit = EntityManager.Instance.GetEntityById(playerInstId);
                var onePlayerUnit = RoomHelper.GetRoomInfoComponent().GetPlayerUnit(MvpTop3[0]);
                if (onePlayerUnit != null)
                {
                    var onePlayerInfo = onePlayerUnit.GetComponent<PlayerInfoComponent>();
                    var onePlayerItem = onePlayerUnit.GetComponent<PlayerItemComponent>();
                    obj.GetComponent<ViewBattleMVPPlayerItem>().OnRefresh(new ViewBattleMVPPlayerItemData()
                    {
                        Name = onePlayerInfo.Name,
                        AvatarUrl = onePlayerInfo.AvatarUrl,
                        Score = onePlayerItem.GetItemNum(GameConst.ScoreId) + onePlayerInfo.WinScore,
                        ScoreAdd = onePlayerInfo.WinScore,
                        Fans = onePlayerItem.GetItemNum(GameConst.FansId) + onePlayerInfo.WinFans,
                        FansAdd = onePlayerInfo.WinFans,
                        FansIsMin = onePlayerInfo.IsBaoDiFans
                    });
                }
            }

            // MVP.2~3 (最佳帮手）
            self.UIIMVPtem2RectTransform.parent.gameObject.SetActive(MvpTop3.Count >= 2);
            if (MvpTop3.Count >= 2)
            {
                ObjectPoolManager.Instance.ReturnToPool(self.Mvp2Items);
                self.Mvp2Items.Clear();
                for (int i = 1; i < MvpTop3.Count; i++)
                {
                    Transform parent = i == 1 ? self.UIIMVPtem2RectTransform : self.UIIMVPtem3RectTransform;
                    var obj = await ObjectPoolManager.Instance.GetFromPool<ViewBattleMVPPlayerItem>(parent);
                    self.Mvp2Items.Add(obj);
                    
                    // var onePlayerData = DatRankList.Find(x => x.PlayerId == MvpTop3[i]);
                    // long playerInstId = roomPlayersMap[MvpTop3[i]];
                    // var onePlayerUnit = EntityManager.Instance.GetEntityById(playerInstId);
                    var onePlayerUnit = RoomHelper.GetRoomInfoComponent().GetPlayerUnit(MvpTop3[i]);
                    if (onePlayerUnit != null)
                    {
                        var onePlayerInfo = onePlayerUnit.GetComponent<PlayerInfoComponent>();
                        var onePlayerItem = onePlayerUnit.GetComponent<PlayerItemComponent>();
                        obj.GetComponent<ViewBattleMVPPlayerItem>().OnRefresh(new ViewBattleMVPPlayerItemData()
                        {
                            Name = onePlayerInfo.Name,
                            AvatarUrl = onePlayerInfo.AvatarUrl,
                            Score = onePlayerItem.GetItemNum(GameConst.ScoreId) + onePlayerInfo.WinScore,
                            ScoreAdd = onePlayerInfo.WinScore,
                            Fans = onePlayerItem.GetItemNum(GameConst.FansId) + onePlayerInfo.WinFans,
                            FansAdd = onePlayerInfo.WinFans,
                            FansIsMin = onePlayerInfo.IsBaoDiFans
                        });
                    }
                }
            }

            // Team
            var Team1Comp = EntityManager.Instance.GetEntityById(carTop3[0]).GetComponent<CarInfoComponent>();
            self.UIChampionNameTextMeshProUGUI.text = $"<color=#821b03>{Team1Comp.Name}</color>";
            for (int i = 0; i < carTop3.Count; i++)
            {
                long carId = carTop3[i];
                var carInfoComp = EntityManager.Instance.GetEntityById(carId).GetComponent<CarInfoComponent>();
                TextMeshProUGUI UITeamTMP = self.UITeam1TextMeshProUGUI;
                String indexDesc = "";
                switch (i)
                {
                    case 0:
                        UITeamTMP = self.UITeam1TextMeshProUGUI;
                        indexDesc = $"<color=#ffd800><size=48>1</size><size=36>ST {carInfoComp.Name}</size></color> ";
                        break;
                    case 1:
                        UITeamTMP = self.UITeam2TextMeshProUGUI;
                        indexDesc = $"<color=#ff8c05><size=48>2</size><size=36>ND {carInfoComp.Name}</size></color>";
                        break;
                    case 2:
                        UITeamTMP = self.UITeam3TextMeshProUGUI;
                        indexDesc = $"<color=#5dc176><size=48>3</size><size=36>RD {carInfoComp.Name}</size></color>";
                        break;
                    default:
                        Debug.LogError("Error Team index : "+ i);
                        break;
                }

                indexDesc += $"\n<size=30>{UIManagerHelper.UIMathCeil(carInfoComp.Mileage)}米</size>";
                UITeamTMP.text = indexDesc;
            }
            
            // 本局全体
            // 全部参与者
            List<string> roomPlayerIds = playerIDs
                .OrderByDescending(playerId =>
                {
                    // var comp = EntityManager.Instance.GetEntityById(kv.Value).GetComponent<PlayerInfoComponent>();
                    var comp = RoomHelper.GetRoomInfoComponent().GetPlayerInfoComponent(playerId);
                    return comp?.Mileage ?? 0f;
                })
                .ToList();
            
            List<RankDataRet> mileDatRankList = await DataManager.GetRankIndexInfo(cfg.RankType.Milestone, playerIDs);	// 服务器 周榜排名

            ObjectPoolManager.Instance.ReturnToPool(self.PlayerItems);
            self.PlayerItems.Clear();
            Transform playerParent = self.UIPlayerContentVerticalLayoutGroup.transform;
            for (int i = 0; i < roomPlayerIds.Count; i++)
            {
                int Index = i + 1;
                var onePlayerData = mileDatRankList.Find(x => x.PlayerId == roomPlayerIds[i]);
                // long playerInstId = roomPlayersMap[onePlayerData.PlayerId];
                // var onePlayerUnit = EntityManager.Instance.GetEntityById(playerInstId);
                var onePlayerUnit = RoomHelper.GetRoomInfoComponent().GetPlayerUnit(onePlayerData.PlayerId);
                if (onePlayerUnit != null)
                {
                    var onePlayerInfo = onePlayerUnit.GetComponent<PlayerInfoComponent>();
                    var onePlayerItem = onePlayerUnit.GetComponent<PlayerItemComponent>();
                    var obj = await ObjectPoolManager.Instance.GetFromPool<ViewBattleMVPIndexItem>(playerParent);
                    self.PlayerItems.Add(obj);
                    obj.GetComponent<ViewBattleMVPIndexItem>().OnRefresh(new ViewBattleMVPIndexItemData()
                    {
                        RoomIndex = Index,
                        PlayerId = onePlayerInfo.PlayerId,
                        Name = onePlayerInfo.Name,
                        AvatarUrl = onePlayerInfo.AvatarUrl,
                        Mile = onePlayerItem.GetItemNum(GameConst.Mileage) + onePlayerInfo.Mileage,
                        MileAdd = onePlayerInfo.Mileage,// 本局增加的里程
                        RankNode = RoomManager.Instance.GetPlayerRank(cfg.RankType.Milestone, onePlayerData.PlayerId),// 上次里程榜单
                        RankIndex = onePlayerData.Rank, // 本次里程排名
                    });
                }
            }
        }

        #endregion
    }
}