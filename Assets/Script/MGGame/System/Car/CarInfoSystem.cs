using System;
using System.Collections.Generic;
using cfg;
using cfg.Fight;
using UnityEngine;

namespace XN
{
    public static class CarInfoSystem
    {
        [UpdateSystem]
        public static void Update(this CarInfoComponent self, float deltaTime)
        {
            if (self.IsDiscard && self.CanMoveY())
            {
                var entity = self.Entity;
                self.Entity.GetComponent<CarPositionComponent>().SetPosY(-10, () => { EntityManager.Instance.RemoveEntity(entity); });
            }

            self.UpdateState(deltaTime);

            if (!GameStateCtrl.IsGaming)
            {
                return;
            }

            self.Mileage = Math.Max(0, self.Mileage + self.GetSpeed() * deltaTime);
            
            self.CarStateMachine.Update();
        }

        /// <summary>
        /// 获取速度
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        private static float GetSpeed(this CarInfoComponent self)
        {
            return (self.Speed + self.ExtraSpeedVale) * (1 + self.ExtraSpeedPct);
        }

        /// <summary>
        /// 加护盾
        /// </summary>
        /// <param name="self"></param>
        /// <param name="value"></param>
        public static void AddShield(this CarInfoComponent self, float value)
        {
            self.Shield += value;

            if (self.Shield > 0 && self.GetState() != State.Invincible)
            {
                self.AddState(State.Invincible, -1);
            }
        }

        /// <summary>
        /// 减护盾
        /// </summary>
        /// <param name="self"></param>
        /// <param name="value"></param>
        public static void ReduceShield(this CarInfoComponent self, float value)
        {
            if (self.GetState() != State.Invincible)
            {
                return;
            }
            
            self.Shield = Mathf.Max(0, self.Shield - value);

            if (self.Shield <= 0)
            {
                self.RemoveState(State.Invincible);
                return;
            }
            
            var carViewComp = self.Entity.GetComponent<CarViewComponent>();
            carViewComp.ViewCarInfoItem.DoPlayShieldAnimation("fx_ui_ViewCarInfoItem_Shield_Hit01");
        }

        /// <summary>
        /// 增加里程
        /// </summary>
        /// <param name="self"></param>
        /// <param name="value"></param>
        public static void AddMileage(this CarInfoComponent self, float value)
        {
            self.Mileage += value;
        }

        /// <summary>
        /// 减少里程
        /// </summary>
        /// <param name="self"></param>
        /// <param name="value"></param>
        public static void ReduceMileage(this CarInfoComponent self, float value)
        {
            self.Mileage = Mathf.Max(0, self.Mileage - value);
        }

        /// <summary>
        /// 设置速度
        /// </summary>
        /// <param name="self"></param>
        /// <param name="value"></param>
        public static void SetSpeed(this CarInfoComponent self, float value)
        {
            self.Speed = value;
        }
        
        /// <summary>
        /// 判断是否可以移动X
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static bool CanMoveX(this CarInfoComponent self)
        {
            if ((self.CarMoveType & CarMoveType.MoveX) == 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 判断是否可以移动Y
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static bool CanMoveY(this CarInfoComponent self)
        {
            if ((self.CarMoveType & CarMoveType.MoveY) == 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 添加移动类型
        /// </summary>
        /// <param name="self"></param>
        /// <param name="move"></param>
        public static void AddMoveType(this CarInfoComponent self, CarMoveType move)
        {
            self.CarMoveType |= move;
        }

        /// <summary>
        /// 删除移动类型
        /// </summary>
        /// <param name="self"></param>
        /// <param name="move"></param>
        public static void RemoveMoveType(this CarInfoComponent self, CarMoveType move)
        {
            self.CarMoveType &= ~move;
        }

        private static readonly State[] _stateValues = (State[])Enum.GetValues(typeof(State));

        /// <summary>
        /// 更新状态
        /// </summary>
        /// <param name="self"></param>
        /// <param name="deltaTime"></param>
        public static void UpdateState(this CarInfoComponent self, float deltaTime)
        {
            State tempState = State.None;

            for (int i = _stateValues.Length - 1; i >= 0; i--)
            {
                (float value, float total) = self.StateDic[_stateValues[i]];

                //-1为持久状态
                if (tempState == State.None && Mathf.Approximately(value, -1))
                {
                    tempState = _stateValues[i];
                }

                if (value <= 0)
                {
                    continue;
                }

                self.StateDic[_stateValues[i]] = (Math.Max(0, value - deltaTime), total);

                //状态有时间获取该状态
                if (tempState == State.None && self.StateDic[_stateValues[i]].Item1 > 0)
                {
                    tempState = _stateValues[i];
                }
            }

            if (tempState == State.None)
            {
                //默认状态
                tempState = GameStateCtrl.IsGameStart ? State.Normal : State.Start;
            }

            if (tempState != self.CarState)
            {
                //切换状态
                self.CarState = tempState;
                self.CarStateMachine.ChangeState(tempState, self.Entity.Id);
            }
        }

        /// <summary>
        /// 获取单位状态
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static State GetState(this CarInfoComponent self) => self.CarState;

        /// <summary>
        /// 添加单位状态, -1持久
        /// </summary>
        /// <param name="self"></param>
        /// <param name="state"></param>
        public static void AddState(this CarInfoComponent self, State state, float time)
        {
            //剩余时间
            var (rts, total) = self.StateDic[state];

            if (time > rts || Mathf.Approximately(time, -1))
            {
                self.StateDic[state] = (time, time);
            }
        }

        /// <summary>
        /// 删除单位状态
        /// </summary>
        /// <param name="self"></param>
        /// <param name="state"></param>
        public static void RemoveState(this CarInfoComponent self, State state)
        {
            self.StateDic[state] = (0,0);
        }

        /// <summary>
        /// 切换类型
        /// </summary>
        /// <param name="self"></param>
        /// <param name="carState"></param>
        /// <param name="InvincibleOff"></param>
        /// <returns></returns>
        public static bool SwitchState(this CarInfoComponent self, State targetState, BuffIndexConfig buffConf)
        {
            //判断互斥
            bool canSwitch =
                TotalConfigManager.ConfigManager.BuffMutexConfigCategory.MutexStateDic[self.GetState()][targetState];

            if (canSwitch)
            {
                self.AddState(targetState, buffConf.Time);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 玩家加入车队
        /// </summary>
        /// <param name="self"></param>
        /// <param name="playerId"></param>
        public static void PlayerJoinCar(this CarInfoComponent self, string playerId)
        {
            self.PlayerIds.Add(playerId);
        }

        /// <summary>
        /// 玩家离开车队
        /// </summary>
        /// <param name="self"></param>
        public static void PlayerExitCar(this CarInfoComponent self, string playerId)
        {
            self.PlayerIds.Remove(playerId);
        }
        
        /// <summary>
        /// 获取载具id
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static int GetCarDeviceId(this CarInfoComponent self)
        {
            int deviceId = self.DeviceId;

            if (self.PlayerIds.Count != 0)
            {
                var playerInfoComp = RoomHelper.GetRoomInfoComponent().GetPlayerInfoComponent(self.PlayerIds[0]);
                int playerSkin = playerInfoComp.GetPlayerSkin();
                if (playerSkin != 0)
                {
                    var itemConf = TotalConfigManager.ConfigManager.ItemInfoConfigCategory.GetOrDefault(playerSkin);
                    if (itemConf != null)
                    {
                        deviceId = itemConf.TypeValue[0];
                        return deviceId;
                    }
                    
                    //备用皮肤
                    var tempSkin = TotalConfigManager.ConfigManager.ConstConfigCategory.CustomizedSpareSkin;
                    var tempItemConf = TotalConfigManager.ConfigManager.ItemInfoConfigCategory.GetOrDefault(tempSkin);
                    if (tempItemConf != null)
                    {
                        deviceId = tempItemConf.TypeValue[0];
                        return deviceId;
                    }
                }
            }

            return deviceId;
        }

        /// <summary>
        /// 获取车辆特效皮肤，皮肤受排行榜特效皮肤，玩家特效皮肤影响
        /// </summary>
        /// <param name="self"></param>
        /// <param name="effectId"></param>
        /// <returns></returns>
        public static int GetEffectSkin(this CarInfoComponent self, int effectId)
        {
            int effectSkin = 0;

            if (self.PlayerIds.Count != 0)
            {
                var playerInfoComp = RoomHelper.GetRoomInfoComponent().GetPlayerInfoComponent(self.PlayerIds[0]);
                playerInfoComp.Effects.TryGetValue(effectId, out effectSkin);
            }

            return effectSkin;
        }

        /// <summary>
        /// 获取车辆特效
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static Dictionary<int, int> GetEffectGroup(this CarInfoComponent self)
        {
            Dictionary<int, int> effectDic = new();

            //默认车辆特效
            var deviceInfoConf = TotalConfigManager.ConfigManager.DeviceInfoConfigCategory.Get(self.GetCarDeviceId());
            foreach (var effect in deviceInfoConf.NormalEffect)
            {
                effectDic[effect.EffectId] = effect.EffectSkin;
            }

            foreach (var (key, value) in self.EffectGroup)
            {
                int effectSkin = self.GetEffectSkin(key);
                effectSkin = effectSkin != 0 ? effectSkin : value;

                effectDic.TryGetValue(key, out int defaultSkin);
                effectDic[key] = Math.Max(defaultSkin, effectSkin);
            }

            return effectDic;
        }

        /// <summary>
        /// 添加特效数据
        /// </summary>
        /// <param name="self"></param>
        /// <param name="effect"></param>
        public static void AddEffectData(this CarInfoComponent self, Effect effect)
        {
            self.EffectGroup.TryGetValue(effect.EffectId, out int effectSkin);

            //同组特效存id大的
            if (effectSkin == effect.EffectSkin || effect.EffectSkin <= effectSkin)
            {
                return;
            }

            self.EffectGroup[effect.EffectId] = effect.EffectSkin;
        }

        /// <summary>
        /// 刷新特效数据
        /// </summary>
        /// <param name="self"></param>
        public static void RefreshEffectData(this CarInfoComponent self)
        {
            self.EffectGroup.Clear();

            foreach (var buffUnit in self.Entity.GetChildren())
            {
                if (!buffUnit.GetComponent<BuffInfoComponent>(out var buffInfoComp) || buffInfoComp.IsDiscard)
                {
                    continue;
                }

                buffInfoComp.EffectDeviceGroup.TryGetValue(0, out var effectGroup1);
                buffInfoComp.EffectDeviceGroup.TryGetValue(self.GetCarDeviceId(), out var effectGroup2);
                List<Dictionary<int, int>> effectGroups = new() { effectGroup1, effectGroup2 };
                foreach (var effectGroup in effectGroups)
                {
                    if (effectGroup == null)
                    {
                        continue;
                    }

                    foreach (var (buffEffectId, buffEffectSkin) in effectGroup)
                    {
                        self.EffectGroup.TryGetValue(buffEffectId, out int effectSkin);

                        if (buffEffectSkin > effectSkin)
                        {
                            self.EffectGroup[buffEffectId] = buffEffectSkin;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 移除buff
        /// </summary>
        /// <param name="self"></param>
        /// <param name="buffId"></param>
        public static void RemoveBuff(this CarInfoComponent self, int buffId)
        {
            self.RefreshEffectData();

            var carViewComp = self.Entity.GetComponent<CarViewComponent>();
            carViewComp.RefreshEffect();
        }

        /// <summary>
        /// 贡献里程排序
        /// </summary>
        /// <param name="self"></param>
        public static void SortPlayer(this CarInfoComponent self)
        {
            var roomInfoComp = RoomHelper.GetRoomInfoComponent();

            self.PlayerIds.Sort((a, b) =>
            {
                return roomInfoComp.GetPlayerInfoComponent(b).Score
                    .CompareTo(roomInfoComp.GetPlayerInfoComponent(a).Score);
            });
        }

        /// <summary>
        /// 判断是否在车队贡献第一
        /// </summary>
        /// <param name="self"></param>
        /// <param name="targetPid"></param>
        /// <returns></returns>
        public static bool IsFirstPlayerId(this CarInfoComponent self, string targetPid)
        {
            int index = -1;

            for (int i = 0; i < self.PlayerIds.Count; i++)
            {
                if (self.PlayerIds[i] == targetPid)
                {
                    index = i;
                    break;
                }
            }

            return index == 0;
        }

        /// <summary>
        /// 获取车辆层级
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static int GetCarOrder(this CarInfoComponent self) => GameConst.CarLayer + self.Group * 50;
    }
}