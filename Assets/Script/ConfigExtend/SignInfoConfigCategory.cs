using System.Collections.Generic;

namespace cfg.Item
{
    public partial class SignInfoConfigCategory
    {
        public readonly Dictionary<(ChannelCmd, string), SignInfoConfig> SignInfoConfigDic = new();

        partial void PostInit()
        {
            foreach (var conf in _dataList)
            {
                SignInfoConfigDic.TryAdd((conf.Channel, conf.ChannelSignID), conf);
            }
        }
    }
}