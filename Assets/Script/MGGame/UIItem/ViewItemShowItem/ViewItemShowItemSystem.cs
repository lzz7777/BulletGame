using cfg;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace XN
{
    public static class ViewItemShowItemSystem
    {
        #region CircleLife

        public static async UniTask OnRefresh(this ViewItemShowItem self, ViewItemShowItemData data)
        {
            var inputConf = TotalConfigManager.ConfigManager.InputIndexConfigCategory.Get(data.InputId);

            if (inputConf.InputNumber != 1)
            {
                return;
            }

            self.Time = 0;

            //相同指令，并且数量为1，数量叠加
            self.ItemNum += inputConf.InputNumber;

            self.UIItemNumTextMeshProUGUI.text = self.ItemNum.ToString();
        }

        public static void OnUpdateSystem(this ViewItemShowItem self)
        {
            self.Time += Time.deltaTime;

            if (self.Time > 1.5f && !self.IsDisable)
            {
                self.OnDisable();
            }
        }

        #endregion

        #region UIEvents

        #endregion

        #region GlobalEvents

        #endregion

        #region Logics

        public static async UniTask OnInit(this ViewItemShowItem self, ViewItemShowItemData data)
        {
            self.transform.localPosition = new Vector2(500, 0);
            self.CanvasGroup.alpha = 1;

            var inputConf = TotalConfigManager.ConfigManager.InputIndexConfigCategory.Get(data.InputId);
            var playerInfoComp = RoomHelper.GetRoomInfoComponent().GetPlayerInfoComponent(data.PlayerId);
            var carInfoComp = EntityManager.Instance.GetEntityById(playerInfoComp.CarId)
                .GetComponent<CarInfoComponent>();
            
            self.Time = 0;
            self.InputId = data.InputId;
            self.PlayerId = data.PlayerId;
            self.ItemNum = inputConf.InputNumber;
            self.IsDisable = false;
            
            //刷新玩家信息
            self.UINameText.text = playerInfoComp.Name;
            self.UICarNameText.text = $"车队:{carInfoComp.Name}";
            self.UIItemNameTextMeshProUGUI.text = inputConf.InputStr;
            self.UIItemNumTextMeshProUGUI.text = self.ItemNum.ToString();
            YooAssetManager.Instance.LoadSpriteAsync(inputConf.Icon, self.UIItemIconImage, true);
            YooAssetManager.Instance.LoadSpriteAsync(inputConf.InputRes, self.UIFrameImage);

            self.ViewHeadItem.OnRefresh(new ViewHeadItemData()
            {
                PlayerId = data.PlayerId
            });
            
            self.transform.DOLocalMoveX(0, 0.1f);
        }

        private static void OnDisable(this ViewItemShowItem self)
        {
            //删除
            self.IsDisable = true;
            self.ParentNode.RemoveData(self.PlayerId, self.InputId);
            self.transform.DOLocalMoveY(200, 0.5f);
            self.CanvasGroup.DOFade(0, 0.5f).OnComplete(() =>
            {
                ObjectPoolManager.Instance.ReturnToPool(self.gameObject);
            });
        }

        #endregion
    }
}