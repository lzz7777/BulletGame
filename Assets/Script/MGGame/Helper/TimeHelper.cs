using System;

namespace XN
{
    public static class TimeHelper
    {
        public const long OneDayTimestampMS = 86400000;

        public static DateTime Time2DateTime(long s) => Time2DateTimeMs(s * 1000);

        public static DateTime Time2DateTimeMs(long ms)
        {
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(ms);
            return dateTimeOffset.ToLocalTime().DateTime;
        }

        /// <summary>
        /// 获取13位时间戳（毫秒）
        /// </summary>
        /// <returns></returns>
        public static long GetTimeStampMs()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }

        /// <summary>
        /// 获取当前时间
        /// </summary>
        /// <returns></returns>
        public static DateTime GetTimeStampDataTime() => Time2DateTimeMs(GetTimeStampMs());

        /// <summary>
        /// 获取当天零点时间
        /// </summary>
        /// <returns></returns>
        public static long GetZeroTimeMs(long serverNow)
        {
            var dataTime = Time2DateTimeMs(serverNow);
            return new DateTimeOffset(new DateTime(dataTime.Year, dataTime.Month, dataTime.Day, 0, 0, 0, DateTimeKind.Local)).ToUnixTimeMilliseconds();
        }
    }
}