using System.Collections.Generic;
using System.Linq;
using cfg;

namespace XN
{
    public static class BuffHelper
    {
        /// <summary>
        /// 执行buff指令
        /// </summary>
        public static void DoBuff(int inputId, string playerId, int buffId)
        {
            //没有开始不执行buff
            if (!GameStateCtrl.IsGameStart)
            {
                return;
            }
            
            var playerInfoComp = RoomHelper.GetRoomInfoComponent().GetPlayerInfoComponent(playerId);
            if (playerInfoComp == null)
            {
                Debug.LogError($"playerId:{playerId} is no exist");
                return;
            }
            
            var buffConf = TotalConfigManager.ConfigManager.BuffIndexConfigCategory.Get(buffId);
            if (buffConf == null)
            {
                Debug.LogError($"buffid:{buffId} is error");
                return;
            }

            long carId = playerInfoComp.CarId;
            if (carId == 0)
            {
                return;
            }

            var targetIds = SelectTargetHelper.SelectTarget(carId, buffConf.TargetType, buffConf.ActiveNumber);
            for (int i = 0; i < targetIds.Count; i++)
            {
                long targetId = targetIds[i];
                AddBuff(playerId, targetId, buffId);
            
                //助力油桶显示
                if (buffId == 101)
                {
                    RoomHelper.DoCarHelp(playerId, carId);
                }
            }
            
            //分数添加
            var inputIndexConf = TotalConfigManager.ConfigManager.InputIndexConfigCategory.Get(inputId);
            int quantity = inputIndexConf.Quantity == 0 ? 1 : inputIndexConf.Quantity;
            float score = inputIndexConf.GetPoint * quantity;
            playerInfoComp.Score += score;
            playerInfoComp.ScoreTime = TimeHelper.GetTimeStampMs();
            RoomHelper.GetRoomInfoComponent().AddScore(score);
            
            var carUnit = EntityManager.Instance.GetEntityById(carId);
            var carInfoComp = carUnit.GetComponent<CarInfoComponent>();
            var carViewComp = carUnit.GetComponent<CarViewComponent>();
            string fristPid = carInfoComp.PlayerIds[0];      
            carInfoComp.SortPlayer();

            if (fristPid != carInfoComp.PlayerIds[0])
            {
                //第一位变动
                carViewComp?.SwitchSkin();
            }
            
            carViewComp?.ViewCarInfoItem.RefreshMembers();
            carViewComp?.RefreshCarTitle();
            
            EventsManager.BroadCast(GameEnum.ViewMatchRankNodeRefreshEvent);
            EventsManager.BroadCast(GameEnum.ViewBattleMainRefreshEvent);
        }

        public static void AddBuff(string playerId, long carId, int buffId)
        {
            var carUnit = EntityManager.Instance.GetEntityById(carId);
            if (carUnit == null)
            {
                Debug.LogWarning($"carId:{carId} is error");
                return;
            }
            
            var buffConf = TotalConfigManager.ConfigManager.BuffIndexConfigCategory.Get(buffId);
            if (buffConf == null)
            {
                Debug.LogError($"buffid:{buffId} is error");
                return;
            }

            Debug.Log($"buffId:{buffId}, playerId:{playerId}, carId:{carId}");

            var buffUnit = carUnit.AddChild(EntityType.Buff);
            var buffComp = buffUnit.AddComponent<BuffInfoComponent>();

            buffComp.BuffId = buffId;
            buffComp.EndTime = buffConf.Time;
            buffComp.PlayerId = playerId;
            buffComp.Init();
        }

        /// <summary>
        /// 根据载具id获取特效数据
        /// </summary>
        /// <param name="factionId"></param>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public static List<Effect> GetBuffEffects(int factionId, int deviceId)
        {
            Dictionary<int, Effect> effects = new();
            var deviceEffectic = TotalConfigManager.ConfigManager.FactionGroupConfigCategory.BuffEffectDic[factionId];
            foreach (var effect in deviceEffectic[0])
            {
                effects[effect.EffectId] = effect;
            }

            if (deviceEffectic.TryGetValue(deviceId, out var tempDeviceEffects))
            {
                foreach (var effect in tempDeviceEffects)
                {
                    effects[effect.EffectId] = effect;
                }
            }

            return effects.Values.ToList();
        }
    }
}