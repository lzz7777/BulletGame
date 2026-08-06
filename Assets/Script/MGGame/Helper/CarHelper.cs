using System;
using System.Collections.Generic;

namespace XN
{
    public static class CarHelper
    {
        static Dictionary<int, int> _echelonDic = new()
        {
            [1] = 2,
            [2] = 3,
            [3] = 1,
            [4] = 1,
            [5] = 1,
            [6] = 1,
            [7] = 1,
        };
        
        /// <summary>
        /// 通过当前里程计算当前X位置
        /// </summary>
        /// <param name="targetCarId">上一个车队</param>
        /// <param name="carId">当前车队</param>
        /// <param name="sort"></param>
        /// <param name="targetDis">目标距离，判断是否到下一梯队</param>
        /// <param name="groupData"></param>
        /// <returns></returns>
        public static float GetXByMileage(long targetCarId, long carId, int sort, float targetDis, List<List<float>> groupData)
        {
            var firstTarget = TotalConfigManager.ConfigManager.ConstConfigCategory.FirstTarget;
            var lastTarget = TotalConfigManager.ConfigManager.ConstConfigCategory.LastTarget;
            
            //距离百分比
            float valuePct = 1 - firstTarget;
            if (sort == 0)
            {
                groupData.Add(new() { valuePct });
                return GetXByPct(valuePct);
            }

            var lastMileage = Math.Max(0.1f,
                EntityManager.Instance.GetEntityById(targetCarId).GetComponent<CarInfoComponent>().Mileage);
            var mileage = EntityManager.Instance.GetEntityById(carId).GetComponent<CarInfoComponent>().Mileage;
            float teamTarget = TotalConfigManager.ConfigManager.ConstConfigCategory.TeamTarget;
            float tierTarget = TotalConfigManager.ConfigManager.ConstConfigCategory.TierTarget;
            
            //差距
            float dis = lastMileage - mileage;
            float lastValuePct = 0;
            
            //判断能否在一个梯队里
            if (dis <= targetDis)
            {
                int echelon = groupData.Count;
                var targetGroup = groupData[^1];
                if (targetGroup.Count < GetEchelon(echelon))
                {
                    //同个梯队
                    lastValuePct = targetGroup[^1];
                    
                    if (dis == 0)
                    {
                        //上个距离比 - 队列系数
                        valuePct = lastValuePct - teamTarget;
                    }
                    else
                    {
                        //上个距离比 - 差距/上个里程 - 队列系数
                        valuePct = lastValuePct - dis / lastMileage - teamTarget;
                    }
                    
                    valuePct = Math.Max(valuePct, lastTarget);
                    groupData[^1].Add(valuePct);

                    return GetXByPct(valuePct);
                }
            }
            
            //下一梯队
            //上个距离比 - 差距/上个里程 - 梯队差距系数
            lastValuePct = groupData[^1][^1];
            valuePct = lastValuePct - dis / lastMileage - tierTarget;
            
            valuePct = Math.Max(valuePct, lastTarget);
            groupData.Add(new() { valuePct });
    
            return GetXByPct(valuePct);
        }

        private static int GetEchelon(int echelon)
        {
            switch (echelon)
            {
                case 1:
                    return 2;
                case 2:
                    return 3;
            }

            return 1;
        }

        /// <summary>
        /// 从屏幕LastTarget FirstTarget
        /// </summary>
        /// <param name="disPct"></param>
        /// <returns></returns>
        public static float GetXByPct(float disPct)
        {
            float startPos = -GameConst.ScreenWidth / 2.0f;
            float pos = startPos + GameConst.ScreenWidth * disPct;
            
            return pos / 100;
        }

        /// <summary>
        /// 获取最左边位置
        /// </summary>
        /// <returns></returns>
        public static float GetMinPos()
        {
            var lastTarget = TotalConfigManager.ConfigManager.ConstConfigCategory.LastTarget;
            return GetXByPct(lastTarget);
        }
    }
}
