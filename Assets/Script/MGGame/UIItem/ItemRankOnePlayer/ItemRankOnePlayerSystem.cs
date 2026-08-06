using cfg;
using cfg.Item;
using Cysharp.Threading.Tasks;

namespace XN
{
public static class ItemRankOnePlayerSystem
{
	#region CircleLife

	public static void OnRefresh(this ItemRankOnePlayer self, ItemRankOnePlayerData data)
	{
		// Clear
		self.UINumImage.gameObject.SetActive(false);
		self.UINumTextMeshProUGUI.SetText("");
		self.UIValueTxt2TextMeshProUGUI.SetText("");
		self.UIValueTxt3TextMeshProUGUI.SetText("");
		self.UIWeekImproveImage.gameObject.SetActive(false);// 局内榜 + 周排名
		self.UIValueWeekImage.gameObject.SetActive(false);	// 七天周榜 + 皮肤
		self.UIBgSkinImage.gameObject.SetActive(false);
		self.UIBgFansImage.gameObject.SetActive(false);
		
		// 通用信息
		// self.UINameTextMeshProUGUI.text = string.IsNullOrEmpty(data.Name) ? data.PlayerId : data.Name;
		self.UINameTextText.text = string.IsNullOrEmpty(data.Name) ? data.PlayerId : data.Name;
		YooAssetManager.Instance.LoadSpriteAsync(data.AvatarUrl, self.UIHeadIconImage).ToCoroutine();
		self.UIHeadFrameImage.enabled = data.IsShowFrame;
		if (data.IsShowFrame)
		{
			string frame = RankHelper.GetHallOfFameFrameResByIndex(data.Index);
			YooAssetManager.Instance.LoadSpriteAsync(frame,self.UIHeadFrameImage,true).ToCoroutine();
		}
		if (data.Index > 3)
		{
			self.UINumTextMeshProUGUI.SetText(data.Index.ToString());
		}
		else
		{
			self.UINumImage.gameObject.SetActive(true);
			YooAssetManager.Instance.LoadSpriteAsync($"sjpm_pm{data.Index}", self.UINumImage).ToCoroutine();
		}
		
		// 差异部件
		switch (data.RankType)
		{
			case RankType.None:
				string fomartStr1 = "<size=30>{0}</size>";
				string fomartStr2 = "<size=30>{0}</size>\n<size=24><color=#00ff06>抢{1}</color></size>";
				string fomartStr3 = "<size=32>{0}</size>\n<size=26><color=#00ff06>保底粉丝</color></size>";

				if (data.WinScore > 0)
				{
					self.UIValueTxt1TextMeshProUGUI.SetText(string.Format(fomartStr2, UIManagerHelper.UIMathCeil(data.OwnScore), UIManagerHelper.UIMathCeil(data.WinScore)));
				}
				else
				{
					self.UIValueTxt1TextMeshProUGUI.SetText(string.Format(fomartStr1, UIManagerHelper.UIMathCeil(data.OwnScore)));
				}

				if (data.FansIsMin)
				{
					self.UIValueTxt2TextMeshProUGUI.SetText(string.Format(fomartStr3,TotalConfigManager.ConfigManager.ConstConfigCategory.MinimumFortune));
				}
				else if (data.WinFans > 0)
				{
					self.UIValueTxt2TextMeshProUGUI.SetText(string.Format(fomartStr2,UIManagerHelper.UIMathCeil(data.OwnFans + data.WinFans),UIManagerHelper.UIMathCeil(data.WinFans)));
				}
				else
				{
					self.UIValueTxt2TextMeshProUGUI.SetText(string.Format(fomartStr1,UIManagerHelper.UIMathCeil(data.OwnFans + data.WinFans)));
				}
				self.UIWeekImproveImage.gameObject.SetActive(true);
				self.UIWeekImproveImage.enabled = data.Index <= 3;
				
				if (data.WeekRankIndex < 0 && data.RankNode.rankIndex < 0)	// 两次都没上榜
				{
					self.UIWeekIndexIncImage.gameObject.SetActive(false);
					self.UIWeekIndexDecImage.gameObject.SetActive(false);
					self.UIWeekImproveTextMeshProUGUI.SetText("");
				}
				else
				{
					int offset = data.RankNode.rankIndex < 0 ? 1 : data.RankNode.rankIndex - data.WeekRankIndex;
					self.UIWeekIndexIncImage.gameObject.SetActive(offset > 0);
					self.UIWeekIndexDecImage.gameObject.SetActive(offset < 0);
					self.UIWeekImproveTextMeshProUGUI.SetText(data.WeekRankIndex.ToString());
				}
				break;
			case RankType.WeekRank:
				self.UIValueTxt1TextMeshProUGUI.SetText(UIManagerHelper.UIMathCeil(data.OwnScore + data.WinScore));
				self.UIValueTxt2TextMeshProUGUI.SetText("");
				// self.UIValueTxt2TextMeshProUGUI.SetText(UIManagerHelper.UIMathCeil(data.OwnFans + data.WinFans));
				YooAssetManager.Instance.LoadSpriteAsync(data.Text5, self.UIValueWeekImage).ToCoroutine();
				self.UIValueWeekImage.gameObject.SetActive(true);
				break;
			case RankType.FansRank:
				self.UIValueTxt1TextMeshProUGUI.SetText(UIManagerHelper.UIMathCeil(data.OwnFans + data.WinFans));
				self.UIValueTxt3TextMeshProUGUI.SetText(data.Text5);
				self.UIValueTxt3TextMeshProUGUI.gameObject.SetActive(true);
				break;
			case RankType.MonthRank:
				self.UIValueTxt1TextMeshProUGUI.SetText(UIManagerHelper.UIMathCeil(data.OwnScore + data.WinScore));
				break;
			case RankType.PreviousWeekRank:
				self.UIValueTxt1TextMeshProUGUI.SetText(UIManagerHelper.UIMathCeil(data.OwnScore + data.WinScore));
				// skin
				self.ParseItemGroupData(data);
				break;
			case RankType.PreviousMonthRank:
				self.UIValueTxt1TextMeshProUGUI.SetText(UIManagerHelper.UIMathCeil(data.OwnScore + data.WinScore));
				self.ParseItemGroupData(data);
				break;
			case RankType.PreviousMilestone:
				self.UIValueTxt1TextMeshProUGUI.SetText(UIManagerHelper.UIMathCeil(data.OwnScore + data.WinScore));
				self.ParseItemGroupData(data);
				break;
			case RankType.KillRank:
				self.UIValueTxt1TextMeshProUGUI.SetText(data.KillCount.ToString());
				break;
		}
		
	}
	
	#endregion

    #region UIEvents
    #endregion
    
    #region GlobalEvents
    #endregion
    
    #region Logics

    public static void ParseItemGroupData(this ItemRankOnePlayer self, ItemRankOnePlayerData data)
    {
	    // 奖励皮肤包
	    if (data.RewardsShow != null)
	    {
		    YooAssetManager.Instance.LoadSpriteAsync(ResHelper.GetIconOrNone(data.RewardsShow), self.UISkinImage,true).ToCoroutine();
		    self.UIBgSkinImage.gameObject.SetActive(true);
	    }
	    
	    // Fans
	    if (data.FansItemGroup != null)
	    {
		    self.UIValueFansTextMeshProUGUI.SetText(UIManagerHelper.UIMathCeil(data.FansItemGroup.Number));
		    self.UIBgFansImage.gameObject.SetActive(true);
	    }
    }
    #endregion
}
}
