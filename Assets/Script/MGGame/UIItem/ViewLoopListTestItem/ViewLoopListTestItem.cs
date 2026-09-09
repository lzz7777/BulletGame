using TMPro;
using UnityEngine.UI;

namespace XN
{
    public class ViewLoopListTestItem : UIItemBase<ViewLoopListTestItemData>
    {
		public Image IconImage;
		public Button ButtonBtn;
		public TextMeshProUGUI TextTMP_UGUI;
		public TextMeshProUGUI TitleTMP_UGUI;
		public UILoopListItem ViewLoopListTestItemLoopListItem;
        
        public override void Refresh(ViewLoopListTestItemData data)
        {
            this.OnRefresh(data);
        }
                
        #region CustomFields

        #endregion
    }
}