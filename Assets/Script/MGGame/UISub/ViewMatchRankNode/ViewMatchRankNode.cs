using System;
using System.Collections.Generic;
using UnityEngine;

namespace XN
{
    public class ViewMatchRankNode : UISubViewBase
    {
		public UISubView ViewMatchRankNodeSubView;
		public UILoopList ScrollViewLoopList;

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
        public bool IsDirty = false;
        public float RefreshDt = 0;
        
        private void Awake()
        {
            ObjectPoolManager.Instance.AdvanceAddRes<ViewMatchRankItem>(10);
        }

        private void Update()
        {
            this.OnUpdateSystem();
        }

        #endregion
    }
}