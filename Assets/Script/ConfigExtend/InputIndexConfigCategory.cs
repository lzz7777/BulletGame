using System.Collections.Generic;

namespace cfg.Item
{
    public partial class InputIndexConfigCategory
    {
        public readonly Dictionary<InteractiveID, List<InputIndexConfig>> InputIndexConfigDic = new();
        public readonly Dictionary<ECmd, List<InputIndexConfig>> ECmdInputIndexConfigDic = new();
        
        partial void PostInit()
        {
            foreach (var conf in _dataList)
            {
                InputIndexConfigDic.TryAdd(conf.Interactive, new List<InputIndexConfig>());
                InputIndexConfigDic[conf.Interactive].Add(conf);
                
                ECmdInputIndexConfigDic.TryAdd(conf.Cmd, new List<InputIndexConfig>());
                ECmdInputIndexConfigDic[conf.Cmd].Add(conf);
            }
        }
    }
}