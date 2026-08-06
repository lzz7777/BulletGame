using System.Collections.Generic;

namespace XN
{
    public class ViewItemShowNode : UISubViewBase
    {
		public UISubView ViewItemShowNodeSubView;

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

        //key:playerid inputid
        public Dictionary<(string, int), ViewItemShowItem> ItemDic = new();

        #endregion
    }
}