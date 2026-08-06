using System.Collections.Generic;
using cfg;

namespace XN
{
    public static class PlayerInfoSystem
    {
        /// <summary>
        /// 判断是否可以执行指令
        /// </summary>
        /// <param name="self"></param>
        /// <param name="inputId"></param>
        /// <returns></returns>
        public static bool CheckDoCmd(this PlayerInfoComponent self, int inputId)
        {
            var inputIndexConf = TotalConfigManager.ConfigManager.InputIndexConfigCategory.Get(inputId);
            if (inputIndexConf == null)
            {
                Debug.LogError($"inputIndexConf: {inputId} is not found");
                return false;
            }
            
            //游戏开始才能加积分，判断数量限制
            if (GameStateCtrl.IsGameStart)
            {
                //单局数量上限
                if ((inputIndexConf.MaxNumOfSingleGame ?? 0) > 0)
                {
                    self.InputSumDic.TryGetValue(inputIndexConf.InputId, out int inputSum);

                    if (inputSum >= inputIndexConf.MaxNumOfSingleGame)
                    {
                        return false;
                    }
                }
                
                //数量限制
                if (inputIndexConf.Quantity > 0)
                {
                    self.InputQuantity.TryGetValue(inputIndexConf.InputId, out int inputQuantity);

                    if (inputQuantity + 1 < inputIndexConf.Quantity)
                    {
                        self.InputQuantity[inputIndexConf.InputId] = inputQuantity + 1;
                        
                        return false;
                    }
                    
                    self.InputQuantity[inputIndexConf.InputId] = 0;
                }

                if ((inputIndexConf.MaxNumOfSingleGame ?? 0) > 0)
                {
                    self.InputSumDic.TryGetValue(inputIndexConf.InputId, out int inputSum);
                    
                    self.InputSumDic[inputIndexConf.InputId] = inputSum + 1;
                }
            }
            
            return true;
        }

        /// <summary>
        /// 添加贡献里程
        /// </summary>
        /// <param name="self"></param>
        /// <param name="mileage"></param>
        public static void AddMileage(this PlayerInfoComponent self, float mileage)
        {
            self.Mileage += mileage;
        }

        /// <summary>
        /// 设置载具皮肤id或特效皮肤
        /// </summary>
        /// <param name="self"></param>
        /// <param name="skinId"></param>
        /// <param name="effects"></param>
        public static void SetSkinData(this PlayerInfoComponent self, int skinId, List<long> effects)
        {
            self.SkinId = skinId;
            
            self.Effects.Clear();
            
            foreach (var effect in effects)
            {
                int itemId = (int)effect;
                var itemConf = TotalConfigManager.ConfigManager.ItemInfoConfigCategory.GetOrDefault(itemId);
                if (itemConf == null)
                {
                    continue;
                }

                int effectId = itemConf.TypeValue[0];
                int effectSkin = itemConf.TypeValue[1];
                self.Effects[effectId] = effectSkin;
            }
        }

        /// <summary>
        /// 设置性别
        /// </summary>
        /// <param name="self"></param>
        /// <param name="sexType"></param>
        public static void SetSex(this PlayerInfoComponent self, SexType sexType)
        {
            self.Sex = sexType;
        }

        /// <summary>
        /// 刷新玩家车辆皮肤特效
        /// </summary>
        /// <param name="self"></param>
        public static void RefreshCarSkin(this PlayerInfoComponent self)
        {
            if (self.CarId == 0)
            {
                return;
            }

            var carUnit = EntityManager.Instance.GetEntityById(self.CarId);
            var carInfoComp = carUnit.GetComponent<CarInfoComponent>();
            var carViewComp = carUnit.GetComponent<CarViewComponent>();

            //不是第一位不执行
            if (!carInfoComp.IsFirstPlayerId(self.PlayerId))
            {
                return;
            }
            
            carViewComp.SwitchSkin();
            carViewComp.RefreshCarTitle();
        }
        
        /// <summary>
        /// 获取玩家对应车辆皮肤
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static int GetPlayerSkin(this PlayerInfoComponent self) => self.SkinId;

        /// <summary>
        /// 完成落座
        /// </summary>
        /// <param name="self"></param>
        public static void FinishTakeSeat(this PlayerInfoComponent self)
        {
            if (self == null)
            {
                return;
            }
            
            self.IsTakeSeat = true;
        }
        
        /// <summary>
        /// 设置玩家标签
        /// </summary>
        /// <returns></returns>
        public static void SetPlayerTitle(this PlayerInfoComponent self, string title)
        {
            var rankData = RoomManager.Instance.GetPlayerRank(RankType.PreviousMonthRank, self.PlayerId);
            
            if (rankData.rankIndex is <= 0 or > 10)
            {
                return;
            }
            
            self.Title = title;
        }

        /// <summary>
        /// 离开车队
        /// </summary>
        /// <param name="self"></param>
        public static void ExitCar(this PlayerInfoComponent self)
        {
            self.CarId = 0;
        }

        /// <summary>
        /// 判断玩家是否加入车队
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static bool CheckJoinCar(this PlayerInfoComponent self)
        {
            return self.CarId != 0;
        }
    }
}