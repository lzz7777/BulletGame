namespace XN
{
public static class ViewLoopListTestSystem
{
	#region CircleLife
    public static void OnOpenSystem(this ViewLoopListTest self, UIWindowData uIWindowData)
    {
	    self.ScrollViewUILoopList.ClearData();
            
	    for (int i = 0; i < 100; i++)
	    {
		    self.ScrollViewUILoopList.AddData(out ViewLoopListTestItemData itemData);
		    itemData.Index = i;
		    itemData.Title = $"content: {i}";
	    }

	    self.ScrollViewUILoopList.RefreshItem();
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
