using System.Collections.Generic;

namespace cfg.Fight
{
    public partial class FactionGroupConfigCategory
    {
        public readonly Dictionary<int, List<FactionGroupConfig>> FactionGroupDic = new();
        public Dictionary<int, Dictionary<int, List<Effect>>> BuffEffectDic = new();
        
        partial void PostInit()
        {
            foreach (var conf in _dataList)
            {
                FactionGroupDic.TryAdd(conf.GroupId, new());
                FactionGroupDic[conf.GroupId].Add(conf);

                BuffEffectDic.TryAdd(conf.FactionId, new());
                foreach (var buffEffect in conf.BuffEffect)
                {
                    BuffEffectDic[conf.FactionId].TryAdd(buffEffect.DeviceId, new());
                    BuffEffectDic[conf.FactionId][buffEffect.DeviceId].Add(buffEffect);
                }
            }
        }
    }
}