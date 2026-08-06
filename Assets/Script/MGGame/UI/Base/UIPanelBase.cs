using System.Collections.Generic;
using UnityEngine;

namespace XN
{
    public enum UIPanelType
    {
        Normal,
        Title,
        Pop,
        Top,
        Net,
    }

    public abstract class UIPanelBase : MonoBehaviour
    {
        public UIPanelType UIPanelType;

        public Dictionary<string, UISubViewBase> SubViews = new();
        
        public virtual void OnOpen(UIWindowData uIWindowData = null)
        {
        }

        public virtual void OnClose()
        {
            foreach (var subViewName in SubViews.Keys)
            {
                this.CloseSubWindowAsync(subViewName);
            }
        }
    }
}