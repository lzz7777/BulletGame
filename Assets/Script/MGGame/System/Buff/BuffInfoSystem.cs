using System;
using cfg;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XN
{
    public static class BuffInfoSystem
    {
        [UpdateSystem]
        public static void Update(this BuffInfoComponent self, float deltaTime)
        {
            self.Time += deltaTime;

            if (self.Time > self.EndTime && !Mathf.Approximately(self.EndTime, -1))
            {
                self.IsDiscard = true;
                self.Entity.GetParent().GetComponent<CarInfoComponent>().RemoveBuff(self.BuffId);

                EntityManager.Instance.RemoveEntity(self.Entity);
                return;
            }

            self.CheckFunction();
        }

        public static void OnDestroySystem(this BuffInfoComponent self)
        {
            self.DoRestoreChangeValue();
        }

        public static void Init(this BuffInfoComponent self)
        {
            var buffConf = TotalConfigManager.ConfigManager.BuffIndexConfigCategory.Get(self.BuffId);
            var carInfoComp = self.Entity.GetParent().GetComponent<CarInfoComponent>();

            //实行状态变化
            carInfoComp.SwitchState(buffConf.StateValue, buffConf);

            //添加方法
            self.AddFunction();
        }

        /// <summary>
        /// 添加方法
        /// </summary>
        /// <param name="self"></param>
        public static void AddFunction(this BuffInfoComponent self)
        {
            var buffConf = TotalConfigManager.ConfigManager.BuffIndexConfigCategory.Get(self.BuffId);
            var funcs = TotalConfigManager.ConfigManager.FactionGroupConfigCategory.FactionGroupDic[buffConf.Faction];

            foreach (var funcConf in funcs)
            {
                var startTime = funcConf.StartTime;

                FunctionData funcData = new();
                funcData.FunctionId = funcConf.FactionId;
                funcData.GroupId = funcConf.GroupId;

                //有时间间隔
                if (funcConf.Interval != 0)
                {
                    for (int i = 0; i < funcConf.ActiveNumber; i++)
                    {
                        float time = startTime + funcConf.Interval * i;
                        funcData.TimeQueue.Enqueue(time);
                    }
                }
                else
                {
                    //没有时间间隔
                    float intervalTime = buffConf.Time * 1.0f / funcConf.ActiveNumber;
                    for (int i = 0; i < funcConf.ActiveNumber; i++)
                    {
                        float time = startTime + intervalTime * i;
                        funcData.TimeQueue.Enqueue(time);
                    }
                }

                self.Functions.Add(funcData);
            }
        }

        /// <summary>
        /// 检测执行方法
        /// </summary>
        /// <param name="self"></param>
        public static void CheckFunction(this BuffInfoComponent self)
        {
            foreach (var functionData in self.Functions)
            {
                if (functionData.TimeQueue.Count > 0 && self.Time >= functionData.TimeQueue.Peek())
                {
                    functionData.TimeQueue.Dequeue();
                    self.DoChangeValue(functionData.FunctionId, functionData.GroupId);
                    self.DoEffect(functionData.FunctionId, functionData.GroupId);
                }
            }
        }

        /// <summary>
        /// 执行属性方法
        /// </summary>
        /// <param name="self"></param>
        /// <param name="funcId"></param>
        /// <param name="groupId"></param>
        public static void DoChangeValue(this BuffInfoComponent self, int funcId, int groupId)
        {
            //执行参数变化
            var funcConf = TotalConfigManager.ConfigManager.FactionGroupConfigCategory.Get(funcId, groupId);
            var carUnit = self.Entity.GetParent();
            var carInfoComp = carUnit.GetComponent<CarInfoComponent>();
            var carViewComp = carUnit.GetComponent<CarViewComponent>();
            var carState = carInfoComp.GetState();
            var buffConf = TotalConfigManager.ConfigManager.BuffIndexConfigCategory.Get(self.BuffId);
            var roomInfoComp = RoomHelper.GetRoomInfoComponent();
            var playerInfoComp = roomInfoComp.GetPlayerInfoComponent(self.PlayerId);

            float lastMileage = carInfoComp.Mileage;
            float lastShield = carInfoComp.Shield;

            foreach (var buffChange in funcConf.ChangeValue)
            {
                bool canDo =
                    !TotalConfigManager.ConfigManager.BuffMutexConfigCategory.MutexDic[carState][buffChange.Type];
                if (!canDo)
                {
                    continue;
                }

                Debug.Log($"执行 {buffChange.Type} {buffChange.Value}");

                float changeValue = buffChange.Value;

                switch (buffChange.Type)
                {
                    case ChangeType.SpeedAddPct:
                        // playerInfoComp.AddMileage(buffChange.Value * buffConf.Time);

                        carInfoComp.ExtraSpeedPct += buffChange.Value;
                        break;
                    case ChangeType.SpeedAddValue:
                        //道具持续时间为-1时，需要用 比赛剩余时间*速度
                        if (Mathf.Approximately(buffConf.Time, -1))
                        {
                            float rts = Math.Max(0, roomInfoComp.EndTime - roomInfoComp.Time);
                            playerInfoComp.AddMileage(buffChange.Value * rts);
                        }
                        else
                        {
                            playerInfoComp.AddMileage(buffChange.Value * buffConf.Time);
                        }

                        carInfoComp.ExtraSpeedVale += buffChange.Value;
                        break;
                    case ChangeType.MileageAddPct:
                        changeValue = carInfoComp.Mileage * buffChange.Value;

                        playerInfoComp.AddMileage(changeValue);
                        carInfoComp.AddMileage(changeValue);
                        break;
                    case ChangeType.MileageAddValue:
                        playerInfoComp.AddMileage(buffChange.Value);
                        carInfoComp.AddMileage(buffChange.Value);
                        break;
                    case ChangeType.SpeedDelPct:
                        carInfoComp.ExtraSpeedPct -= buffChange.Value;
                        break;
                    case ChangeType.SpeedDelValue:
                        carInfoComp.ExtraSpeedVale -= buffChange.Value;
                        break;
                    case ChangeType.MileageDelPct:
                        changeValue = carInfoComp.Mileage * buffChange.Value;

                        carInfoComp.ReduceMileage(changeValue);

                        EventsManager.BroadCast(GameEnum.CarMileageDelEvent, carUnit.Id);
                        break;
                    case ChangeType.MileageDelValue:
                        carInfoComp.ReduceMileage(buffChange.Value);

                        EventsManager.BroadCast(GameEnum.CarMileageDelEvent, carUnit.Id);
                        break;
                    case ChangeType.ShieldAdd:
                        carInfoComp.AddShield(buffChange.Value);
                        break;
                    case ChangeType.ShieldDel:
                        carInfoComp.ReduceShield(buffChange.Value);
                        break;
                }

                self.Mutexes.Add(buffChange);
            }

            int changeMileage = (int)(carInfoComp.Mileage - lastMileage);
            if (changeMileage != 0)
            {
                ChangeType changeType = changeMileage > 0 ? ChangeType.MileageAddValue : ChangeType.MileageDelValue;
                RoomHelper.DoBuffChangeUI(carInfoComp.Entity.Id, changeType, changeMileage);
            }

            if (!Mathf.Approximately(carInfoComp.Shield, lastShield))
            {
                ChangeType changeType = changeMileage > 0 ? ChangeType.ShieldAdd : ChangeType.ShieldDel;
                RoomHelper.DoBuffChangeUI(carInfoComp.Entity.Id, changeType);
            }
        }

        /// <summary>
        /// 回滚参数变化
        /// </summary>
        /// <param name="self"></param>
        public static void DoRestoreChangeValue(this BuffInfoComponent self)
        {
            var carInfoComp = self.Entity.GetParent().GetComponent<CarInfoComponent>();

            foreach (var mutexe in self.Mutexes)
            {
                switch (mutexe.Type)
                {
                    case ChangeType.SpeedAddPct:
                        carInfoComp.ExtraSpeedPct -= mutexe.Value;
                        break;
                    case ChangeType.SpeedAddValue:
                        carInfoComp.ExtraSpeedVale -= mutexe.Value;
                        break;
                    case ChangeType.MileageAddPct:
                        break;
                    case ChangeType.MileageAddValue:
                        break;
                    case ChangeType.SpeedDelPct:
                        carInfoComp.ExtraSpeedPct += mutexe.Value;
                        break;
                    case ChangeType.SpeedDelValue:
                        carInfoComp.ExtraSpeedVale += mutexe.Value;
                        break;
                    case ChangeType.MileageDelPct:
                        break;
                    case ChangeType.MileageDelValue:
                        break;
                    case ChangeType.ShieldAdd:
                        break;
                    case ChangeType.ShieldDel:
                        break;
                }
            }
        }

        /// <summary>
        /// 执行特效
        /// </summary>
        /// <param name="self"></param>
        /// <param name="funcId"></param>
        /// <param name="groupId"></param>
        public static async UniTask DoEffect(this BuffInfoComponent self, int funcId, int groupId)
        {
            await UniTask.CompletedTask;

            var funcConf = TotalConfigManager.ConfigManager.FactionGroupConfigCategory.Get(funcId, groupId);

            if (funcConf.BuffEffect.Count == 0)
            {
                return;
            }

            var carUnit = self.Entity.GetParent();
            var carInfoComp = carUnit.GetComponent<CarInfoComponent>();
            var carViewComp = carUnit.GetComponent<CarViewComponent>();

            //整理数据，载具同特效id，存特效皮肤id大的
            foreach (var buffEffect in funcConf.BuffEffect)
            {
                //一次性特效
                if (EffectHelper.JudgeDisposableEffect(buffEffect.EffectId, buffEffect.EffectSkin))
                {
                    continue;
                }

                //可替换特效
                self.EffectDeviceGroup.TryAdd(buffEffect.DeviceId, new());
                self.EffectDeviceGroup.TryGetValue(buffEffect.DeviceId, out var effectGroup);

                effectGroup.TryGetValue(buffEffect.EffectId, out var effectSkin);
                effectGroup[buffEffect.EffectId] = Mathf.Max(effectSkin, buffEffect.EffectSkin);
            }

            //特效生成
            var effects = BuffHelper.GetBuffEffects(funcId, carInfoComp.GetCarDeviceId());
            foreach (var effect in effects)
            {
                //一次性特效
                if (EffectHelper.JudgeDisposableEffect(effect.EffectId, effect.EffectSkin))
                {
                    self.AddDisposableEffect(effect.EffectId, effect.EffectSkin);
                    continue;
                }

                carInfoComp.AddEffectData(effect);
            }

            carViewComp.RefreshEffect();
        }

        /// <summary>
        /// 一次性特效
        /// </summary>
        /// <param name="self"></param>
        /// <param name="effectId"></param>
        /// <param name="effectSkin"></param>
        private static async UniTask AddDisposableEffect(this BuffInfoComponent self, int effectId, int effectSkin)
        {
            var effConf = TotalConfigManager.ConfigManager.EffectInfoConfigCategory.Get(effectId, effectSkin);
            Transform effectPoint = null;

            if (effConf == null)
            {
                Debug.LogError($"effectId:{effectId} effectSkin:{effectSkin}");
                return;
            }

            var carUnit = self.Entity.GetParent();

            if (effConf.EffectPoint != PointType.A0 && !carUnit.GetComponent(out CarViewComponent carViewComp))
                return;

            //一次性特效也放外面
            effectPoint = RoomManager.Instance.UnitRoot.transform;

            var effectCtrl = await EffectHelper.GetEffect(effConf.EffectRes, effectPoint);
            if (effectCtrl == null)
            {
                return;
            }

            //随机位置
            Vector3 offset = Vector3.zero;
            if (effConf.RandArea != null)
            {
                var randArea = effConf.RandArea;
                offset = new Vector3(UnityEngine.Random.Range(-randArea.X, randArea.X),
                    UnityEngine.Random.Range(-randArea.Y, randArea.Y));
            }

            long carId = carUnit.Id;
            var pos = Vector3.zero;
            if (effConf.EffectPoint == PointType.A0)
            {
                //特效放外面不需要跟车走
                carId = 0;
            }
            else
            {
                carViewComp = carUnit.GetComponent<CarViewComponent>();
                if (carViewComp.CarCtrl.effectPoints.TryGetValue(effConf.EffectPoint.ToString(), out effectPoint))
                {
                    pos = effectPoint.position;
                }
            }

            // 先设置位置，再播放动画和刷新层级，防止出现一帧在原点的闪烁
            effectCtrl.gameObject.transform.position = pos + offset;
            effectCtrl.RefreshLayerOrder(2000);
            effectCtrl.Play(effectId, effectSkin);

            var deUnit = self.Entity.AddChild(EntityType.DisposableEffect);
            var edComp = deUnit.AddComponent<DisposableEffectComponent>();
            edComp.CarId = carId;
            edComp.EffectId = effectId;
            edComp.EffectSkin = effectSkin;
            edComp.EffectCtrl = effectCtrl;
            edComp.Offset = offset;
        }
    }
}