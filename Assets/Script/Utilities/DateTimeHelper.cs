using System;
using GameMain;

public static class DateTimeHelper
{
    private const int TimeZone = 8;

    public static readonly DateTime DateTime1970 = new(1970, 1, 1, TimeZone, 0, 0);
    public static readonly TimeSpan TimeSpanWeek = TimeSpan.FromDays(7);
    public static long ServerAndLocalTimeDifference;

    //改为北京时间
    private static DateTime NowBeiJin => DateTime.UtcNow.AddHours(TimeZone);

    public static DateTime NowServer =>
        ServerAndLocalTimeDifference == 0 ? NowBeiJin : DateTime.UtcNow.AddMilliseconds(ServerAndLocalTimeDifference);

    public static TimeSpan Now => NowServer.Subtract(DateTime1970);

    public static long Timestamp => (long)Now.TotalSeconds;

    public static double TimestampMs => Now.TotalMilliseconds;

    /// <summary>
    /// 获取服务器最新时间并初始化
    /// </summary>
    public static async void InitTime()
    {
        await TotalConfigManager.Wait();

        var data = await DataManager.GetTime();
        if (data.code == 0 && long.TryParse(data.data, out var time))
        {
            var ms = DateTime.UtcNow.Subtract(DateTime1970).TotalMilliseconds;
            ServerAndLocalTimeDifference = time - (long)ms;
            Debug.Log(
                $"获取服务时间成功 本地北京时间:{NowBeiJin} 服务器时间 : {NowServer} 时间戳: {time} 本地时间戳:{ms} , 时间差 :{ServerAndLocalTimeDifference}");
        }
    }

    /// <summary>
    /// 获取这周指定星期
    /// </summary>
    /// <param name="week"></param>
    /// <returns></returns>
    public static DateTime NowWeek(DayOfWeek week)
    {
        var now = NowServer;
        while (now.DayOfWeek != week) now = now.AddDays(1);

        return now;
    }

    public static DateTime GetTime(long timeStamp, bool accurateToMilliseconds = false)
    {
        if (accurateToMilliseconds) return DateTimeOffset.FromUnixTimeMilliseconds(timeStamp).LocalDateTime;

        return DateTimeOffset.FromUnixTimeSeconds(timeStamp).LocalDateTime;
    }
}