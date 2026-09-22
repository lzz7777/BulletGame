namespace XN
{
    public class UISubViewBase : UIAssetHandleOwnerBase
    {
        public bool IsOpen;
        
        public virtual void OnOpen(UIWindowData uIWindowData = null)
        {
        }

        public virtual void OnClose()
        {
        }
    }
}