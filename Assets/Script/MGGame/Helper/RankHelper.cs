using System;
using cfg;
using cfg.Item;

namespace XN
{
    public static class RankHelper
    {
        public static int GetRefreshDay(RankEnum rankEnum)
        {
            int day = 0;

            switch (rankEnum)
            {
                case RankEnum.None:
                    break;
                case RankEnum.Day:
                    day = 1;
                    break;
                case RankEnum.Week:
                    day = 7;
                    break;
                case RankEnum.Month:
                    day = 30;
                    break;
                case RankEnum.Forever:
                    break;
                case RankEnum.HalfMonth:
                    day = 14;
                    break;
                case RankEnum.Instant:
                    break;
            }
            
            return day;
        }
        
        public static DateTime GetNextRefreshDay(long endTime, RankEnum rankEnum)
        {
            DateTime dt = TimeHelper.Time2DateTimeMs(endTime);
            DateTime nextDt = dt.ToLocalTime();

            switch (rankEnum)
            {
                case RankEnum.Day:
                    nextDt = dt.AddDays(1);
                    break;
                case RankEnum.Week:
                    nextDt = dt.AddDays(7);
                    break;
                case RankEnum.Month:
                    nextDt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second).AddMonths(1);
                    break;
                case RankEnum.Forever:
                    break;
                case RankEnum.HalfMonth:
                    if (dt.Day < 15)
                    {
                        nextDt = new DateTime(dt.Year, dt.Month, 15, dt.Hour, dt.Minute, dt.Second);
                    }
                    else
                    {
                        nextDt = new DateTime(dt.Year, dt.Month, 1, dt.Hour, dt.Minute, dt.Second).AddMonths(1);
                    }
                    break;
                default:
                    break;
            }

            return nextDt;
        }

        public static string GetHallOfFameFrameResByIndex(int index)
        {
            StarInfoConfigCategory starInfoCc = TotalConfigManager.ConfigManager.StarInfoConfigCategory;
            var oneStarInfo = starInfoCc.DataList.Find(x => x.RankNumber[0] <= index && index <= x.RankNumber[1]);
            //百名开外，或者index是-1不在榜单吧
            return ResHelper.GetIconOrNone(oneStarInfo?.FrameRes);
        }
    }
}