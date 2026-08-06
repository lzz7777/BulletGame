using TMPro;

namespace XN
{
    public class ViewMaximumRangeNode : UISubViewBase
    {
		public UISubView ViewMaximumRangeNodeSubView;
		public TextMeshProUGUI UIMaximumRangeTMP_UGUI;

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

        public bool IsShow;

        private void Update()
        {
            this.OnUpdateSystem();
        }

        #endregion
    }
}