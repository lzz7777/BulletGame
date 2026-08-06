namespace XN
{
public static class ItemRankRuleKVSystem
{
	#region CircleLife

	public static void OnRefresh(this ItemRankRuleKV self, ItemRankRuleKVData data)
	{
		self.UIRuleTileRankKeyTextMeshProUGUI.SetText(data.Key);
		self.UIRuleTileRankValueTextMeshProUGUI.SetText(data.Value);
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
