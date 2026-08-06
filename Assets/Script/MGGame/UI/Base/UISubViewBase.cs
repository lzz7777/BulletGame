using UnityEngine;

namespace XN
{
    public class UISubViewBase : MonoBehaviour
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