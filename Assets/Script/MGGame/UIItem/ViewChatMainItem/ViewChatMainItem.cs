using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XN
{
    public class ViewChatMainItem : UIItemBase<ViewChatMainItemData>
    {
		public TextMeshProUGUI ContentTMP_UGUI;
		public UILoopListItem ViewChatMainItemLoopListItem;
        
        public override void Refresh(ViewChatMainItemData data)
        {
            this.OnRefresh(data);
        }
                
        #region CustomFields

        #endregion
    }
}