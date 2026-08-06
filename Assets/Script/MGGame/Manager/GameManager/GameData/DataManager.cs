using System;
using System.Collections.Generic;
using System.Linq;
using Apifox;
using BestHTTP.Extensions;
using BestHTTP.JSON.LitJson;
using cfg;
using cfg.Global;
using cfg.Net;
using Cysharp.Threading.Tasks;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace XN
{
    public static partial class DataManager
    {
        // TODO 拉取失败数据 处理
        // private const string CST_URL_TIKTOK_FAIL_DATA = "ga/api/combat/tiktok/tiktokGiftFailDataGet";
        private static LoginInfoConfigCategory LoginInfoConfig => TotalConfigManager.ConfigManager.LoginInfoConfigCategory;
        private static string BaseUrl => TotalConfigManager.ConfigManager.ConstConfigCategory.BaseUrl ?? LoginInfoConfig.GetOrDefault(Channel).BaseUrl;
        private static ChannelCmd Channel => TotalConfigManager.ConfigManager.ConstConfigCategory.CurrChannel;
        private static bool Dy => Channel is ChannelCmd.DouYin;

        #region 请求底层

        public static async UniTask<T> AsyncSendPost<T>(string url, Dictionary<string, string> headers = null,
            Dictionary<string, string> param = null, Dictionary<string, object> body = null, string bodyStr = "",
            UnityAction<T> callback = null, int attempts = 3)
        {
            var text = await AsyncSendPost(url, headers, param, body, bodyStr, attempts);
            try
            {
                if (string.IsNullOrEmpty(text))
                {
                    Debug.LogError("回调Post数据为空!");
                    return default;
                }

                var jsonData = JsonMapper.ToObject<T>(text);
                callback?.Invoke(jsonData);
                return jsonData;
            }
            catch (Exception e)
            {
                Debug.LogError("回调Post解析错误!");
                Debug.LogError(e);
            }

            return default;
        }


        public static async UniTask<string> AsyncSendPost(string url, Dictionary<string, string> headers = null,
            Dictionary<string, string> param = null, Dictionary<string, object> body = null, string bodyStr = "",
            int attempts = 3)
        {
            if (headers == null) headers = new Dictionary<string, string>();
            if (body != null)
            {
                if (!string.IsNullOrEmpty(bodyStr))
                {
                    Debug.LogError($"有数据 !!! {bodyStr} body也有这样bodyStr会被丢弃");
                }
                bodyStr = JsonMapper.ToJson(body);
            }

            url = url.StartsWith("/") ? $"{BaseUrl}{url}" : $"{BaseUrl}/{url}";

            do
            {
                Debug.Log($"PostUrl {url} Body : {bodyStr}");
                using var webRequest = UnityWebRequest.Post(url, bodyStr, "application/json");
                foreach (var (key, value) in headers)
                {
                    webRequest.SetRequestHeader(key, value);
                }

                try
                {
                    await webRequest.SendWebRequest();
                    if (webRequest.result == UnityWebRequest.Result.Success)
                    {
                        try
                        {
                            var text = webRequest.downloadHandler.text;
                            Debug.Log($"Response - {url} : {text}");
                            if (!string.IsNullOrEmpty(text))
                            {
                                try
                                {
                                    return text;
                                }
                                catch (Exception e)
                                {
                                    Debug.Log($"Error PostUrl {url}  {e}");
                                }
                            }
                            else
                            {
                                Debug.Log($"{url} -- 返回的字符串为空");
                            }

                            // Debug.LogError(webRequest.downloadHandler);

                            break;
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                            break;
                        }
                    }

                    Debug.Log($"Pose {url} -- Response:{webRequest.result}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Post {url} -- {e}");
                }

                await UniTask.Delay(1000);
            } while (--attempts > 0);

            return string.Empty;
        }

        private static async UniTask<T> AsyncSendGet<T>(string url, Dictionary<string, string> headers = null,
            Dictionary<string, string> param = null, UnityAction<T> callback = null, int attempts = 3)
        {
            var text = await AsyncSendGet(url, headers, param, attempts);
            try
            {
                if (string.IsNullOrEmpty(text))
                {
                    Debug.LogError("回调Post数据为空!");
                }

                var jsonData = JsonMapper.ToObject<T>(text);
                callback?.Invoke(jsonData);
                return jsonData;
            }
            catch (Exception e)
            {
                Debug.LogError("回调Get解析错误!");
                Debug.LogError(e);
            }

            return default;
        }

        /// <summary>
        /// 异步请求
        /// </summary>
        /// <param name="url">请求url</param>
        /// <param name="headers">请求头部</param>
        /// <param name="param">请求的参数</param>
        /// <param name="attempts">尝试请求次数</param>
        private static async UniTask<string> AsyncSendGet(string url, Dictionary<string, string> headers = null,
            Dictionary<string, string> param = null, int attempts = 3)
        {
            if (headers == null) headers = new Dictionary<string, string>();
            // if (!string.IsNullOrEmpty(Token)) headers.TryAdd("Authorization", Token);

            url = url.StartsWith("/") ? $"{BaseUrl}{url}" : $"{BaseUrl}/{url}";

            do
            {
                Debug.Log($"GetUrl {url}");
                using var webRequest = UnityWebRequest.Get(url);
                foreach (var (key, value) in headers)
                {
                    webRequest.SetRequestHeader(key, value);
                }

                webRequest.SetRequestHeader("Content-Type", "application/json");

                try
                {
                    await webRequest.SendWebRequest();
                    if (webRequest.result == UnityWebRequest.Result.Success)
                    {
                        try
                        {
                            var text = webRequest.downloadHandler.text;
                            Debug.Log($"{url} -- Response:{text}");
                            if (!string.IsNullOrEmpty(text))
                            {
                                try
                                {
                                    return text;
                                }
                                catch (Exception e)
                                {
                                    Debug.Log($"Error GetUrl {url}  {e}");
                                }
                            }
                            else
                            {
                                Debug.Log($"{url} -- 返回的字符串为空");
                            }

                            Debug.LogError(webRequest.downloadHandler);

                            break;
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                            break;
                        }
                    }

                    Debug.Log($"Get {url} -- Response:{webRequest.result}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Get {url} -- {e}");
                }

                await UniTask.Delay(1000);
            } while (--attempts > 0);

            return string.Empty;
        }

        #endregion
        
        #region 测试案例

        public static async UniTask testFunc()
        {
            var param = new Dictionary<string, object>
            {
                { "PlayerId", "test123" },
                { "Nickname", "1" },
                { "AvatarUrl", "https://avatars.githubusercontent.com/u/10482175" },
            };
            // 参考 DataRank.cs ， 再把相关模块的协议，请求，放一起
            var resp = await AsyncSendPost<RespRetString>(GameConst.Url.Post_GetPlayerInfo, body: param);
            Debug.Log(resp);
            
        }

        #endregion
    }
}