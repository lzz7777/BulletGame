using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XN
{
    public class ViewRankListPop : UIPanelBase
    {
		public Button MaskBtn;
		public Button UICloseBtn;
		public RectTransform NodeRankRectTrans;
		public Button UIMrtBtn;
		public Button UIZlcbBtn;
		public Button UIDbpfBtn;
		public Button UIPhbBtn;
		public RectTransform NodeSkinRectTrans;
		public Button UIZbpfBtn;
		public Button UIDhpfBtn;
		public RectTransform NodeRectTrans;

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

        public float Offset = 20f;

        private void Awake()
        {
            // common
            UICloseBtn.onClick.AddListener(this.UICloseButtonOnClick);
            MaskBtn.onClick.AddListener(this.UICloseButtonOnClick);
            
            // 排行榜
            UIMrtBtn.onClick.AddListener(this.UIMrtButtonOnClick);
            UIZlcbBtn.onClick.AddListener(this.UIZlcbButtonOnClick);
            UIDbpfBtn.onClick.AddListener(this.UIDbpfButtonOnClick);
            UIPhbBtn.onClick.AddListener(this.UIPhbButtonOnClick);

            // 皮肤
            UIZbpfBtn.onClick.AddListener(this.UIZbpfButtonOnClick);
            UIDhpfBtn.onClick.AddListener(this.UIDhpfButtonOnClick);
        }

        #endregion
    }
}