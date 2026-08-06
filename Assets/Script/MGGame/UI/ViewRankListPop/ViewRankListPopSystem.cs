using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace XN
{
    public static class ViewRankListPopSystem
    {
        #region CircleLife

        public static void OnOpenSystem(this ViewRankListPop self, UIWindowData uIWindowData = null)
        {
            var listType = uIWindowData?.StringArgs1;

            if (string.IsNullOrEmpty(listType))
                return;

            var rankNode = self.NodeRankRectTrans.GetComponent<RectTransform>();
            var skinNode = self.NodeSkinRectTrans.GetComponent<RectTransform>();
            var nodeRect = self.NodeRectTrans.GetComponent<RectTransform>();

            rankNode.gameObject.SetActive(listType == "Rank");
            skinNode.gameObject.SetActive(listType == "Skin");

            switch (listType)
            {
                case "Rank":
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rankNode);
                    nodeRect.sizeDelta = new Vector2(nodeRect.sizeDelta.x, rankNode.sizeDelta.y + self.Offset);
                    break;
                case "Skin":
                    LayoutRebuilder.ForceRebuildLayoutImmediate(skinNode);
                    nodeRect.sizeDelta = new Vector2(nodeRect.sizeDelta.x, skinNode.sizeDelta.y + self.Offset);
                    break;
            }
        }

        public static void OnCloseSystem(this ViewRankListPop self)
        {
        }

        #endregion

        #region UIEvents

        public static void UICloseButtonOnClick(this ViewRankListPop self)
        {
            self.gameObject.SetActive(false);
        }

        public static void UIMrtButtonOnClick(this ViewRankListPop self)
        {
            UIManager.Instance.OpenWindow<ViewFamousRankMain>();
        }

        public static void UIZlcbButtonOnClick(this ViewRankListPop self)
        {
            UIManager.Instance.OpenWindow<ViewRankMilesFuelPop>();
        }

        public static void UIDbpfButtonOnClick(this ViewRankListPop self)
        {
            UIManager.Instance.OpenWindow<ViewRankLastSeason>().ToCoroutine();
        }

        public static void UIPhbButtonOnClick(this ViewRankListPop self)
        {
            UIManager.Instance.OpenWindow<ViewWorldRankMain>(new UIWindowData() { StringArgs1 = "World2Rank", })
                .ToCoroutine();
        }

        #region 皮肤

        /// <summary>
        /// 周榜皮肤
        /// </summary>
        /// <param name="self"></param>
        public static void UIZbpfButtonOnClick(this ViewRankListPop self)
        {
            UIManager.Instance.OpenWindow<ViewRankWeekPop>().ToCoroutine();
        }

        /// <summary>
        /// 兑换皮肤
        /// </summary>
        /// <param name="self"></param>
        public static void UIDhpfButtonOnClick(this ViewRankListPop self)
        {
            UIManager.Instance.OpenWindow<ViewRedeemPop>().ToCoroutine();
        }

        #endregion

        #endregion

        #region GlobalEvents

        #endregion

        #region Logics

        #endregion
    }
}