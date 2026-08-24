using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XN
{
    public class ViewMatchRankItem : UIItemBase<ViewMatchRankItemData>
    {
        public UILoopListItem ViewMatchRankItemLoopListItem;
        public TextMeshProUGUI UITmpRankIndexTMP_UGUI;
        public Text UITmpNameText;
        public RectTransform UILayoutRectTrans;

        #region CustomFields

        public List<GameObject> Objs = new();

        private void Awake()
        {
            this.OnAwakeSystem();
        }

        public override void Refresh(ViewMatchRankItemData data)
        {
            // 没有任何强转，直接调 System
            this.OnRefresh(data);
        }

        #endregion
    }
}