using System;
using System.Collections.Generic;
using System.Linq;
using cfg;
using cfg.Rank;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XN
{
    public static class ViewRankLastSeasonSystem
    {
        #region CircleLife

        public static void OnOpenSystem(this ViewRankLastSeason self, UIWindowData uIWindowData)
        {
            self.RefreshScollToggle();
        }

        public static void OnCloseSystem(this ViewRankLastSeason self)
        {
        }

        #endregion

        #region UIEvents

        //
        // public static void UIBtnCloseButtonOnClick(this ViewRankLastSeason self)
        // {
        //  self.Close();
        // }
        //
        public static void UIBgButtonOnClick(this ViewRankLastSeason self)
        {
            self.Close();
        }

        #endregion

        #region GlobalEvents

        #endregion

        #region Logics

        public static void RefreshScollToggle(this ViewRankLastSeason self)
        {
            // 兼容处理当前分页签 按钮栏
            self.RefreshToggleBar();

            // 模拟切页签 Toggle 显示选中 ToggleDetail
            // self.UIRankToggleOnChanged(self.currToggle, true);
        }

        public static void RefreshToggleBar(this ViewRankLastSeason self)
        {
            // 上周， 上月，上里程榜
            RankInfoConfigCategory rankInfoCc = TotalConfigManager.ConfigManager.RankInfoConfigCategory;
            var pWeek = rankInfoCc.GetOrDefault(RankType.PreviousWeekRank);
            self.UIBtnName1TextMeshProUGUI.SetText(pWeek.RankName);
            var pMonth = rankInfoCc.GetOrDefault(RankType.PreviousMonthRank);
            self.UIBtnName2TextMeshProUGUI.SetText(pMonth.RankName);
            var pMile = rankInfoCc.GetOrDefault(RankType.PreviousMilestone);
            self.UIBtnName3TextMeshProUGUI.SetText(pMile.RankName);

            self.UIToggleLastWeekToggle.isOn = true;
        }

        public static void ToggleDetail(this ViewRankLastSeason self, List<string> GradeName)
        {
            // oneRankInfo.GradeName[0],oneRankInfo.GradeName[1],oneRankInfo.GradeName[2],oneRankInfo.GradeName[3],oneRankInfo.GradeName[4]
            if (GradeName.Count < 5)
            {
                Debug.LogError(" 配置不足 五个命名Title :" + GradeName.Count);
                return;
            }

            self.UIDataIndexTextMeshProUGUI.SetText(GradeName[0]);
            self.UIDataNameTextMeshProUGUI.SetText(GradeName[1]);
            self.UIDataScoreTextMeshProUGUI.SetText(GradeName[2]);
            self.UIDataFansTextMeshProUGUI.SetText(GradeName[3]);
            self.UIDataFansTextMeshProUGUI.SetText(GradeName[4]);
        }

        public static void UIRankToggleOnChanged(this ViewRankLastSeason self, RankType rankType, bool isOn)
        {
            Debug.Log($" toggle rankType: {rankType} / {isOn}");
            if (!isOn) return;
            RankInfoConfigCategory rankInfoCC = TotalConfigManager.ConfigManager.RankInfoConfigCategory;
            RankInfoConfig oneRankInfo = rankInfoCC.Get(rankType);
            self.ToggleDetail(oneRankInfo.GradeName);

            switch (rankType)
            {
                case RankType.PreviousWeekRank:
                case RankType.PreviousMonthRank:
                case RankType.PreviousMilestone:
                    var rankInfo = SceneHelper.GetRankUnit().GetComponent<RankInfoComponent>().RankInfo;
                    if (rankInfo.TryGetValue(rankType, out RankTimesData timeData))
                    {
                        var startDataTime = TimeHelper.Time2DateTimeMs(timeData.StartTime);
                        string startTimeStr = startDataTime.ToString("M月d日HH:mm:ss");
                        
                        var endDataTime = TimeHelper.Time2DateTimeMs(timeData.EndTime);
                        string endTimeStr = endDataTime.ToString("M月d日HH:mm:ss");

                        // ver1. 固定天  7,14,30
                        // var skinEndTime = timeData.EndTime + RankHelper.GetRefreshDay(oneRankInfo.Refresh) * TimeHelper.OneDayTimestampMS;
                        // var skinEndDataTime = TimeHelper.Time2DateTimeMs(skinEndTime);
                        // ver2. 自然周期   7，半月（15号，或月底）， 自然月
                        var skinEndTime = RankHelper.GetNextRefreshDay(timeData.EndTime, oneRankInfo.Refresh);
                        string skinEndTimeStr = skinEndTime.ToString("M月d日HH:mm:ss");
                        
                        self.UIRankSeasonTipsTextMeshProUGUI.SetText($"榜单结算周期 {startTimeStr}-{endTimeStr}");
                        self.UISkinSeasonTipsTextMeshProUGUI.SetText($"皮肤生效周期 {endTimeStr}-{skinEndTimeStr}");
                    }
                    else
                    {
                        self.UIRankSeasonTipsTextMeshProUGUI.SetText("榜单结算周期");
                        self.UISkinSeasonTipsTextMeshProUGUI.SetText("皮肤生效周期");
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rankType), rankType, null);
            }

            // 兼容分页签下拉取Player List
            self.currToggle = rankType;
            self.RefreshScrollPlayerItems(self.currToggle).ToCoroutine();
        }

        public static async UniTask RefreshScrollPlayerItems(this ViewRankLastSeason self, RankType currRankType)
        {
            var rankRewardCc = TotalConfigManager.ConfigManager.RankRewardConfigCategory;
            List<RankDataRet> datRankList = await DataManager.GetRankIndexInfo(self.currToggle, 0, 99);
            int num = datRankList.Count; // 配置给到多少个
            Transform nPlayerParent = self.UIScrollContentVerticalLayoutGroup.transform;

            ObjectPoolManager.Instance.ReturnToPool(self.RankListItems);
            self.RankListItems.Clear();

            Debug.Log($"{self.currToggle} , ");
            for (int i = 0; i < num; i++)
            {
                int Index = i + 1;
                // TODO 拉取排行榜玩家数据
                RankDataRet onePlayerData = datRankList?.Find(x => x.Rank == Index);
                var obj = await ObjectPoolManager.Instance.GetFromPool<ItemRankOnePlayer>(nPlayerParent);
                self.RankListItems.Add(obj);
                var itemData = new ItemRankOnePlayerData()
                {
                    RankType = currRankType,
                    PlayerId = onePlayerData.PlayerId,
                    Name = onePlayerData.Nickname,
                    AvatarUrl = ResHelper.GetAvatarUrl(onePlayerData.AvatarUrl),
                    Index = Index,
                };

                itemData.OwnScore = Convert.ToSingle(onePlayerData.Score);

                RankRewardConfig oneRwd = rankRewardCc.GetOrDefault(Index);
                // 上月榜单等待差异
                switch (currRankType)
                {
                    case RankType.PreviousWeekRank:
                        itemData.RewardsShow = oneRwd.PreviousWeekRankShow;
                        itemData.FansItemGroup = oneRwd.PreviousWeekRankReward.FirstOrDefault(
                            x => x.ItemId == 2 // 粉丝 一定为 货币2
                        );
                        break;
                    case RankType.PreviousMonthRank:
                        itemData.RewardsShow = oneRwd.PreviousMonthRankShow;
                        itemData.FansItemGroup = oneRwd.PreviousMonthRankReward.FirstOrDefault(
                            x => x.ItemId == 2
                        );
                        break;
                    case RankType.PreviousMilestone:
                        itemData.RewardsShow = oneRwd.PreviousMilestoneShow;
                        itemData.FansItemGroup = oneRwd.PreviousMilestoneReward.FirstOrDefault(
                            x => x.ItemId == 2
                        );
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(currRankType), currRankType, null);
                }

                obj.GetComponent<ItemRankOnePlayer>().OnRefresh(itemData);
            }
        }

        #endregion
    }
}