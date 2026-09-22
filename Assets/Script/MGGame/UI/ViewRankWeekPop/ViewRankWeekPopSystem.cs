using System;
using System.Threading;
using cfg.Rank;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

namespace XN
{
    public static class ViewRankWeekPopSystem
    {
        #region CircleLife

        public static async UniTask OnOpenSystem(this ViewRankWeekPop self, UIWindowData uIWindowData)
        {
            if (self.Cts != null)
            {
                self.Cts.Cancel();
                self.Cts.Dispose();
            }

            self.Cts = new CancellationTokenSource();
            
            self.LoadViewAsync(self.Cts.Token).Forget();
        }

        public static void OnCloseSystem(this ViewRankWeekPop self)
        {
            
            if (self.Cts != null)
            {
                self.Cts.Cancel();
                self.Cts.Dispose();
                self.Cts = null;
            }
            
            self.UITitleImage.sprite = null;
            self.UIContent1Image.sprite = null;
            self.UIContent2Image.sprite = null;
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

        public static async UniTask LoadViewAsync(this ViewRankWeekPop self, CancellationToken token)
        {
            WeekRewardShowConfigCategory weekRewardShowConfigCategory =
                TotalConfigManager.ConfigManager.WeekRewardShowConfigCategory;
            WeekRewardShowConfig weekRewardShowConfig = weekRewardShowConfigCategory.GetOrDefault(1);
            // SignRewardConfigCategory signRewardConfigCategory = TotalConfigManager.ConfigManager.Wee;
            // WeekRewardShow
            self.UITitle2TextMeshProUGUI.text = weekRewardShowConfig.BannerText;
            self.UITitleImage.sprite = null;
            self.UIContent1Image.sprite = null;
            self.UIContent2Image.sprite = null;

            YooAssetManager.Instance.LoadSpriteAsync(weekRewardShowConfig.Banner, self.UITitleImage,
                self, false, token).Forget();
            YooAssetManager.Instance.LoadSpriteAsync(weekRewardShowConfig.CarRewardShow,
                self.UIContent1Image, self, false, token).Forget();
            YooAssetManager.Instance.LoadSpriteAsync(weekRewardShowConfig.ItemRewardShow,
                self.UIContent2Image, self, false, token).Forget();
        }

        #endregion
    }
}