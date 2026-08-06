using Spine.Unity;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XN
{
public class ViewLoadingMain : UIPanelBase
{
    public TextMeshProUGUI UITextPrepaTextMeshProUGUI;
    public Image UIProgressFgImage;
    public TextMeshProUGUI UIProgressPctTextMeshProUGUI;
    public TextMeshProUGUI UITextTipsTextMeshProUGUI;

	public override void OnOpen(UIWindowData uIWindowData)
	{
		base.OnOpen();
		this.OnOpenSystem(uIWindowData);
	}

	public override void OnClose()
	{
		base.OnClose();
		this.OnCloseSystem();
	}

	#region CustomFields

	public List<Image> logoList;
	public void Refresh( int currNum, int totalNum)
	{
		float progress = currNum / (float)totalNum;
		UIProgressFgImage.fillAmount = progress;
		UIProgressPctTextMeshProUGUI.SetText($"{System.Math.Round(progress*100, 2)}%");
	}

	public void LoadLogo()
	{
		var currChannel = TotalConfigManager.ConfigManager.ConstConfigCategory.CurrChannel;
		var constCc = TotalConfigManager.ConfigManager.LoginInfoConfigCategory.GetOrDefault(currChannel);
		foreach (var Img in logoList)
		{
			Img.SetActiveScale(Img.name == constCc.Logo);
		}
		// YooAssetManager.Instance.LoadSpriteAsync(constCc.Logo, UITitleImage, true).ToCoroutine();
	}
	#endregion
}
}
