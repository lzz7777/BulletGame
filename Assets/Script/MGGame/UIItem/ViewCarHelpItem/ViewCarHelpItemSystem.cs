namespace XN
{
public static class ViewCarHelpItemSystem
{
	#region CircleLife

	public static void OnRefresh(this ViewCarHelpItem self, ViewCarHelpItemData data)
	{
		self.viewHeadItem.OnRefresh(new ViewHeadItemData()
		{
			PlayerId = data.PlayerId,
		});
	}

	#endregion

    #region UIEvents
    #endregion
    
    #region GlobalEvents
    #endregion
    
    #region Logics
    #endregion
}
}
