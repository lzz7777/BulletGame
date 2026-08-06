using System;
using System.Collections.Generic;
using cfg;
using cfg.Rank;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XN
{
    public static class ViewRankMilesFuelPopSystem
    {
        #region CircleLife

        public static void OnOpenSystem(this ViewRankMilesFuelPop self, UIWindowData uIWindowData)
        {
            self.RefreshMiles().ToCoroutine();
            self.InitFuelUI().ToCoroutine();
        }

        public static void OnCloseSystem(this ViewRankMilesFuelPop self)
        {
        }

        #endregion

        #region UIEvents

        public static void UIBtnCloseButtonOnClick(this ViewRankMilesFuelPop self)
        {
            self.Close();
        }

        public static void UIMaskButtonOnClick(this ViewRankMilesFuelPop self)
        {
            self.Close();
        }

        #endregion

        #region GlobalEvents

        #endregion

        #region Logics

        public static void RefreshTitle(this ViewRankMilesFuelPop self, long startTime)
        {
            DateTime dt = TimeHelper.Time2DateTimeMs(startTime);
            DateTime lastDt = dt.AddMilliseconds(-1);
            Debug.Log($"{dt} -1  ==> {lastDt.Month}月 , {(lastDt.Day <=15 ? "上" : "下")}");
            self.UIMonthTextMeshProUGUI.SetText($"{lastDt.Month}月");
            self.UIDayTextMeshProUGUI.SetText($"{(lastDt.Day <=15 ? "上" : "下")}");
        }
        public static async UniTask RefreshMiles(this ViewRankMilesFuelPop self)
        {
            RankRewardConfigCategory rankRewardConfigCategory = TotalConfigManager.ConfigManager.RankRewardConfigCategory;
            List<(int, string, string, float, string)> list = new();

            // TODO 刷新
            List<RankDataRet> datRankList = await DataManager.GetRankIndexInfo(RankType.Milestone, 0, 99);

            var rankInfo = SceneHelper.GetRankUnit().GetComponent<RankInfoComponent>().RankInfo;
            if (rankInfo.TryGetValue(RankType.Milestone, out RankTimesData timeData))
            {
                var dateTime = TimeHelper.Time2DateTimeMs(timeData.EndTime);
                string formattedTime = dateTime.ToString("yyyy年M月d日HH:mm:ss");
                self.UIEndTimeTextMeshProUGUI.text = $"截榜日期 {formattedTime}";
                
                self.RefreshTitle(timeData.StartTime);
            }
            else
            {
                self.UIEndTimeTextMeshProUGUI.text = $"截榜日期";
            }


            int num = Mathf.Min(datRankList.Count, rankRewardConfigCategory.DataList.Count);
            for (int i = 0; i < num; i++)
            {
                int rankIndex = i + 1;
                var onePlayerData = datRankList[i];
                RankRewardConfig rankRewardConfig = rankRewardConfigCategory.GetOrDefault(rankIndex);
                list.Add((
                    onePlayerData.Rank, 
                    ResHelper.GetAvatarUrl(onePlayerData.AvatarUrl),
                    onePlayerData.Nickname,
                    Convert.ToSingle(onePlayerData.Score),
                    rankRewardConfig.MilestoneShow
                ));
            }

            if (list.Count > 0)
            {
                self.UITopScoreTextMeshProUGUI.text = $"{Convert.ToSingle(list[0].Item4).ToString("F0")}米";
                self.UITopScoreTextMeshProUGUI.outlineColor = new Color(116 / 255f, 59 / 255f, 121 / 255f);

                self.UITopDescTextMeshProUGUI.text = list[0].Item3;
            }
            else
            {
                self.UITopScoreTextMeshProUGUI.text = "";
                self.UITopDescTextMeshProUGUI.text = "";
            }

            ObjectPoolManager.Instance.ReturnToPool(self.UIItemList);
            self.UIItemList.Clear();
            foreach (var item in list)
            {
                var obj = await ObjectPoolManager.Instance.GetFromPool<ItemMiles>(self.UIMilesContentVerticalLayoutGroup.transform);
                self.UIItemList.Add(obj);
                var itemData = new ItemMilesData()
                {
                    rankIndex = item.Item1,
                    headIcon = item.Item2,
                    name = item.Item3,
                    score = item.Item4,
                    RwdSkin = item.Item5,
                };
                obj.GetComponent<ItemMiles>().OnRefresh(itemData);
            }
        }

        public static async UniTask InitFuelUI(this ViewRankMilesFuelPop self)
        {
            // 这个是不变的
            if (self.FuelItemList.Count > 0) return;

            SignRewardConfigCategory signRewardConfigCategory = TotalConfigManager.ConfigManager.SignRewardConfigCategory;
            foreach (var d in signRewardConfigCategory.DataList)
            {
                var obj = await ObjectPoolManager.Instance.GetFromPool<ItemFuel>(self.UIFuelContentVerticalLayoutGroup.transform);
                self.FuelItemList.Add(obj);
                var itemData = new ItemFuelData()
                {
                    config = d
                };
                obj.GetComponent<ItemFuel>().OnRefresh(itemData);
            }
        }

        #endregion
    }
}