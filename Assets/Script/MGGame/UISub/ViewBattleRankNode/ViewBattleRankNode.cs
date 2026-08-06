using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XN
{
    public class ViewBattleRankNode : UISubViewBase
    {
		public UISubView ViewBattleRankNodeSubView;
		public TextMeshProUGUI UIMonthTMP_UGUI;
		public VerticalLayoutGroup UIItemNodeVerticalLayoutGroup;

        public override void OnOpen(UIWindowData uIWindowData)
        {
            base.OnOpen();
            this.OnOpenSystem(uIWindowData);
        }

        public override void OnClose()
        {
            base.OnClose();
            this.OnCloseSystem();
        }

        #region CustomFields

	public List<GameObject> Items = new();

        #endregion
    }
}