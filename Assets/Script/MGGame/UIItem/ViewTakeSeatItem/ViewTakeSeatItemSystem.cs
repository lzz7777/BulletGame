namespace XN
{
public static class ViewTakeSeatItemSystem
{
	#region CircleLife

	public static void OnRefresh(this ViewTakeSeatItem self, ViewTakeSeatItemData data)
	{
		self.ViewHeadItem.OnRefresh(new ViewHeadItemData()
		{
			PlayerId = data.PlayerId
		});
		self.Animation.Play();
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
