using System;
using cfg.Item;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XN
{
public static class ItemRankTopPlayerSystem
{
	#region CircleLife

	public static void OnRefresh(this ItemRankTopPlayer self, ItemRankTopPlayerData data)
	{
		StarInfoConfigCategory starInfoCc = TotalConfigManager.ConfigManager.StarInfoConfigCategory;
		self.BtnOnClick = data.OnClick;
		self.UIHeadBgBtn.enabled = data.OnClick != null;
		self.UINameTextMeshProUGUI.text = data.Name;
		self.UINameTxetText.text = data.Name;
		YooAssetManager.Instance.LoadSpriteAsync(ResHelper.GetAvatarUrl(data.AvatarUrl), self.UIHeadIconImage).ToCoroutine();
		
		self.UIHeadBgImage.transform.localScale = Vector3.one * data.Scale;
		// Star 星级
		self.StarGroupComp.SetStarNum(data.StarNum);
		
		int index = data.RankIndex;
		var oneStarInfo = starInfoCc.DataList.Find(x => x.RankNumber[0] <= index && index <= x.RankNumber[1]);

		// frame
		self.UIHeadFrameImage.enabled = data.IsShowFrame;
		if (data.IsShowFrame)
		{
			YooAssetManager.Instance.LoadSpriteAsync(ResHelper.GetIconOrNone(oneStarInfo.FrameRes),self.UIHeadFrameImage).ToCoroutine();
		}

		// Bg + Fg
		YooAssetManager.Instance.LoadSpriteAsync(oneStarInfo.StarBGRes,self.UIHeadBgImage,true).ToCoroutine();

		YooAssetManager.Instance.LoadSpriteAsync(ResHelper.GetIconOrNone(oneStarInfo.StarUpRes),self.UIHeadFgImage,true).ToCoroutine();

	}

	#endregion

    #region UIEvents
    
    public static void UIHeadBgBtnOnClick(this ItemRankTopPlayer self)
    {
	    self.BtnOnClick?.Invoke("todo");
    }
    
    #endregion
    
    #region GlobalEvents
    #endregion
    
    #region Logics
    #endregion
}
}
