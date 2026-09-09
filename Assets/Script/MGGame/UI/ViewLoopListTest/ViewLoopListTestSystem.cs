using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace XN
{
public static class ViewLoopListTestSystem
{
	#region CircleLife
    public static void OnOpenSystem(this ViewLoopListTest self, UIWindowData uIWindowData)
    {
	    self.ScrollViewUILoopList.ClearData();

	    var itemHeights = new List<float>();
	    for (int i = 0; i < 100; i++)
	    {
		    self.ScrollViewUILoopList.AddData(out ViewLoopListTestItemData itemData);
		    itemData.Index = i;
		    itemData.Title = $"content: {i}";
		    // 测试动态高度：偶数索引高度为100，奇数索引高度为200
		    itemHeights.Add(i % 2 == 0 ? 100f : 200f);
	    }

	    self.ScrollViewUILoopList.RefreshItem(true, itemHeights).Forget();
    }
    
    public static void OnCloseSystem(this ViewLoopListTest self)
    {
        
    }
	#endregion

    #region UIEvents
    
    public static void OnMaskClick(this ViewLoopListTest self)
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
