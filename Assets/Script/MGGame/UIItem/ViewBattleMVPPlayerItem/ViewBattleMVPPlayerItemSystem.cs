using Cysharp.Threading.Tasks;

namespace XN
{
public static class ViewBattleMVPPlayerItemSystem
{
	#region CircleLife

	public static void OnRefresh(this ViewBattleMVPPlayerItem self, ViewBattleMVPPlayerItemData data)
	{
		// self.UINameTextMeshProUGUI.SetText(data.Name);
		self.UINameText.text = data.Name;
		YooAssetManager.Instance.LoadSpriteAsync(ResHelper.GetAvatarUrl(data.AvatarUrl), self.UIHeadIconImage).ToCoroutine();
		self.UIScoreTextMeshProUGUI.text = $"积分<color=#f3ec32><size=24>{UIManagerHelper.UIMathCeil(data.Score)}</size></color>";
		self.UIScoreAddTextMeshProUGUI.text = data.ScoreAdd > 0 ? $"抢{UIManagerHelper.UIMathCeil(data.ScoreAdd)}" :"";
		string formStr = "粉丝<color=#f3ec32><size=24>{0}</size></color>";

		if (data.FansIsMin)
		{
			self.UIFansTextMeshProUGUI.text = string.Format(formStr,TotalConfigManager.ConfigManager.ConstConfigCategory.MinimumFortune);
			self.UIFansAddTextMeshProUGUI.text = "保底粉丝";
		}
		else
		{
			self.UIFansTextMeshProUGUI.text = string.Format(formStr,UIManagerHelper.UIMathCeil(data.Fans));
			self.UIFansAddTextMeshProUGUI.text = data.FansAdd > 0 ? $"抢{UIManagerHelper.UIMathCeil(data.FansAdd)}" : "";
		}

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
