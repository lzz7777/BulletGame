using System.Threading;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XN
{
    public class ViewGridLoopListTestItem : UIItemBase<ViewGridLoopListTestItemData>
    {
        public UILoopListItem ViewGridLoopListTestItemLoopListItem;
        public Image IconImage;
        public Text NumText;

        #region CustomFields

        public CancellationTokenSource Cts;

        public override void Refresh(ViewGridLoopListTestItemData data)
        {
            this.OnRefresh(data);
        }

        private void OnDestroy()
        {
            if (Cts != null)
            {
                Cts.Cancel();
                Cts.Dispose();
                Cts = null;
            }
        }

        #endregion
    }
}