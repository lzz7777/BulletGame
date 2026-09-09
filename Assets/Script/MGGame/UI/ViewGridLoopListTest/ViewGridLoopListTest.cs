using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XN
{
    public class ViewGridLoopListTest : UIPanelBase
    {
		public Button MaskBtn;
		public UIGridLoopList ScrollViewGridLoopList;

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

        private void Awake()
        {
            MaskBtn.onClick.AddListener(this.OnBack);
        }

        #endregion
    }
}