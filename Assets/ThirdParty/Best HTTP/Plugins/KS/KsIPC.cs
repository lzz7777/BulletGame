using System;
using System.Runtime.InteropServices;
using AOT;
using BestHTTP.JSON.LitJson;
using UnityEngine;

public class KsIPC
{
    public struct IPCInfo
    {
        public string code;
        public string message;
    }

    public struct IPCData
    {
        public string type;
        public int version;
        public IPCInfo data;
    }

    public enum IPC_CS_TYPE
    {
        CLIENT,
        SERVER,
        UNKNOWN
    };

    [DllImport("kuaishou_ipc.dll")]
    private static extern int InitIpc(string name, int len, IPC_CS_TYPE type);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void DataReceivedCallback(string data, int len, IntPtr user_data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ConnectedCallback(IntPtr user_data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void DisconnectCallback(IntPtr user_data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void LogCallback(string data, int len, IntPtr user_data);

    [DllImport("kuaishou_ipc.dll")]
    private static extern void SetDataReceivedCallback(DataReceivedCallback cb, IntPtr user_data);

    [DllImport("kuaishou_ipc.dll")]
    private static extern void SetConnectedCallback(ConnectedCallback cb, IntPtr user_data);

    [DllImport("kuaishou_ipc.dll")]
    private static extern void SetDisconnectCallback(DisconnectCallback cb, IntPtr user_data);

    [DllImport("kuaishou_ipc.dll")]
    private static extern void SetLogCallback(LogCallback cb, IntPtr user_data);

    public static void Init(string ipc) {
        Debug.Log($"KsIPC ipc:{ipc} action:{InitIpc(ipc, ipc.Length, IPC_CS_TYPE.CLIENT)}");
        SetDataReceivedCallback(_DataReceivedCallback, IntPtr.Zero);
        SetConnectedCallback(_ConnectedCallback, IntPtr.Zero);
        SetDisconnectCallback(_DisconnectCallback, IntPtr.Zero);
        SetLogCallback(_LogCallback, IntPtr.Zero);
    }

    public static string Code { get; private set; }

    /// <summary>
    /// 监听Code
    /// </summary>
    public static event Action<string> CallCode;

    /// <summary>
    /// 监听退出
    /// </summary>
    public static event Action CallQuit;

    [MonoPInvokeCallback(typeof(DataReceivedCallback))]
    private static void _DataReceivedCallback(string data, int len, IntPtr userData) {
        Debug.Log($"ConnectedCallback data:{data} len:{len}");
        if (data.Length < 1) return;
        data = data[..len];
        var info = JsonMapper.ToObject<IPCData>(data);
        switch (info.type) {
            //登录
            case "SC_SET_CODE":
                var code = info.data.code;
                Code = code;
                CallCode?.Invoke(info.data.code);
                break;
            //用户退出
            case "SC_QUIT":
                CallQuit?.Invoke();
                break;
        }
    }

    [MonoPInvokeCallback(typeof(ConnectedCallback))]
    private static void _ConnectedCallback(IntPtr userData) {
        Debug.Log("ConnectedCallback");
    }

    [MonoPInvokeCallback(typeof(DisconnectCallback))]
    private static void _DisconnectCallback(IntPtr userData) {
        Debug.Log("DisconnectCallback");
    }

    [MonoPInvokeCallback(typeof(LogCallback))]
    private static void _LogCallback(string data, int len, IntPtr userData) {
        Debug.Log($"LogCallback data:{data}");
    }
}