using System.Collections.Generic;

namespace cfg.Fight
{
    public partial class FightRoomConfigCategory
    {
        public readonly Dictionary<FightRoomType, List<FightRoomConfig>> FightRoomTypeDic = new ();
        // public static List<FightRoomConfig> GetListByRoomType(FightRoomType type)
        // {
        //     foreach (var VARIABLE in _dataList)
        //     {
        //         
        //     }
        //     return null;
        // }

        partial void PostInit()
        {
            // TODO 需要新建存储数据结构，外面定义， PostInit组织结构

            foreach (var conf in _dataList)
            {
                if (!FightRoomTypeDic.TryGetValue(conf.RoomType, out List<FightRoomConfig> list))
                {
                    list = new List<FightRoomConfig>();
                    FightRoomTypeDic.Add(conf.RoomType, list);
                }
                list.Add(conf);
            }
        }
    }
}