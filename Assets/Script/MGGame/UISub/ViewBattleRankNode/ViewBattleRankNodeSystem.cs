using System.Globalization;
using cfg;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XN
{
    public static class ViewBattleRankNodeSystem
    {
        #region CircleLife

        public static void OnOpenSystem(this ViewBattleRankNode self, UIWindowData uIWindowData = null)
        {
            (self.transform as RectTransform).anchoredPosition = uIWindowData.Pos;
            
            self.OnRefresh();
        }

        public static void OnCloseSystem(this ViewBattleRankNode self)
        {
            ObjectPoolManager.Instance.ReturnToPool(self.Items);
            self.Items.Clear();
        }

        #endregion

        #region UIEvents

        #endregion

        #region GlobalEvents

        #endregion

        #region Logics

        public static async UniTask OnRefresh(this ViewBattleRankNode self)
        {
            var dataTime = TimeHelper.GetTimeStampDataTime();

            string monthName = dataTime.ToString("MMMM", new CultureInfo("zh-CN"));
            self.UIMonthTMP_UGUI.text = $"[{monthName}]";

            var datRankList = await DataManager.GetRankIndexInfo(RankType.MonthRank, 0, 4);
            for (int i = 0; i < datRankList.Count; i++)
            {
                var rankDataRet = datRankList[i];
                var obj = await ObjectPoolManager.Instance.GetFromPool<ViewBattleRankItem>(self.UIItemNodeVerticalLayoutGroup.transform);
                self.Items.Add(obj);

                obj.GetComponent<ViewBattleRankItem>().OnRefresh(rankDataRet);
            }
        }

        #endregion
    }
}