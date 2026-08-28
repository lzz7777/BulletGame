using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XN
{
    public static class ViewHeadItemSystem
    {
        #region CircleLife

        public static async UniTask OnRefresh(this ViewHeadItem self, ViewHeadItemData data)
        {
            string avatarUrl = string.Empty;

            if (!string.IsNullOrEmpty(data.PlayerId))
            {
                var playerInfoComp = RoomHelper.GetRoomInfoComponent().GetPlayerInfoComponent(data.PlayerId);
                avatarUrl = playerInfoComp?.AvatarUrl;
            }

            if (!string.IsNullOrEmpty(data.AvatarUrl))
            {
                avatarUrl = data.AvatarUrl;
            }

            if (data.SizeData != Vector2.zero)
            {
                (self.transform as RectTransform).sizeDelta = data.SizeData;
            }

            await YooAssetManager.Instance.LoadSpriteAsync("Fight",ResHelper.GetAvatarUrl(avatarUrl), self.UIHeadIconImage);
            self.UIHeadFrameImage.gameObject.SetActive(!string.IsNullOrEmpty(data.Frame));

            if (self.FrameEffect != null)
            {
                ObjectPoolManager.Instance.ReturnToPool(self.FrameEffect?.gameObject);
                self.FrameEffect = null;
            }

            if (!string.IsNullOrEmpty(data.Frame))
            {
                YooAssetManager.Instance.LoadSpriteAsync(data.Frame, self.UIHeadFrameImage).ToCoroutine();
                string frameEffectId = data.Frame.Replace("mrt_txk_", "fx_ui_UIHeadFrame_");
                if (data.Frame == "none") return;

                self.FrameEffect =
                    await EffectHelper.GetEffect(frameEffectId,
                        self.UIHeadFrameImage
                            .transform);
                self.FrameEffect?.RefreshLayerOrder(data.SortingOrder);
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