using System;
using System.Collections.Generic;
using UnityEngine.Events;

/// <summary>
/// 事件管理类
/// </summary>
public class EventsManager : EventsBase
{
}

public class EventsBase
{
    /// <summary>
    /// 调试模式,打印为空等
    /// </summary>
    public static readonly bool Debug = false;

    private static readonly Dictionary<string, Delegate> mEventTable = new();

    private static void Throw(object str)
    {
        if (!Debug) return;
        global::Debug.LogWarning(str);
    }

    #region NoParamter

    /// <summary>
    /// 添加事件
    /// </summary>
    public static void AddListener(Enum eventType, UnityAction unityAction)
    {
        OnListenerAdding(eventType, unityAction, out var key);
        mEventTable[key] = (UnityAction)mEventTable[key] + unityAction;
    }

    /// <summary>
    /// 移除事件
    /// </summary>
    public static void RemoveListener(Enum eventType, UnityAction unityAction)
    {
        OnListenerRemoving(eventType, unityAction, out var key);
        if (mEventTable.ContainsKey(key)) mEventTable[key] = (UnityAction)mEventTable[key] - unityAction;

        OnListenerRemoved(eventType);
    }

    /// <summary>
    /// 事件的广播
    /// </summary>
    public static void BroadCast(Enum eventType)
    {
        var key = GetKey(eventType);
        if (mEventTable.TryGetValue(key, out var del))
            if (del is UnityAction unityAction)
            {
                unityAction();
                return;
            }

        Throw($"广播事件错误，对应事件为空: {key}");
    }

    #endregion

    #region One Paramter

    //添加事件
    public static void AddListener<T>(Enum eventType, UnityAction<T> unityAction)
    {
        OnListenerAdding(eventType, unityAction, out var key);
        mEventTable[key] = (UnityAction<T>)mEventTable[key] + unityAction;
    }

    /// <summary>
    /// 移除事件
    /// </summary>
    public static void RemoveListener<T>(Enum eventType, UnityAction<T> unityAction)
    {
        OnListenerRemoving(eventType, unityAction, out var key);
        if (mEventTable.ContainsKey(key)) mEventTable[key] = (UnityAction<T>)mEventTable[key] - unityAction;

        OnListenerRemoved(eventType);
    }

    /// <summary>
    /// 事件的广播
    /// </summary>
    public static void BroadCast<T>(Enum eventType, T arg)
    {
        try
        {
            var key = GetKey(eventType);
            if (mEventTable.TryGetValue(key, out var del))
                if (del is UnityAction<T> unityAction)
                {
                    unityAction(arg);
                    return;
                }

            Throw($"广播事件错误，对应事件为空: {key}");
        }
        catch (Exception e)
        {
            Throw(e);
            throw;
        }
    }

    #endregion

    #region Two Paramters

    /// <summary>
    /// 添加事件
    /// </summary>
    public static void AddListener<T, TX>(Enum eventType, UnityAction<T, TX> unityAction)
    {
        OnListenerAdding(eventType, unityAction, out var key);
        mEventTable[key] = (UnityAction<T, TX>)mEventTable[key] + unityAction;
    }

    /// <summary>
    /// 移除事件
    /// </summary>
    public static void RemoveListener<T, TX>(Enum eventType, UnityAction<T, TX> unityAction)
    {
        OnListenerRemoving(eventType, unityAction, out var key);
        mEventTable[key] = (UnityAction<T, TX>)mEventTable[key] - unityAction;
        OnListenerRemoved(key);
    }

    /// <summary>
    /// 事件的广播
    /// </summary>
    public static void BroadCast<T, TX>(Enum eventType, T arg1, TX arg2)
    {
        var key = GetKey(eventType);
        if (mEventTable.TryGetValue(key, out var del))
            if (del is UnityAction<T, TX> unityAction)
            {
                unityAction(arg1, arg2);
                return;
            }

        Throw($"广播事件错误，对应事件为空: {key}");
    }

    #endregion

    #region Three Paramters

    /// <summary>
    /// 添加事件
    /// </summary>
    public static void AddListener<T, TX, TZ>(Enum eventType, UnityAction<T, TX, TZ> unityAction)
    {
        OnListenerAdding(eventType, unityAction, out var key);
        mEventTable[key] = (UnityAction<T, TX, TZ>)mEventTable[key] + unityAction;
    }

    /// <summary>
    /// 移除事件
    /// </summary>
    public static void RemoveListener<T, TX, TZ>(Enum eventType, UnityAction<T, TX, TZ> unityAction)
    {
        OnListenerRemoving(eventType, unityAction, out var key);
        mEventTable[key] = (UnityAction<T, TX, TZ>)mEventTable[key] - unityAction;
        OnListenerRemoved(key);
    }

    /// <summary>
    /// 事件的广播
    /// </summary>
    public static void BroadCast<T, TX, TZ>(Enum eventType, T arg1, TX arg2, TZ arg3)
    {
        var key = GetKey(eventType);
        if (mEventTable.TryGetValue(key, out var del))
            if (del is UnityAction<T, TX, TZ> unityAction)
            {
                unityAction(arg1, arg2, arg3);
                return;
            }

        Throw($"广播事件错误，对应事件为空: {key}");
    }

    #endregion

    #region Four Paramters

    /// <summary>
    /// 添加事件
    /// </summary>
    public static void AddListener<T, TX, TZ, TY>(Enum eventType, UnityAction<T, TX, TZ, TY> unityAction)
    {
        OnListenerAdding(eventType, unityAction, out var key);
        mEventTable[key] = (UnityAction<T, TX, TZ, TY>)mEventTable[key] + unityAction;
    }

    /// <summary>
    /// 移除事件
    /// </summary>
    public static void RemoveListener<T, TX, TZ, TY>(Enum eventType, UnityAction<T, TX, TZ, TY> unityAction)
    {
        OnListenerRemoving(eventType, unityAction, out var key);
        mEventTable[key] = (UnityAction<T, TX, TZ, TY>)mEventTable[key] - unityAction;
        OnListenerRemoved(key);
    }

    /// <summary>
    /// 事件的广播
    /// </summary>
    public static void BroadCast<T, TX, TZ, TY>(Enum eventType, T arg1, TX arg2, TZ arg3, TY arg4)
    {
        var key = GetKey(eventType);
        if (mEventTable.TryGetValue(key, out var del))
            if (del is UnityAction<T, TX, TZ, TY> unityAction)
            {
                unityAction(arg1, arg2, arg3, arg4);
                return;
            }

        Throw($"广播事件错误，对应事件为空: {key}");
    }

    #endregion

    //精简代码

    #region 精简

    /// <summary>
    /// 添加事件
    /// </summary>
    private static void OnListenerAdding(Enum eventType, Delegate unityAction, out string key)
    {
        key = GetKey(eventType);
        mEventTable.TryAdd(key, null);

        var del = mEventTable[key];
        //判断该事件码对应的事件类型（参数）是否一样
        if (del != null && del.GetType() != unityAction.GetType()) Throw("尝试添加事件失败");
    }

    /// <summary>
    /// 移除事件
    /// </summary>
    private static void OnListenerRemoving(Enum eventType, Delegate unityAction, out string key)
    {
        key = GetKey(eventType);
        if (mEventTable.TryGetValue(key, out var del))
        {
            if (del == null)
                Throw($"移除失败，对应事件为空: {eventType.ToString()}");
            else if (del.GetType() != unityAction.GetType()) Throw("移除失败，对应事件不同");
        }
        else
        {
            Throw($"移除失败，事件码为空: {eventType.ToString()}");
        }
    }

    /// <summary>
    /// 清理为空的事件
    /// </summary>
    private static void OnListenerRemoved(Enum eventType)
    {
        var key = GetKey(eventType);
        if (mEventTable.TryGetValue(key, out var value) && value == null) mEventTable.Remove(key);
    }

    /// <summary>
    /// 清理为空的事件
    /// </summary>
    private static void OnListenerRemoved(string key)
    {
        if (mEventTable[key] == null) mEventTable.Remove(key);
    }

    /// <summary>
    /// 事件ID转字符串
    /// </summary>
    private static string GetKey(Enum eventType)
    {
        var key = $"{eventType.GetType()}.{eventType}";
        return key;
    }

    #endregion
}