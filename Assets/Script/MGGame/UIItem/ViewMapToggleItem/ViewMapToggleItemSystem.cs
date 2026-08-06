using cfg.Fight;
using Cysharp.Threading.Tasks;

namespace XN
{
public static class ViewMapToggleItemSystem
{
	#region CircleLife

	public static void OnRefresh(this ViewMapToggleItem self, ViewMapToggleItemData data)
	{
		self.toggleIndex = data.SceneId;
		self.OnClick = data.OnClick;
		self.toggle.group = data.toggleGroup;
		self.UIOneMapToggleSetIsOn(data.isOn);

		SceneGroupInfoConfigCategory sceneInfoCc = TotalConfigManager.ConfigManager.SceneGroupInfoConfigCategory;
		SceneGroupInfoConfig oneSceneInfo = sceneInfoCc.GetOrDefault(data.SceneId);
		if (oneSceneInfo != null && oneSceneInfo.IsUsed)
		{
			self.toggle.interactable = true;
			self.UIBackgroundImage.gameObject.SetActive(true);
			self.UITextTextMeshProUGUI.SetText(oneSceneInfo.SceneName);
			YooAssetManager.Instance.LoadSpriteAsync(oneSceneInfo.Preview, self.UIBgImage,true).ToCoroutine();
		}
		else
		{
			self.toggle.interactable = false;
			self.UIBackgroundImage.gameObject.SetActive(false);
			self.UITextTextMeshProUGUI.SetText("");
			YooAssetManager.Instance.LoadSpriteAsync("bg_ditu_jqqd", self.UIBgImage,true).ToCoroutine();
		}
	}

	#endregion

    #region UIEvents

    public static void UIOneMapToggleOnChanged(this ViewMapToggleItem self, bool isOn)
    {
	    if (isOn)
	    {
		    self.OnClick?.Invoke(self.toggleIndex);
	    }
    }
    #endregion
    
    #region GlobalEvents
    #endregion
    
    #region Logics
    
    public static void UIOneMapToggleSetIsOn(this ViewMapToggleItem self, bool isOn)
    {
	    self.toggle.SetIsOnWithoutNotify(isOn);	// 普通 toggle

	    // 手动切换背景图
	    // if (isOn)
	    // {
		   //  self.toggle.OnSelect(null);
	    // }
	    // else
	    // {
		   //  self.toggle.OnDeselect(null);
	    // }
    }
    
    #endregion
}
}
