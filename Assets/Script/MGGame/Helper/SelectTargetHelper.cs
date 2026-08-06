using System.Collections.Generic;
using cfg;
using UnityEngine;

namespace XN
{
    public static class SelectTargetHelper
    {
        public static List<long> SelectTarget(long carId, Target target, int targetNum)
        {
            List<long> targetIds = new();
            var otherIds = GetOtherIds(carId);

            switch (target)
            {
                case Target.MySelf:
                    targetIds.Add(carId);
                    break;
                case Target.Random:
                    targetIds = GetRandomIds(otherIds, targetNum);
                    break;
                case Target.Top:
                    targetIds = otherIds.GetRange(0, targetNum);
                    break;
            }

            return targetIds;
        }

        private static List<long> GetOtherIds(long carId)
        {
            List<long> otherIds = new();
            foreach (var id in RoomHelper.GetCars())
            {
                if (id != carId)
                {
                    otherIds.Add(id);
                }
            }

            return otherIds;
        }

        /// <summary>
        /// 随机获取id，不重复
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="tempIds"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        private static List<long> GetRandomIds(List<long> tempIds, int count)
        {
            List<long> ids = new();

            for (int i = 0; i < count; i++)
            {
                int randNum = Random.Range(0, tempIds.Count);
                ids.Add(tempIds[randNum]);
                tempIds.RemoveAt(randNum);
            }
            
            return ids;
        }
    }
}