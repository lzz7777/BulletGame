using cfg.Rank;
using Sirenix.OdinInspector;
using UnityEngine;

namespace XN
{
public static class ViewRankWeekPopSystem
{
	#region CircleLife
    public static void OnOpenSystem(this ViewRankWeekPop self, UIWindowData uIWindowData)
    {
        
    }
    
    public static void OnCloseSystem(this ViewRankWeekPop self)
    {
        
    }
	#endregion

    #region UIEvents
    
    public static void UIBtnCloseButtonOnClick(this ViewRankWeekPop self)
    {
	    self.Close();
    }
    
    public static void UIMaskButtonOnClick(this ViewRankWeekPop self)
    {
	    self.Close();
    }
    #endregion
    
    #region GlobalEvents
    #endregion
    
    #region Logics

    public static void Init(this ViewRankWeekPop self)
    {
	    WeekRewardShowConfigCategory weekRewardShowConfigCategory = TotalConfigManager.ConfigManager.WeekRewardShowConfigCategory;
	    WeekRewardShowConfig weekRewardShowConfig = weekRewardShowConfigCategory.GetOrDefault(1);
	    // SignRewardConfigCategory signRewardConfigCategory = TotalConfigManager.ConfigManager.Wee;
	    // WeekRewardShow
	    self.UITitleImage.sprite = YooAssetManager.Instance.LoadAssetSync<Sprite>(weekRewardShowConfig.Banner);
	    self.UITitle2TextMeshProUGUI.text = weekRewardShowConfig.BannerText;
	    self.UIContent1Image.sprite = YooAssetManager.Instance.LoadAssetSync<Sprite>(weekRewardShowConfig.CarRewardShow);
	    self.UIContent2Image.sprite = YooAssetManager.Instance.LoadAssetSync<Sprite>(weekRewardShowConfig.ItemRewardShow);
    }
    #endregion
}
}
