using System;
using System.Collections.Generic;
using ByteDance.CloudSync;
using cfg;
using cfg.Global;
using cfg.Net;
using EnumStringValues;

public enum CommandKey
{
    //快手
    [StringValue("ipc")] Ipc,
    [StringValue("c")] KsToken,

    //抖音
    [StringValue("token")] DyToken,
    [StringValue("screen-height")] ScreenHeight,
    [StringValue("screen-width")] ScreenWidth,
    [StringValue("screen-fullscreen")] Fullscreen,
    [StringValue("cloud-game")] IsCloud
}

public static class CommandLine
{
    private static readonly Dictionary<string, string> ArgsDic = new();
    private static LoginInfoConfigCategory ChannelConfig => TotalConfigManager.ConfigManager.LoginInfoConfigCategory;
    private static ConstConfigCategory ConstConfig => TotalConfigManager.ConfigManager.ConstConfigCategory;

    public static bool Init()
    {
        var commandLineArgs = Environment.GetCommandLineArgs();
        var returnInfo = false;

        LoginInfoConfig loginInfo = ChannelConfig.GetOrDefault(ConstConfig.CurrChannel);
        switch (loginInfo.Channel)
        {
            // case ChannelCmd.快手:
            //     for (var i = 0; i < commandLineArgs.Length; i++)
            //     {
            //         var arg = commandLineArgs[i];
            //         if (arg.StartsWith("-"))
            //         {
            //             arg = commandLineArgs[i].Substring(1, arg.Length - 1).Trim().ToLower();
            //             var val = commandLineArgs[++i];
            //             Debug.Log($"命令行 key：{arg}  val:{val}");
            //             ArgsDic.TryAdd(arg, val);
            //         }
            //     }
            //     returnInfo = true;
            //     break;
            case ChannelCmd.DouYin:
                foreach (var arg in commandLineArgs)
                {
                    if (arg.StartsWith("-"))
                    {
                        var index = arg.IndexOf("=", StringComparison.Ordinal);
                        if (index > -1)
                        {
                            Debug.Log("命令行参数：" + arg);
                            var key = arg.Substring(1, index - 1);
                            var val = arg.Substring(index + 1, arg.Length - index - 1);
                            Debug.Log($"命令行 key：{key}  val:{val}");
                            ArgsDic.TryAdd(key, val);
                        }
                    }
                }
                returnInfo = true;
                break;
            default:
                Debug.LogError($"Channel: {loginInfo.Channel} TODO...");
                break;
        }

        SaveData.UpdateSaveType();
        return returnInfo;
    }

    public static bool HasArg(CommandKey key)
    {
        return ArgsDic.ContainsKey(key.GetStringValue());
    }

    public static string GetArg(CommandKey key)
    {
        if (ArgsDic.TryGetValue(key.GetStringValue(), out var val)) return val;

        return string.Empty;
    }

    public static bool TryGetArg(CommandKey key, out string val)
    {
        return ArgsDic.TryGetValue(key.GetStringValue(), out val);
    }

    /// <summary>
    /// 判断抖音是否为云播放
    /// </summary>
    /// <returns></returns>
    public static bool IsCloudGame()
    {
        LoginInfoConfig loginInfo = ChannelConfig.GetOrDefault(ConstConfig.CurrChannel);
        return loginInfo.Channel is ChannelCmd.DouYin && ICloudSync.Env.IsCloud();
    }
}