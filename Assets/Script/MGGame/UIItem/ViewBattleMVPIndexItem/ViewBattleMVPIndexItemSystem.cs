using System;

namespace XN
{
public static class ViewBattleMVPIndexItemSystem
{
	#region CircleLife

	public static void OnRefresh(this ViewBattleMVPIndexItem self, ViewBattleMVPIndexItemData data)
	{
		self.UISingleBgImage.enabled = data.RoomIndex % 2 == 1;
		
		// 局内排名
		String indexDesc = "";
		switch (data.RoomIndex)
		{
			case 1:
				indexDesc = $"<color=#ffd800><size=48>{data.RoomIndex}</size><size=36>ST</size></color> ";
				break;
			case 2:
				indexDesc = $"<color=#ff8c05><size=48>{data.RoomIndex}</size><size=36>ND</size></color>";
				break;
			case 3:
				indexDesc = $"<color=#4dd16e><size=48>{data.RoomIndex}</size><size=36>RD</size></color>";
				break;
			default:
				indexDesc = $"<color=#5dc176><size=48>{data.RoomIndex}</size></color>";
				break;
		}
		self.UIIndexTextMeshProUGUI.text = indexDesc;
		// self.UINameTextMeshProUGUI.text = data.Name ?? data.PlayerId;
		self.UINameText.text = data.Name;

		// 里程变化
		string mileStandardStr = "{0}米\n<size=26><color=#daa430>{1}{2}米</color></size>";
		string mileStr = UIManagerHelper.UIMathCeil(data.Mile);
		string addMileSign = data.MileAdd > 0 ? "+" : "-";
		string mileAddStr = UIManagerHelper.UIMathCeil(data.MileAdd);
		self.UIMilesAndAddTextMeshProUGUI.text = string.Format(mileStandardStr, mileStr, addMileSign, mileAddStr);
		
		// 排名变化
		if (data.RankNode.rankIndex < 0 && data.RankIndex < 0)	// 两次都没上榜
		{
			self.UIRankIncImage.gameObject.SetActive(false);
			self.UIRankDecImage.gameObject.SetActive(false);
			self.UIWorldRankTextMeshProUGUI.SetText("");
		}
		else
		{
			int offset = data.RankNode.rankIndex < 0 ? 1 : data.RankNode.rankIndex - data.RankIndex;
			self.UIRankIncImage.gameObject.SetActive(offset > 0);
			self.UIRankDecImage.gameObject.SetActive(offset < 0);
			self.UIWorldRankTextMeshProUGUI.SetText(data.RankIndex.ToString());
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
