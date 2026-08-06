// ******************************************************************
// @file       TotalConfigManager.cs
// @brief      配置管理类
// ******************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using cfg;
using cfg.Global;
using cfg.Net;
using Cysharp.Threading.Tasks;
using Luban;
using UnityEngine;
using UnityEngine.Networking;
using YooAsset;
using Random = UnityEngine.Random;
using XN;

public class TotalConfigManager : MonoSingleton<TotalConfigManager>
{
    private static ConstConfigCategory ConstConfig => ConfigManager.ConstConfigCategory;

    private static readonly Dictionary<string, byte[]> ByteConfigDic = new();
    public bool IsLocalLoadOver { get; private set; }
    public bool IsLoadOver { get; private set; }

    private ConfigManager _configManager;
    public string LocalVer { get; private set; }

    private static async UniTask<bool> PreloadAllTablesByAddress()
    {
        await UniTask.WaitUntil(() => YooAssetManager.Instance != null && YooAssetManager.Instance.IsInitialized);

        var package = YooAssets.TryGetPackage("ConfigPackage") ?? YooAssetManager.DefaultPackage;
        bool ok = true;
        var assetInfos = package.GetAssetInfos("Config");
        if (assetInfos == null || assetInfos.Length == 0)
        {
            assetInfos = package.GetAssetInfos("config");
        }
        foreach (var assetInfo in assetInfos)
        {
            var address = assetInfo.Address;
            if (string.IsNullOrEmpty(address)) continue;
            var key = Path.GetFileNameWithoutExtension(address);
            if (ByteConfigDic.ContainsKey(key)) continue;
            var handle = package.LoadAssetAsync<TextAsset>(address);
            await handle.Task;
            if (handle.Status != EOperationStatus.Succeed || handle.AssetObject == null)
            {
                ok = false;
                handle.Release();
                continue;
            }
            var ta = handle.AssetObject as TextAsset;
            if (ta == null || ta.bytes == null || ta.bytes.Length == 0)
            {
                ok = false;
                handle.Release();
                continue;
            }
            ByteConfigDic[key] = ta.bytes;
            handle.Release();
        }
        return ok;
    }

    public GameObject reporter;
    private Reporter repComp;
    
    private static ByteBuf LoadByteBuf(string file)
    {
        if (!ByteConfigDic.TryGetValue(file, out var buf))
        {
            var path = $"{Application.streamingAssetsPath}/config/{file}.bytes";
            if (File.Exists(path))
            {
                buf = File.ReadAllBytes(path);
                ByteConfigDic.TryAdd(file, buf);
            }
            else
            {
                throw new FileNotFoundException($"config bytes not found: {file}", path);
            }
        }

        return new ByteBuf(buf);
    }

    protected override async void OnInit()
    {
        #region 自动识别加载json与bin

        //var tablesCtor = typeof(ConfigManager).GetConstructors()[0];
        // var loaderReturnType = tablesCtor.GetParameters()[0].ParameterType.GetGenericArguments()[1];
        // 根据cfg.Tables的构造函数的Loader的返回值类型决定使用json还是ByteBuf Loader
        // Delegate loader = loaderReturnType == typeof(ByteBuf)
        //     ? new Func<string, ByteBuf>(LoadByteBuf)
        //     : new Func<string, JSONNode>(LoadJson);
        // var tables = (ConfigManager)tablesCtor.Invoke(new object[] { loader });

        #endregion

        await PreloadAllTablesByAddress();
        _configManager = new ConfigManager(LoadByteBuf);
        IsLocalLoadOver = true;

        //读取在线数据
        await LoadNetConfig();
        _configManager = new ConfigManager(LoadByteBuf);

        // 读取本地Json
        await CheckConstConfig();
        // Debug 下性能分析
        InitGameObjectSetting();
        LocalVer = ConfigManager.ConstConfigCategory.Ver;

        Debug.Log("TotalConfigManager OnInitOver");
        IsLoadOver = true;
    }

    protected override void OnRemove()
    {
#if UNITY_EDITOR
            
#else
        Application.logMessageReceived -= OnLog;
#endif
    }

    /// <summary>
    /// 开关实例类型
    /// </summary>
    private void InitGameObjectSetting()
    {
        if (GameConst.DebugType)
        {
#if UNITY_EDITOR
            
#else
            if (repComp == null)
            {
                repComp = Instantiate(reporter)?.GetComponent<Reporter>();
            }
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
#endif
        }
    }
    
    private void OnLog(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            if (GameConst.DebugType && repComp != null)
            {
                repComp?.doShow();
            }
        }
    }

    void OnGUI()
    {
        if (GameConst.DebugType && repComp != null)
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F1)
            {
                if (repComp.show)
                {
                    repComp?.doHide();
                }
                else
                {
                    repComp?.doShow();
                }
            }
        }
    }
    
    private async UniTask<bool> LoadNetConfig()
    {
        var netInfo = ConfigManager.NetInfoConfigCategory.GetOrDefault(ConstConfig.CurrChannel);
#if UNITY_EDITOR
        return false;
#else
        if (!netInfo.LoadNet) {
            return false;
        }
#endif
        // var url = netInfo.NetUrl;
        if (!string.IsNullOrEmpty(netInfo.NetUrl) && netInfo.NetUrl.StartsWith("http"))
        {
            var keys = ByteConfigDic.Keys.ToArray();
            var lst = new List<UniTask<bool>>();
            foreach (var key in keys) lst.Add(LoadByNet(key));

            var bools = await UniTask.WhenAll(lst);
            return bools.All(b => b);
        }

        return false;
    }

    private async UniTask<bool> LoadByNet(string key)
    {
        var netInfo = ConfigManager.NetInfoConfigCategory.GetOrDefault(ConstConfig.CurrChannel);
        // var url = netInfo.NetUrl;
        var attempts = 3;
        while (attempts-- > 0)
        {
            using var webRequest = UnityWebRequest.Get($"{netInfo.NetUrl}/{key}.bytes");
            await webRequest.SendWebRequest();
            if (webRequest.result == UnityWebRequest.Result.Success)
                try
                {
                    var data = webRequest.downloadHandler.data;
                    if (data != null)
                    {
                        ByteConfigDic[key] = webRequest.downloadHandler.data;
                        Debug.Log($"在线配置 {key} 更新完成!!!");
                        return true;
                    }

                    Debug.LogError($"在线配置 {key} 不存在,需要上传!!!");

                    break;
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }

            await UniTask.WaitForSeconds(1);
        }

        if (attempts <= 0) Debug.LogError($"{key} 读取在线配置失败!!!,使用本地配置");

        return false;
    }

    public static string GetHand()
    {
        var path = new[] { "hhr", "sc", "swk", "tc", "xxf", "zbj" };
        return
            $"https://gz-cdn-1258783731.cos.ap-guangzhou.myqcloud.com/LiveGame/vatar/{path[Random.Range(0, path.Length)]}.jpg";
    }

    #region 开放访问

    public static ConfigManager ConfigManager => Instance._configManager;
    public static ChannelCmd Channel => ConstConfig.CurrChannel;

    #endregion

    /// <summary>
    /// 方便等待配置加载
    /// </summary>
    public static async Task Wait()
    {
        if (Instance.IsLoadOver)
            return;

        await UniTask.WaitUntil(() => Instance.IsLoadOver);
    }

    private async UniTask CheckConstConfig()
    {
        if (XN.UIManager.Instance.GameModel == XN.GameModel.Debug)
        {
            var fileName = "ConstConfig.json";
            string fullPath = Path.Combine(Application.streamingAssetsPath, fileName);
            UnityWebRequest request = UnityWebRequest.Get(fullPath);

            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // 获取文件内容
                string jsonContent = request.downloadHandler.text;
                Debug.Log("成功加载JSON文件:\n" + jsonContent);

                var data = JsonUtility.FromJson<ConstConfigCategory>(jsonContent);
                _configManager.ConstConfigCategory.HostAddress = data.HostAddress;
                _configManager.ConstConfigCategory.CurrChannel = data.CurrChannel;
                _configManager.ConstConfigCategory.DebugInt = data.DebugInt;
                GameConst.DebugInt = data.DebugInt;
            }
        }
        else
        {
            Debug.Log("release 版本， 不接受ConstConfig.json 修改");
#if UNITY_EDITOR
            _configManager.ConstConfigCategory.HostAddress = ConstConfigCategory.DebugHost;
#else
            _configManager.ConstConfigCategory.HostAddress = ConstConfigCategory.OnlineHost;
#endif
        }
    }
}
