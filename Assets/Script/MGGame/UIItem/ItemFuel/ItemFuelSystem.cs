using cfg.Rank;

namespace XN
{
public static class ItemFuelSystem
{
	#region CircleLife

	public static void OnRefresh(this ItemFuel self, ItemFuelData data)
	{

		SignRewardConfig d = data.config;
		string rankStr;
		if(d.RankNumber[0] == d.RankNumber[1])
		{
		 rankStr = $"{d.RankNumber[0]}";
		}
		else if (d.RankNumber[1] == -1)
		{
		 rankStr = $"{d.RankNumber[0]}+";
		}
		else
		{
		 rankStr = $"{d.RankNumber[0]}-{d.RankNumber[1]}";
		}
		self.UIRankitemValue1TextMeshProUGUI.text = rankStr;
		self.UIRankitemValue2TextMeshProUGUI.text = d.SignRewardShow;
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
