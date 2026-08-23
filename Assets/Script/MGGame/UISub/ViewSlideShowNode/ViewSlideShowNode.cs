using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XN
{
    public class ViewSlideShowNode : UISubViewBase
    {
        public UISubView ViewSlideShowItemSubView;
        public Image UIIcon1Image;
        public Image UIIcon2Image;

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

        public List<Image> Icons = new();
        public float Time;
        public int IconIndex;
        public int MaxNum;
        public Sprite[] CachedSprites;

        private void Update()
        {
            this.OnUpdateSystem();
        }

        #endregion
    }
}