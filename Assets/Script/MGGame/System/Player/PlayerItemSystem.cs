using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace XN
{
    public static class PlayerItemSystem
    {
        /// <summary>
        /// 刷新背包数据
        /// </summary>
        /// <param name="self"></param>
        /// <param name="bagItems"></param>
        public static void SetItemData(this PlayerItemComponent self, List<BagData> bagItems)
        {
            self.BagDataDict.Clear();
            
            if (bagItems == null || bagItems.Count == 0)
            {
                return;
            }

            foreach (var bagItem in bagItems)
            {
                //判断过期
                if (bagItem.ExpirationAt != 0 && bagItem.ExpirationAt >= TimeHelper.GetTimeStampMs() || bagItem.ItemNum == 0)
                {
                    continue;
                }

                self.BagDataDict[bagItem.ItemId] = bagItem;
            }
        }
        
        public static double GetItemNum(this PlayerItemComponent self, long itemId)
        {
            self.BagDataDict.TryGetValue(itemId, out var bagData);
            return bagData?.ItemNum ?? 0;
        }

        public static void SetItemNum(this PlayerItemComponent self, long itemId, double num)
        {
            num = Math.Max(0, num);
            if (!self.BagDataDict.ContainsKey(itemId))
            {
                self.BagDataDict[itemId] = new BagData()
                {
                    ItemId = itemId
                };
            }
            
            self.BagDataDict[itemId].ItemNum = num;
        }

        /// <summary>
        /// 玩家最终积分
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static double GetFinalScoreNum(this PlayerItemComponent self)
        {
            return self.GetItemNum(GameConst.ScoreId) + self.Entity.GetComponent<PlayerInfoComponent>().WinScore;
        }
        
        /// <summary>
        /// 玩家最终粉丝
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static double GetFinalFansNum(this PlayerItemComponent self)
        {
            return self.GetItemNum(GameConst.FansId) + self.Entity.GetComponent<PlayerInfoComponent>().WinFans;
        }
        
        /// <summary>
        /// 添加秒榜数据
        /// </summary>
        /// <param name="self"></param>
        public static void AddMaximumRangeData(this PlayerItemComponent self)
        {
            var maximumRange = self.GetItemNum(GameConst.KillCount) + 1;
            Debug.Log($"AddMaximumRangeData:{maximumRange}");
            self.SetItemNum(GameConst.KillCount, maximumRange);
        }
    }
}