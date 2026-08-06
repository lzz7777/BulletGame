//====================================================
//Author:HDS
//Time  :2026/01/14 14:01:55
//Desc  :
//====================================================

using System;
using System.Collections.Generic;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Networking;

namespace XN
{
    public static class SensitiveManager
    {
        private static readonly HashSet<string> _whiteNames = new();

        public static bool IsWhiteName(string name) => _whiteNames is { Count: > 0 } && _whiteNames.Contains(name);

        public static async void Refresh()
        {
            try
            {
                var nameOssPath = @"https://xuanniaoasset.oss-cn-shenzhen.aliyuncs.com/DanMu/name.json";
                var request = UnityWebRequest.Get(nameOssPath);
                await request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string jsonContent = request.downloadHandler.text;
                    // UnityEngine.Debug.LogError(jsonContent);
                    // 直接解析JSON数组（Unity 2017+）
                    string[] result = JsonHelper.FromJson<string>(jsonContent);
                    _whiteNames.AddRange(result);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }
    }

    // JsonHelper类用于处理数组解析
    public static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            // 在JSON字符串外添加包装
            string newJson = "{\"array\":" + json + "}";
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
            return wrapper.array;
        }

        [System.Serializable]
        private class Wrapper<T>
        {
            public T[] array;
        }
    }
}