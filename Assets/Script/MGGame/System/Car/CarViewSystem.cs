using System;
using System.Collections.Generic;
using cfg;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace XN
{
    public static class CarViewSystem
    {
        public static void OnCreateSystem(this CarViewComponent self)
        {
        }

        public static void OnDestroySystem(this CarViewComponent self)
        {
            self.Car.transform.DOKill();

            ObjectPoolManager.Instance.ReturnToPool(self.TrackLightEffect?.gameObject);
            ObjectPoolManager.Instance.ReturnToPool(self.CarCtrl.gameObject);
            ObjectPoolManager.Instance.ReturnToPool(self.Car);
            ObjectPoolManager.Instance.ReturnToPool(self.ViewCarInfoItem.gameObject);
            ObjectPoolManager.Instance.ReturnToPool(self.ViewCarTitleItem?.gameObject);
        }

        public static async UniTask InitSystem(this CarViewComponent self)
        {
            var carPositionComp = self.Entity.GetComponent<CarPositionComponent>();

            var carObj = await ObjectPoolManager.Instance.GetFromPool("Car", RoomManager.Instance.UnitRoot.transform);
            carObj.transform.position = new Vector3(carPositionComp.X, carPositionComp.Y, 0);

            var carInfoObj =
                await ObjectPoolManager.Instance.GetFromPool<ViewCarInfoItem>(RoomManager.Instance.CanvasRoleUI
                    .transform);
            var carInfo = carInfoObj.GetComponent<ViewCarInfoItem>();
            carInfo.TargetEntity = self.Entity.Id;

            self.Car = carObj;
            self.ViewCarInfoItem = carInfo;
            await self.RefreshDevice();

            self.ViewCarInfoItem.Init();
            self.ViewCarInfoItem.RefreshInfo();
            self.ViewCarInfoItem.RefreshMembers();
        }

        /// <summary>
        /// 刷新车辆大小
        /// </summary>
        /// <param name="self"></param>
        /// <param name="scale"></param>
        public static void RefreshCarScaleX(this CarViewComponent self)
        {
            if (self == null)
                return;

            var carInfoComp = self.Entity.GetComponent<CarInfoComponent>();

            //有人大小1。1，没人1
            self.Car.transform.localScale = carInfoComp.PlayerIds.Count > 0 ? Vector3.one * 1.1f : Vector3.one;
        }

        /// <summary>
        /// 刷新载具
        /// </summary>
        /// <param name="self"></param>
        public static async UniTask<bool> RefreshDevice(this CarViewComponent self)
        {
            var carInfoComp = self.Entity.GetComponent<CarInfoComponent>();
            int deviceId = carInfoComp.GetCarDeviceId();
            if (deviceId == self.CurDeviceId)
            {
                return false;
            }

            self.CurDeviceId = deviceId;

            if (self.CarCtrl != null)
            {
                //回收载具特效
                foreach (var effectData in self.EffectGroup.Values)
                {
                    EntityManager.Instance.RemoveEntity(effectData.EffectEntityId);
                }

                self.EffectGroup.Clear();

                ObjectPoolManager.Instance.ReturnToPool(self.CarCtrl.gameObject);

                //回收自定义文本
                ObjectPoolManager.Instance.ReturnToPool(self.ViewCarTitleItem?.gameObject);
                self.ViewCarTitleItem = null;
            }

            //创建载具
            var deviceInfoConf = TotalConfigManager.ConfigManager.DeviceInfoConfigCategory.Get(deviceId);
            var deviceObj = await ObjectPoolManager.Instance.GetFromPool(deviceInfoConf.DeviceRes, self.Car.transform);
            // HDS

            if (!deviceObj.TryGetComponent<CarCtrl>(out var carCtrl))
            {
                carCtrl = deviceObj.AddComponent<CarCtrl>();
                carCtrl.InitData(deviceInfoConf.DeviceRes);
            }
            else
            {
                carCtrl.Reset();
            }

            self.CarCtrl = carCtrl;
            self.UpdateDeviceScale();

            self.CarCtrl.orderCtrl.RefreshLayerOrder(self.Entity.GetComponent<CarInfoComponent>().GetCarOrder());

            self.CarCtrl.skinCtrl.SetSkin(deviceInfoConf.SpineSkin);

            self.SwitchSpine(carInfoComp.GetState());

            return true;
        }

        /// <summary>
        /// 更新车辆大小
        /// </summary>
        /// <param name="self"></param>
        public static void UpdateDeviceScale(this CarViewComponent self)
        {
            if (self == null)
                return;

            var carInfoComp = self.Entity.GetComponent<CarInfoComponent>();
            int deviceId = carInfoComp.GetCarDeviceId();
            var deviceInfoConf = TotalConfigManager.ConfigManager.DeviceInfoConfigCategory.Get(deviceId);

            float extraScale = 1;

            if (GameStateCtrl.IsGameStart && carInfoComp.Group == 0)
            {
                extraScale = GameConst.FirstCarScale;
            }

            self.CarCtrl.transform.localScale = Vector3.one * deviceInfoConf.Size / 10000f * extraScale;
        }

        /// <summary>
        /// 添加特效
        /// </summary>
        /// <param name="self"></param>
        /// <param name="effectId"></param>
        /// <param name="effectSkin"></param>
        /// <returns></returns>
        public static Entity AddEffect(this CarViewComponent self, int effectId, int effectSkin,
            int layerOrder = 0)
        {
            if (self == null)
                return null;

            var effConf = TotalConfigManager.ConfigManager.EffectInfoConfigCategory.Get(effectId, effectSkin);
            Transform effectPoint = null;

            if (effConf == null)
            {
                Debug.LogError($"effectId:{effectId} effectSkin:{effectSkin}");
                return null;
            }

            if (effConf.EffectPoint != PointType.A0)
            {
                self.CarCtrl.effectPoints.TryGetValue(effConf.EffectPoint.ToString(), out effectPoint);
            }

            var effectCtrl = EffectHelper.GetEffect(effConf.EffectRes, effectPoint);
            if (!effectCtrl)
            {
                return null;
            }

            //随机位置
            Vector3 offset = Vector3.zero;
            if (effConf.RandArea != null)
            {
                var randArea = effConf.RandArea;
                offset = new Vector3(UnityEngine.Random.Range(-randArea.X, randArea.X),
                    UnityEngine.Random.Range(-randArea.Y, randArea.Y));
            }

            if (layerOrder == 0)
            {
                layerOrder = self.Entity.GetComponent<CarInfoComponent>().GetCarOrder();
            }

            effectCtrl.RefreshLayerOrder(layerOrder);

            var deUnit = self.Entity.AddChild(EntityType.Effect);
            deUnit.AddComponent<EffectComponent>(comp =>
            {
                comp.EffectId = effectId;
                comp.EffectSkin = effectSkin;
                comp.EffectCtrl = effectCtrl;
                comp.Offset = offset;
                comp.Target = effectPoint;
            });

            return deUnit;
        }

        /// <summary>
        /// 刷新灯带
        /// </summary>
        /// <param name="self"></param>
        public static async UniTask RefreshTrackLight(this CarViewComponent self)
        {
            if (self == null)
                return;

            if (!GameStateCtrl.IsGameStart)
            {
                return;
            }

            ObjectPoolManager.Instance.ReturnToPool(self.TrackLightEffect?.gameObject);

            //根据排名刷新不同灯带
            int rank = RoomHelper.GetCarRank(self.Entity.Id) + 1;

            rank = Math.Min(4, rank);
            string res = "fx_ranktrail_0" + rank;

            self.CarCtrl.effectPoints.TryGetValue(PointType.E5.ToString(), out var effectPoint);
            self.TrackLightEffect = await EffectHelper.GetEffectAsync(res, effectPoint);
            self.TrackLightEffect?.RefreshLayerOrder(self.Entity.GetComponent<CarInfoComponent>().GetCarOrder());
        }

        /// <summary>
        /// 切换动画
        /// </summary>
        /// <param name="self"></param>
        /// <param name="state"></param>
        public static void SwitchSpine(this CarViewComponent self, State state)
        {
            if (!self?.CarCtrl)
            {
                return;
            }

            string tempAnim = "Standby";
            foreach (var conf in TotalConfigManager.ConfigManager.DeviceStateConfigCategory.DataList)
            {
                if (conf.CarState == state)
                {
                    tempAnim = conf.SpineAnimation;
                    break;
                }
            }

            self.CarCtrl.animCtrl.SetAnimation(tempAnim);
        }

        /// <summary>
        /// 车队换皮
        /// </summary>
        public static async UniTask SwitchSkin(this CarViewComponent self)
        {
            await self.RefreshDevice();
            self.Entity.GetComponent<CarInfoComponent>().RefreshEffectData();
            await self.RefreshEffect();
            await self.RefreshTrackLight();
        }

        /// <summary>
        /// 更新特效皮肤
        /// </summary>
        /// <param name="self"></param>
        public static async UniTask RefreshEffect(this CarViewComponent self)
        {
            await UniTask.CompletedTask;

            if (!GameStateCtrl.IsGameStart || self == null)
            {
                return;
            }

            var effectGroup = self.Entity.GetComponent<CarInfoComponent>().GetEffectGroup();

            //回收失效特效
            List<int> invalidEffects = new();
            foreach (var (effectId, effectData) in self.EffectGroup)
            {
                if (!effectGroup.ContainsKey(effectId))
                {
                    invalidEffects.Add(effectId);
                }
            }

            foreach (var effectId in invalidEffects)
            {
                var effectData = self.EffectGroup[effectId];

                EntityManager.Instance.RemoveEntity(effectData.EffectEntityId);

                self.EffectGroup.Remove(effectId);
            }

            //刷新特效
            foreach (var (effectId, effectSkin) in effectGroup)
            {
                if (!self.EffectGroup.TryGetValue(effectId, out var effectData))
                {
                    self.EffectGroup.Add(effectId, effectData = new());
                }

                if (effectData.EffectSkin != effectSkin)
                {
                    //删除旧特效组件
                    if (effectData.EffectEntityId != 0)
                    {
                        EntityManager.Instance.RemoveEntity(effectData.EffectEntityId);
                    }

                    //生成特效
                    var unit = self.AddEffect(effectId, effectSkin);

                    if (unit != null)
                    {
                        effectData.EffectSkin = effectSkin;
                        effectData.EffectEntityId = unit.Id;
                    }
                }
            }
        }

        /// <summary>
        /// 刷新车辆下所以层级，特效层级
        /// </summary>
        /// <param name="self"></param>
        public static async UniTask RefreshAllOrder(this CarViewComponent self)
        {
            if (self == null)
                return;

            var carOrder = self.Entity.GetComponent<CarInfoComponent>().GetCarOrder();

            foreach (var effectData in self.EffectGroup.Values)
            {
                EntityManager.Instance.GetEntityById(effectData.EffectEntityId).GetComponent<EffectComponent>().EffectCtrl?.RefreshLayerOrder(carOrder);
            }

            self.TrackLightEffect?.RefreshLayerOrder(carOrder);

            self.CarCtrl?.orderCtrl.RefreshLayerOrder(carOrder);
        }

        public static void PlayCarTint(this CarViewComponent self)
        {
            self?.CarCtrl?.tintCtrl.Play();
        }

        /// <summary>
        /// 播放受击效果
        /// </summary>
        /// <param name="self"></param>
        public static void DoCarHitAnimation(this CarViewComponent self)
        {
            var carInfoComp = self.Entity.GetComponent<CarInfoComponent>();

            if (!carInfoComp.CanMoveX() || !carInfoComp.CanMoveY())
                return;

            carInfoComp.AddMoveType(CarMoveType.MoveX);
            carInfoComp.AddMoveType(CarMoveType.MoveY);
            float targetX = self.Car.transform.position.x - 1;
            self.Car.transform.DOMoveX(targetX, 0.1f).SetEase(Ease.OutSine).OnComplete(() =>
            {
                carInfoComp.RemoveMoveType(CarMoveType.MoveX);
                carInfoComp.RemoveMoveType(CarMoveType.MoveY);
            });
        }

        /// <summary>
        /// 刷新玩家标签
        /// </summary>
        /// <param name="self"></param>
        public static async UniTask RefreshCarTitle(this CarViewComponent self)
        {
            var carInfoComp = self.Entity.GetComponent<CarInfoComponent>();
            var playerIds = carInfoComp.PlayerIds;
            if (playerIds.Count == 0)
            {
                ObjectPoolManager.Instance.ReturnToPool(self.ViewCarTitleItem?.gameObject);
                self.ViewCarTitleItem = null;
                return;
            }

            var targetPlayerId = playerIds[0];
            var playerInfoComp = RoomHelper.GetRoomInfoComponent().GetPlayerInfoComponent(targetPlayerId);
            string title = playerInfoComp.Title;
            if (string.IsNullOrEmpty(title))
            {
                ObjectPoolManager.Instance.ReturnToPool(self.ViewCarTitleItem?.gameObject);
                self.ViewCarTitleItem = null;
                return;
            }

            if (!self.ViewCarTitleItem)
            {
                self.CarCtrl.effectPoints.TryGetValue(PointType.E2.ToString(), out var effectPoint);
                var go = await ObjectPoolManager.Instance.GetFromPool<ViewCarTitleItem>(effectPoint);
                self.ViewCarTitleItem = go.GetComponent<ViewCarTitleItem>();
            }

            var deviceId = carInfoComp.GetCarDeviceId();
            var deviceConf = TotalConfigManager.ConfigManager.DeviceInfoConfigCategory.Get(deviceId);
            var localPos = Vector3.zero;
            try
            {
                localPos = new Vector3(deviceConf.NameTitlePos[0], deviceConf.NameTitlePos[1], 0);
            }
            catch (Exception e)
            {
                localPos = Vector3.zero;
            }

            self.ViewCarTitleItem.OnRefresh(new ViewCarTitleItemData()
            {
                Title = title,
                Frame = deviceConf.NameTitleBg,
                LocalPosition = localPos
            });
        }
    }
}