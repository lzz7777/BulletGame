using Cysharp.Threading.Tasks;

namespace XN
{
    public static class ViewGridLoopListTestSystem
    {
        #region CircleLife
        
        public static void OnOpenSystem(this ViewGridLoopListTest self, UIWindowData uIWindowData)
        {
            // 1. 清空旧数据
            self.ScrollViewGridLoopList.ClearData();

            // 2. 模拟添加 100 个背包格子数据
            for (int i = 0; i < 10000; i++)
            {
                self.ScrollViewGridLoopList.AddData<ViewGridLoopListTestItemData>(out var itemData);
                
                // 给数据赋值，这个数据会被传递给 Item 的 OnRefresh
                itemData.Index = i;
            }

            // 3. 刷新整个列表，这会触发内部的对象池获取与排版
            self.ScrollViewGridLoopList.RefreshItem().Forget();
        }
        
        public static void OnCloseSystem(this ViewGridLoopListTest self)
        {
            // 在面板关闭时，主动清空数据并将所有格子回收到对象池
            // 这遵循了我们将生命周期控制权交给 UI 面板的架构设计
            self.ScrollViewGridLoopList.OnClose();
        }
        
        #endregion
        
        #region UIEvents

        public static void OnBack(this ViewGridLoopListTest self)
        {
            self.Close();
        }

        #endregion
        
        #region GlobalEvents
        #endregion
        
        #region Logics
        #endregion
    }
}