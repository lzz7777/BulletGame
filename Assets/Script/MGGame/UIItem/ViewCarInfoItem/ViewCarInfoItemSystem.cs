using System;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using State = cfg.State;

namespace XN
{
    public static class ViewCarInfoItemSystem
    {
        private static readonly StringBuilder _sb = new StringBuilder(32);

        #region CircleLife

        public static void OnRefresh(this ViewCarInfoItem self, ViewCarInfoItemData itemData)
        {
        }

        public static void OnUpdateSystem(this ViewCarInfoItem self)
        {
            var carUnit = self.CarUnit;
            if (carUnit == null)
            {
                return;
            }

            self.UpdateMileage();
            self.UpdatePos();
        }

        #endregion

        #region UIEvents

        #endregion

        #region GlobalEvents

        #endregion

        #region Logics

        public static void Init(this ViewCarInfoItem self)
        {
            self.UIItemProgressBgImage.gameObject.SetActive(false);
            self.DoCloseFastAnim();

            self.MemberIds.Clear();
            self.TempMemberIds.Clear();
            ObjectPoolManager.Instance.ReturnToPool(self.MemberPrefabs);
            self.MemberPrefabs.Clear();

            self.CloseShield();
            self.LastMileage = -1;
            self.MileageUpdateTimer = 0;
        }

        private static void UpdateMileage(this ViewCarInfoItem self)
        {
            long mileage = (long)self.CarInfoComponent.Mileage;
            if (mileage == self.LastMileage)
            {
                return;
            }

            self.MileageUpdateTimer += Time.deltaTime;
            if (self.LastMileage != -1 && self.MileageUpdateTimer < 0.1f)
            {
                return;
            }

            self.MileageUpdateTimer = 0f;
            self.LastMileage = mileage;

            _sb.Clear();
            if (mileage < 1000000)
            {
                _sb.Append(mileage);
            }
            else
            {
                _sb.Append(UIManagerHelper.UIMathCeil(mileage, 2, true));
            }
            _sb.Append("米");
            string mileageStr = _sb.ToString();

            switch (self.NameplateType)
            {
                case ViewCarInfoItemNameplateType.Player:
                    if (self.UIPlayerMileageText.text != mileageStr)
                    {
                        self.UIPlayerMileageText.text = mileageStr;
                        // 移除昂贵的 ForceRebuildLayoutImmediate 调用，使用自适应宽度或由下帧自动布局解决
                        // LayoutRebuilder.ForceRebuildLayoutImmediate(self.UIPlayerMileageText.rectTransform);

                        float playerNodeWidth = Math.Max(324, self.UIPlayerMileageText.preferredWidth + 200);
                        var playerRt = self.UIPlayerNodeImage.transform as RectTransform;
                        if (Math.Abs(playerRt.sizeDelta.x - playerNodeWidth) > 1f)
                        {
                            playerRt.sizeDelta = new Vector2(playerNodeWidth, playerRt.sizeDelta.y);
                        }
                    }
                    break;
                case ViewCarInfoItemNameplateType.PlayerLong:
                    if (self.UIPlayerLongMileageText.text != mileageStr)
                    {
                        self.UIPlayerLongMileageText.text = mileageStr;
                        
                        float playerLongNodeWidth = Math.Max(378, self.UIPlayerLongMileageText.preferredWidth + 240);
                        var playerLongRt = self.UIPlayerLongNodeImage.transform as RectTransform;
                        if (Math.Abs(playerLongRt.sizeDelta.x - playerLongNodeWidth) > 1f)
                        {
                            playerLongRt.sizeDelta = new Vector2(playerLongNodeWidth, playerLongRt.sizeDelta.y);
                        }
                    }
                    break;
                case ViewCarInfoItemNameplateType.Nobody:
                    if (self.UINobodyMileageTextMeshProUGUI.text != mileageStr)
                    {
                        self.UINobodyMileageTextMeshProUGUI.text = mileageStr;
                        
                        float nobodyNodeWidth = Math.Max(304, self.UINobodyMileageTextMeshProUGUI.preferredWidth + 160);
                        var nobodyRt = self.UINobodyNodeImage.transform as RectTransform;
                        if (Math.Abs(nobodyRt.sizeDelta.x - nobodyNodeWidth) > 1f)
                        {
                            nobodyRt.sizeDelta = new Vector2(nobodyNodeWidth, nobodyRt.sizeDelta.y);
                        }
                    }
                    break;
                case ViewCarInfoItemNameplateType.NobodyLong:
                    if (self.UINobodyLongMileageTextMeshProUGUI.text != mileageStr)
                    {
                        self.UINobodyLongMileageTextMeshProUGUI.text = mileageStr;
                        
                        float nobodyLongNodeWidth = Math.Max(357, self.UINobodyLongMileageTextMeshProUGUI.preferredWidth + 200);
                        var nobodyLongRt = self.UINobodyLongNodeImage.transform as RectTransform;
                        if (Math.Abs(nobodyLongRt.sizeDelta.x - nobodyLongNodeWidth) > 1f)
                        {
                            nobodyLongRt.sizeDelta = new Vector2(nobodyLongNodeWidth, nobodyLongRt.sizeDelta.y);
                        }
                    }
                    break;
            }
        }

        private static void UpdatePos(this ViewCarInfoItem self)
        {
            var carInfoComp = self.CarInfoComponent;

            var (isfindNode, carPos) = self.GetPosition();
            float x = carPos.x * 100;
            float frameWidth = 0;

            switch (self.NameplateType)
            {
                case ViewCarInfoItemNameplateType.Player:
                    frameWidth = (self.UIPlayerNodeImage.transform as RectTransform).GetActualWidth();
                    break;
                case ViewCarInfoItemNameplateType.PlayerLong:
                    frameWidth = (self.UIPlayerLongNodeImage.transform as RectTransform).GetActualWidth();
                    break;
                case ViewCarInfoItemNameplateType.Nobody:
                    frameWidth = (self.UINobodyNodeImage.transform as RectTransform).GetActualWidth();
                    break;
                case ViewCarInfoItemNameplateType.NobodyLong:
                    frameWidth = (self.UINobodyLongNodeImage.transform as RectTransform).GetActualWidth();
                    break;
            }

            x -= frameWidth / 2;
            float limitWidth = GameConst.ScreenWidth / 2.0f - 100 - frameWidth;
            x = Math.Min(x, limitWidth);

            float y;

            if (isfindNode)
            {
                y = carPos.y * 100;
            }
            else
            {
                y = carPos.y * 100 + 80;
            }

            if (carInfoComp.Group == 0)
            {
                y += 20;
            }

            ((RectTransform)self.transform).anchoredPosition = new Vector2(x, y);
        }

        public static void RefreshInfo(this ViewCarInfoItem self)
        {
            var carInfoComp = self.CarInfoComponent;

            //玩家信息
            bool isPlayerFirst = carInfoComp.PlayerIds.Count > 0 && carInfoComp.Group == 0;
            bool isLongName = carInfoComp.Name.Length > 2;
            int longNameSize = carInfoComp.Name.Length == 3 ? 32 : 31;

            //车辆铭牌
            if (isPlayerFirst)
            {
                self.NameplateType =
                    isLongName ? ViewCarInfoItemNameplateType.PlayerLong : ViewCarInfoItemNameplateType.Player;
            }
            else
            {
                self.NameplateType =
                    isLongName ? ViewCarInfoItemNameplateType.NobodyLong : ViewCarInfoItemNameplateType.Nobody;
            }

            self.UIPlayerNodeImage.gameObject.SetActive(false);
            self.UIPlayerLongNodeImage.gameObject.SetActive(false);
            self.UINobodyNodeImage.gameObject.SetActive(false);
            self.UINobodyLongNodeImage.gameObject.SetActive(false);

            switch (self.NameplateType)
            {
                case ViewCarInfoItemNameplateType.Player:
                    self.UIPlayerNodeImage.gameObject.SetActive(true);
                    self.UIPlayerNameText.text = carInfoComp.Name;

                    break;
                case ViewCarInfoItemNameplateType.PlayerLong:
                    self.UIPlayerLongNodeImage.gameObject.SetActive(true);
                    self.UIPlayerLongNameText.text = $"<size={longNameSize}>{carInfoComp.Name}</size>";

                    break;
                case ViewCarInfoItemNameplateType.Nobody:
                    self.UINobodyNodeImage.gameObject.SetActive(true);
                    self.UINobodyNameText.text = carInfoComp.Name;

                    break;
                case ViewCarInfoItemNameplateType.NobodyLong:
                    self.UINobodyLongNodeImage.gameObject.SetActive(true);
                    self.UINobodyLongNameText.text = $"<size={longNameSize}>{carInfoComp.Name}</size>";

                    break;
            }

            if (self.MemberPrefabs.Count > 0 && self.MemberPrefabs[0])
            {
                var itemRt =
                    self.MemberPrefabs[0].GetComponent<ViewCarInfoPlayerItem>().ViewHeadItem.transform as RectTransform;
                float size = carInfoComp.Group == 0 ? 52 : 44;
                itemRt.sizeDelta = new(size, size);
            }
        }

        /// <summary>
        /// 刷新成员数据
        /// </summary>
        /// <param name="self"></param>
        public static async UniTask RefreshMembers(this ViewCarInfoItem self)
        {
            if (GameStateCtrl.State < MGGameState.游戏开始)
            {
                self.RefreshCaptainNode();
                return;
            }

            self.UICaptainNodeRectTransform.SetActiveScale(false);
            self.UIMemberNodeHorizontalLayoutGroup.SetActiveScale(true);

            var carInfoComp = self.CarInfoComponent;
            var playerIds = carInfoComp.PlayerIds;
            self.TempMemberIds.Clear();

            int index = 0;
            for (int i = 0; i < playerIds.Count; i++)
            {
                if (index >= 2)
                {
                    break;
                }

                var playerId = playerIds[i];

                if (!(RoomHelper.GetRoomInfoComponent()?.GetPlayerInfoComponent(playerId)?.IsTakeSeat ?? false))
                {
                    continue;
                }

                index++;
                self.TempMemberIds.Add(playerId);
            }

            await UniTask.Delay(300);
            self.RefreshMembersPrefabs();
        }

        /// <summary>
        /// 刷新成员预制
        /// </summary>
        /// <param name="self"></param>
        private static async UniTask RefreshMembersPrefabs(this ViewCarInfoItem self)
        {
            if (self.MemberIds.SequenceEqual(self.TempMemberIds))
            {
                return;
            }

            self.MemberIds = self.TempMemberIds.ToList();
            ObjectPoolManager.Instance.ReturnToPool(self.MemberPrefabs);
            self.MemberPrefabs.Clear();

            for (int i = 0; i < self.MemberIds.Count; i++)
            {
                var obj = await ObjectPoolManager.Instance.GetFromPool<ViewCarInfoPlayerItem>(
                    self.UIMemberNodeHorizontalLayoutGroup.transform);
                var carInfoPlayerItem = obj.GetComponent<ViewCarInfoPlayerItem>();
                await carInfoPlayerItem.OnRefresh(new ViewCarInfoPlayerItemData()
                {
                    PlayerId = self.MemberIds[i],
                    //0主驾 1副驾
                    DriveType = i == 0 ? 0 : 1,
                    CarId = self.TargetEntity,
                });
                self.MemberPrefabs.Add(obj);
            }

            if (self.MemberPrefabs.Count == 2)
            {
                self.MemberPrefabs[0].transform.SetAsLastSibling();
            }
        }

        /// <summary>
        /// 刷新开始前队长位置
        /// </summary>
        /// <param name="self"></param>
        public static void RefreshCaptainNode(this ViewCarInfoItem self)
        {
            self.UIMemberNodeHorizontalLayoutGroup.SetActiveScale(false);

            var carInfoComp = self.CarInfoComponent;
            var playerIds = carInfoComp.PlayerIds;

            if (playerIds.Count == 0)
            {
                self.UICaptainNodeRectTransform.SetActiveScale(false);
                return;
            }

            self.UICaptainNodeRectTransform.SetActiveScale(true);
            (self.UICaptainNodeRectTransform.transform as RectTransform).anchoredPosition =
                new Vector2(self.CaptainNodePosXList[(int)self.NameplateType], 0);

            var playerId = playerIds[0];
            var playerInfoComp = RoomHelper.GetRoomInfoComponent().GetPlayerInfoComponent(playerId);

            self.HeadItem.OnRefresh(new ViewHeadItemData()
            {
                PlayerId = playerId
            });

            self.UICaptainNameText.text = playerInfoComp.Name;
        }

        /// <summary>
        /// 打开护盾
        /// </summary>
        /// <param name="self"></param>
        public static void OpenShield(this ViewCarInfoItem self) => self.UIShieldNodeRectTransform.SetActiveScale(true);

        /// <summary>
        /// 关闭护盾
        /// </summary>
        /// <param name="self"></param>
        public static void CloseShield(this ViewCarInfoItem self) =>
            self.UIShieldNodeRectTransform.SetActiveScale(false);

        /// <summary>
        /// 护盾破碎动画
        /// </summary>
        /// <param name="self"></param>
        public static async UniTask DoPlayShieldBreakAnimation(this ViewCarInfoItem self)
        {
            string name = "fx_ui_ViewCarInfoItem_Shield_Break";

            self.ShieldAnimation.Play(name);

            int ms = (int)(self.ShieldAnimation.GetAnimationLength(name) * 1000);
            await UniTask.Delay(ms);

            var carInfoComp = self.CarInfoComponent;
            if (carInfoComp.GetState() != State.Invincible)
            {
                self.CloseShield();
            }
        }

        /// <summary>
        /// 刷新护盾
        /// </summary>
        /// <param name="self"></param>
        public static void RefreshShield(this ViewCarInfoItem self)
        {
            var shield = self.CarInfoComponent.Shield;
            self.UIShieldText.text = shield.ToString();
        }

        /// <summary>
        /// 护盾动画
        /// </summary>
        /// <param name="self"></param>
        /// <param name="name"></param>
        public static void DoPlayShieldAnimation(this ViewCarInfoItem self, string name)
        {
            if (!self?.ShieldAnimation)
            {
                return;
            }

            self.ShieldAnimation.Play(name);
        }

        /// <summary>
        /// 加速动画
        /// </summary>
        /// <param name="self"></param>
        public static void DoPlayFastAnim(this ViewCarInfoItem self)
        {
            self.ViewCarAnimation.Play("fx_ui_ViewCarInfoItem_Accelerate");

            self.UINobodyNodeImage.transform.DOScale(0.87f, 0.045f).SetLoops(-1, LoopType.Yoyo);
            self.UINobodyLongNodeImage.transform.DOScale(0.87f, 0.045f).SetLoops(-1, LoopType.Yoyo);

            self.UIPlayerNodeImage.transform.DOScale(0.92f, 0.05f).SetLoops(-1, LoopType.Yoyo);
            self.UIPlayerLongNodeImage.transform.DOScale(0.915f, 0.05f).SetLoops(-1, LoopType.Yoyo);
        }

        /// <summary>
        /// 关闭加速动画
        /// </summary>
        /// <param name="self"></param>
        public static void DoCloseFastAnim(this ViewCarInfoItem self)
        {
            self.ViewCarAnimation.Play("fx_ui_ViewCarInfoItem_Normal");

            self.UINobodyNodeImage.transform.DOKill();
            self.UINobodyNodeImage.transform.localScale = Vector3.one * 0.85f;

            self.UINobodyLongNodeImage.transform.DOKill();
            self.UINobodyLongNodeImage.transform.localScale = Vector3.one * 0.85f;

            self.UIPlayerNodeImage.transform.DOKill();
            self.UIPlayerNodeImage.transform.localScale = Vector3.one * 0.9f;

            self.UIPlayerLongNodeImage.transform.DOKill();
            self.UIPlayerLongNodeImage.transform.localScale = Vector3.one * 0.9f;
        }

        /// <summary>
        /// 刷新道具时间进度条
        /// </summary>
        public static void RefreshProgress(this ViewCarInfoItem self, float current = 0)
        {
            if (current <= 0)
            {
                self.UIItemProgressBgImage.gameObject.SetActive(false);
                self.UIItemProgressImage.fillAmount = 1;
                return;
            }

            self.UIItemProgressBgImage.gameObject.SetActive(true);
            self.UIItemProgressImage.fillAmount = current;
        }

        /// <summary>
        /// 落座效果
        /// </summary>
        /// <param name="self"></param>
        /// <param name="playerId"></param>
        public static async UniTask DoTakeSeatView(this ViewCarInfoItem self, string playerId)
        {
            var obj = await ObjectPoolManager.Instance.GetFromPool<ViewTakeSeatItem>(self.transform);
            obj.transform.localPosition = new Vector3(obj.transform.localPosition.x, obj.transform.localPosition.y - 80,
                obj.transform.localPosition.z);
            obj.GetComponent<ViewTakeSeatItem>().OnRefresh(new ViewTakeSeatItemData()
            {
                PlayerId = playerId,
            });
            await UniTask.Delay(1200);
            ObjectPoolManager.Instance.ReturnToPool(obj);
        }

        /// <summary>
        /// 车辆铭牌动画
        /// </summary>
        /// <param name="self"></param>
        /// <param name="name"></param>
        public static void DoPlayViewCarAnimation(this ViewCarInfoItem self, string name)
        {
            if (!self?.ViewCarAnimation)
            {
                return;
            }

            self.ViewCarAnimation.Play(name);
        }

        private static (bool, Vector3) GetPosition(this ViewCarInfoItem self)
        {
            var pos = self.CarViewComponent.Car.transform.position;
            bool isFindNode = false;

            if (self.CarViewComponent.CarCtrl.effectPoints.TryGetValue("Hud", out var effectPoint))
            {
                pos = effectPoint.position;
                isFindNode = true;
            }

            return (isFindNode, pos);
        }

        #endregion
    }
}