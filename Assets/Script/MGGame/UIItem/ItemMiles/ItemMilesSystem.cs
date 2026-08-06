using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XN
{
public static class ItemMilesSystem
{
	#region CircleLife

	public static void OnRefresh(this ItemMiles self, ItemMilesData data)
	{
		self.UIRankIndexTextMeshProUGUI.text = data.rankIndex.ToString();
		self.UINameText.text = data.name;
		self.UIScoreTextMeshProUGUI.text = $"{UIManagerHelper.UIMathCeil(data.score)}米";
		YooAssetManager.Instance.LoadSpriteAsync(ResHelper.GetAvatarUrl(data.headIcon), self.UIHeadIconImage).ToCoroutine();
		YooAssetManager.Instance.LoadSpriteAsync(ResHelper.GetIconOrNone(data.RwdSkin), self.UIIconImage).ToCoroutine();
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
