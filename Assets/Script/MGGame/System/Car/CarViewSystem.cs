using System;
using System.Collections.Generic;
using System.Threading;
using cfg;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Spine;
using Spine.Unity;
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
            //回收载具特效
            foreach (var effectData in self.EffectGroup.Values)
            {
                ObjectPoolManager.Instance.ReturnToPool(effectData.EffectCtrl?.gameObject);
            }

            self.Car.transform.DOKill();
                
            ObjectPoolManager.Instance.ReturnToPool(self.TrackLightEffect?.gameObject);
            ObjectPoolManager.Instance.ReturnToPool(self.CarCtrl.gameObject);
            ObjectPoolManager.Instance.ReturnToPool(self.Car);
            ObjectPoolManager.Instance.ReturnToPool(self.ViewCarInfoItem.gameObject);
            ObjectPoolManager.Instance.ReturnToPool(self.ViewCarTitleItem?.gameObject);
        }

        /// <summary>
        /// 设置位置x
        /// </summary>
        /// <param name="self"></param>
        /// <param name="posX"></param>
        public static void SetPosX(this CarViewComponent self, float posX)
        {
            self.Car.transform.position =
                new Vector3(posX, self.Car.transform.position.y, self.Car.transform.position.z);
        }

        /// <summary>
        /// do动画x
        /// </summary>
        /// <param name="self"></param>
        /// <param name="x"></param>
        /// <param name="time"></param>
        public static void DoMoveX(this CarViewComponent self, float x, float time)
        {
            var carInfoComp = self.Entity.GetComponent<CarInfoComponent>();

            bool canMove = carInfoComp.CanMoveX();
            if (!canMove)
            {
                return;
            }

            carInfoComp.AddMoveType(CarMoveType.MoveX);
            self.Car.transform.DOMoveX(x, time).OnComplete(() => { carInfoComp.RemoveMoveType(CarMoveType.MoveX); });
        }

        /// <summary>
        /// do动画y
        /// </summary>
        /// <param name="self"></param>
        /// <param name="y"></param>
        /// <param name="time"></param>
        public static void DoMoveY(this CarViewComponent self, float y, float time, Action onComplete = null)
        {
            var carInfoComp = self.Entity.GetComponent<CarInfoComponent>();

            bool canMove = carInfoComp.CanMoveY();
            if (!canMove)
            {
                return;
            }

            carInfoComp.AddMoveType(CarMoveType.MoveY);
            self.Car.transform.DOMoveY(y, time).OnComplete(() =>
            {
                carInfoComp.RemoveMoveType(CarMoveType.MoveY);

                onComplete?.Invoke();
            });
        }

        /// <summary>
        /// 刷新车辆大小
        /// </summary>
        /// <param name="self"></param>
        /// <param name="scale"></param>
        public static void RefreshCarScaleX(this CarViewComponent self)
        {
            var carInfoComp = self.Entity.GetComponent<CarInfoComponent>();
            
            //有人大小1。1，没人1
            self.Car.transform.localScale = carInfoComp.PlayerIds.Count > 0 ?  Vector3.one * 1.1f : Vector3.one;
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
                    ObjectPoolManager.Instance.ReturnToPool(effectData.EffectCtrl?.gameObject);
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
            var carInfoComp = self.Entity.GetComponent<CarInfoComponent>();
            int deviceId = carInfoComp.GetCarDeviceId();
            var deviceInfoConf = TotalConfigManager.ConfigManager.DeviceInfoConfigCategory.Get(deviceId);

            float extraScale = 1;

            if (GameStateCtrl.IsGameStart && carInfoComp.Group == 0)
            {
                extraScale =GameConst.FirstCarScale;
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
        public static async UniTask<EffectCtrl> AddEffect(this CarViewComponent self, int effectId, int effectSkin)
        {
            var effConf = TotalConfigManager.ConfigManager.EffectInfoConfigCategory.Get(effectId, effectSkin);
            Transform effectPoint = null;

            if (effConf == null)
            {
                Debug.LogError($"effectId:{effectId} effectSkin:{effectSkin}");
                return null;
            }

            if (effConf.EffectPoint == PointType.A0)
            {
                //放外面
                effectPoint = RoomManager.Instance.UnitRoot.transform;
            }
            else
            {
                self.CarCtrl.effectPoints.TryGetValue(effConf.EffectPoint.ToString(), out effectPoint);
            }

            if (effectPoint == null)
            {
                Debug.LogError($"Effect effectPoint {effConf.EffectPoint} is no exist");
                return null;
            }

            var effectCtrl = await EffectHelper.GetEffect(effConf.EffectRes, effectPoint);
            if (effectCtrl == null)
            {
                return null;
            }

            var effectGo = effectCtrl.gameObject;

            //随机位置
            if (effConf.RandArea != null)
            {
                var randArea = effConf.RandArea;
                effectGo.transform.localPosition = new Vector3(UnityEngine.Random.Range(-randArea.X, randArea.X),
                    UnityEngine.Random.Range(-randArea.Y, randArea.Y), 0);
            }

            var carOrder = self.Entity.GetComponent<CarInfoComponent>().GetCarOrder();
            
            // 先设置位置和层级，再播放动画，防止出现一帧在原点的闪烁
            effectCtrl.RefreshLayerOrder(carOrder);
            effectCtrl.Play(effectId, effectSkin);

            return effectCtrl;
        }

        /// <summary>
        /// 刷新灯带
        /// </summary>
        /// <param name="self"></param>
        public static async UniTask RefreshTrackLight(this CarViewComponent self)
        {
            await UniTask.CompletedTask;

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
            self.TrackLightEffect = await EffectHelper.GetEffect(res, effectPoint);
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
                ObjectPoolManager.Instance.ReturnToPool(effectData.EffectCtrl?.gameObject);
                self.EffectGroup.Remove(effectId);
            }

            //刷新特效
            foreach (var (effectId, effectSkin) in effectGroup)
            {
                self.EffectGroup.TryAdd(effectId, new());

                if (self.EffectGroup[effectId].EffectSkin != effectSkin)
                {
                    //回收特效
                    ObjectPoolManager.Instance.ReturnToPool(self.EffectGroup[effectId].EffectCtrl?.gameObject);

                    //生成特效
                    self.EffectGroup[effectId].EffectSkin = effectSkin;
                    self.EffectGroup[effectId].EffectCtrl = await self.AddEffect(effectId, effectSkin);
                }
            }
        }

        /// <summary>
        /// 刷新车辆下所以层级，特效层级
        /// </summary>
        /// <param name="self"></param>
        public static async UniTask RefreshAllOrder(this CarViewComponent self)
        {
            var carOrder = self.Entity.GetComponent<CarInfoComponent>().GetCarOrder();

            foreach (var effectViewData in self.EffectGroup.Values)
            {
                effectViewData.EffectCtrl?.RefreshLayerOrder(carOrder);
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

            if (!carInfoComp.CanMoveX())
            {
                return;
            }

            carInfoComp.AddMoveType(CarMoveType.MoveX);
            float targetX = self.Car.transform.position.x - 1;
            self.Car.transform.DOMoveX(targetX, 0.1f).SetEase(Ease.OutSine).OnComplete(() =>
            {
                carInfoComp.RemoveMoveType(CarMoveType.MoveX);
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