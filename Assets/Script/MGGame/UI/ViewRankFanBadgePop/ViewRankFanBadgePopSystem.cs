using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

namespace XN
{
public static class ViewRankFanBadgePopSystem
{
	#region CircleLife
    public static void OnOpenSystem(this ViewRankFanBadgePop self, UIWindowData uIWindowData)
    {
	    self.currIndex = 1;
	    self.RefreshImage(0).ToCoroutine();
    }
    
    public static void OnCloseSystem(this ViewRankFanBadgePop self)
    {
        
    }
	#endregion

    #region UIEvents
    public static void UIBgButtonOnClick(this ViewRankFanBadgePop self)
    {
	    self.Close();
    }

    public static void UILeftButtonOnClick(this ViewRankFanBadgePop self)
    {
	    // -1
	    self.RefreshImage(-1).ToCoroutine();
    }

    public static void UIRightButtonOnClick(this ViewRankFanBadgePop self)
    {
	    // +1
	    self.RefreshImage(1).ToCoroutine();

    }
    #endregion
    
    #region GlobalEvents
    #endregion
    
    #region Logics

    public static async UniTask RefreshImage(this ViewRankFanBadgePop self, int offset)
    {
	    var badgeInfo = TotalConfigManager.ConfigManager.BadgeInfoConfigCategory;
	    
	    self.currIndex += offset;
	    if (self.currIndex > badgeInfo.DataList.Count)
	    {
		    self.currIndex %= badgeInfo.DataList.Count;
	    }
	    else if (self.currIndex <= 0)
	    {
		    self.currIndex += badgeInfo.DataList.Count;
	    }

	    string desc = badgeInfo.GetOrDefault(self.currIndex).BadgeText;
	    self.UIDescTextMeshProUGUI.SetText(desc);
	    
	    string icon = badgeInfo.GetOrDefault(self.currIndex).BadgeImage;
	    await YooAssetManager.Instance.LoadSpriteAsync(icon, self.UIIconImage);
	    self.UIIconImage.SetNativeSize();
    }
    #endregion
}
}
