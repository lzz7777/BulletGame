namespace XN
{
public static class ViewBattleRankItemSystem
{
	#region CircleLife

	public static void OnRefresh(this ViewBattleRankItem self, RankDataRet data)
	{
		self.UITmpRankIndexTextMeshProUGUI.text = data.Rank.ToString();
		self.UITmpNameText.text = data.Nickname;
		self.UIValueTextMeshProUGUI.text = UIManagerHelper.UIMathCeil((float)data.Score);
		
		self.ViewHeadItem.OnRefresh(new ViewHeadItemData()
		{
			AvatarUrl = data.AvatarUrl,
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
