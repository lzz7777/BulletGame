using System.Collections.Generic;
using Newtonsoft.Json;
using ByteDance.CloudSync.TeaSDK;

public static class TeaService
{
    private static readonly TeaSdk sdk;

    // Start is called before the first frame update
    static TeaService()
    {
        sdk = new TeaSdk();
    }

    public static void Init(int appId, string uuid, string appChannel, string appVersion)
    {
        sdk.Init(appId, uuid, appChannel, appVersion);
    }

    public static void Report(string eventName, Dictionary<string, string> eventParams)
    {
        var json = JsonConvert.SerializeObject(eventParams);

        sdk.Collect(eventName, json, null);
    }
}