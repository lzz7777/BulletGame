using UnityEngine;

namespace XN
{
    public abstract class UIItemBase : MonoBehaviour
    {
        public virtual void Refresh(UIItemDataBase uIItemDataBase) { }
        
        /// <summary>
        /// 当该Item进入分帧加载队列时触发。
        /// 子类可重写此方法，显示加载中状态（如隐藏旧内容、显示白块或Loading特效），避免数据串位。
        /// </summary>
        public virtual void ShowLoadingState() { }
    }
    
    // 增加一个泛型基类
    public abstract class UIItemBase<TData> : UIItemBase where TData : UIItemDataBase
    {
        // 隐藏/密封基类的方法，内部做安全转换
        public sealed override void Refresh(UIItemDataBase data)
        {
            if (data is TData typedData)
            {
                Refresh(typedData);
            }
        }

        // 暴露给子类重写的强类型方法
        public abstract void Refresh(TData data);
    }
}