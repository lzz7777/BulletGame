using System;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using CodeStage.AntiCheat.Storage;
using EnumStringValues;
using Newtonsoft.Json;

public static class SaveData
{
    public enum Key
    {
        [StringValue("IHU*7gD%F%R7iyhui56451")] ScaleKey,
        [StringValue("LanguageInfo1")] LanguageInfo,
        [StringValue("o(h7Hjb^%&^1")] LoginKey,

        #region 设置属性
        [StringValue("O(*H*(kLKOKO)J")] AudioVolume,
        [StringValue("O(*H*(kLK*&GHJ")] MusicVolume,
        [StringValue("SettingTop100Anim")] SettingTop100Anim,
        [StringValue("SettingMuteMusic")] MuteMusic,
        [StringValue("SettingMuteAudio")] MuteAudio,
        #endregion

        #region 主页设置
        [StringValue("MapSceneId")] MapSceneId, // 缓存既可，丢弃概念

        #endregion

    }

    private static bool _savePrefs = true;
    private static readonly Dictionary<string, IObscuredType> LocalInfo = new();

    public static void UpdateSaveType()
    {
        _savePrefs = !CommandLine.IsCloudGame();
    }

    public static void DeleteAll()
    {
        ObscuredPrefs.DeleteAll();
        Debug.Log("清理缓存");
    }

    public static bool HasKey(Key key, string expKey = "")
    {
        return ObscuredPrefs.HasKey(GetKeyString(key, expKey));
    }

    public static ObscuredInt GetInt(Key key, ObscuredInt defaultVal = default, string expKey = "")
    {
        var k = GetKeyString(key, expKey);
        if (LocalInfo.TryGetValue(k, out var info)) return (ObscuredInt)info;

        return ObscuredPrefs.Get(k, defaultVal);
    }

    public static void SetInt(Key key, ObscuredInt val, string expKey = "")
    {
        var k = GetKeyString(key, expKey);
        LocalInfo[k] = val;
        if (_savePrefs) ObscuredPrefs.Set(k, val);
    }

    public static ObscuredFloat GetFloat(Key key, ObscuredFloat defaultVal = default, string expKey = "")
    {
        var k = GetKeyString(key, expKey);
        if (LocalInfo.TryGetValue(k, out var info)) return (ObscuredFloat)info;

        return ObscuredPrefs.Get(k, defaultVal);
    }

    public static void SetFloat(Key key, ObscuredFloat val, string expKey = "")
    {
        var k = GetKeyString(key, expKey);
        LocalInfo[k] = val;
        if (_savePrefs) ObscuredPrefs.Set(k, val);
    }

    public static ObscuredString GetString(Key key, ObscuredString defaultVal = default, string expKey = "")
    {
        var k = GetKeyString(key, expKey);
        if (LocalInfo.TryGetValue(k, out var info)) return (ObscuredString)info;

        return ObscuredPrefs.Get<string>(k, defaultVal);
    }

    public static void SetString(Key key, ObscuredString val, string expKey = "")
    {
        var k = GetKeyString(key, expKey);
        LocalInfo[k] = val;
        if (_savePrefs) ObscuredPrefs.Set(k, val);
    }

    public static T GetObject<T>(Key key, string expKey = "")
    {
        var val = GetString(key, expKey: expKey);
        try
        {
            return string.IsNullOrEmpty(val) ? default : JsonConvert.DeserializeObject<T>(val);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return default;
        }
    }

    public static void SetObject<T>(Key key, T val, string expKey = "")
    {
        if (_savePrefs) SetString(key, JsonConvert.SerializeObject(val), expKey);
    }

    private static string GetKeyString(Key key, string expKey = "")
    {
        var keyString = key.GetStringValue();
        if (!string.IsNullOrEmpty(expKey)) keyString += expKey;

        return keyString;
    }
}