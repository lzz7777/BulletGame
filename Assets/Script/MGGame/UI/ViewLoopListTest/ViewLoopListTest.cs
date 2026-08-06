using UnityEngine.UI;

namespace XN
{
    public class ViewLoopListTest : UIPanelBase
    {
		public UILoopList ScrollViewUILoopList;
		public Button MaskBtn;

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
			MaskBtn.onClick.AddListener(this.OnMaskClick);
		}

        #endregion
    }
}