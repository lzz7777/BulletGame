using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XN
{
    public class ViewMatchRankNode : UISubViewBase
    {
		public UISubView ViewMatchRankNodeSubView;
		public VerticalLayoutGroup UILayoutVerticalLayoutGroup;

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

	    public List<ViewMatchRankNodeData> TempDatas = new();
	    public List<ViewMatchRankNodeData> Datas = new();
	    public List<GameObject> Objs = new();

        #endregion
    }
}