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

namespace GameMain
{
    public class GaCombatConfigVo
    {
        public string exchange;
        public string host;
        public string name;
        public int port;
        public string pwd;
        public string queueName;
        public string routingKey;
        public string virtualHost;
    }

    public class WsCombatConfigVo
    {
        public string ws_url;
    }

    public static partial class DataManager
    {
        private const string CST_URL_TIKTOK_FAIL_DATA = "ga/api/combat/tiktok/tiktokGiftFailDataGet";
        private static NetInfoConfigCategory NetInfoConfig => TotalConfigManager.ConfigManager.NetInfoConfigCategory;
        private static LoginInfoConfigCategory LoginInfoConfig => TotalConfigManager.ConfigManager.LoginInfoConfigCategory;
        // private static ConstConfigCategory ConstConfig => TotalConfigManager.ConfigManager.ConstConfigCategory;

        private static string _recordId;
        private static string _roomId;
        private static string _roomPwd;

        public static string CombatId { get; private set; }

        private static string BaseUrl => LoginInfoConfig.GetOrDefault(Channel).BaseUrl;
        private static bool NoSend { get; set; }

        private static ChannelCmd Channel => TotalConfigManager.ConfigManager.ConstConfigCategory.CurrChannel;
        private static bool Dy => Channel is ChannelCmd.DouYin;
        // private static bool Wx => Channel is ChannelCmd.微信;
        // private static bool Ks => Channel is ChannelCmd.快手;
        // private static bool Sud => Channel is ChannelCmd.SUD;
        // private static bool Game560 => Channel is ChannelCmd.Game560;
        //
        // private static bool KsOrDy => (ChannelCmd.抖音或快手 & Channel) != 0;

        public static void GetFailStatus<T>(UnityAction<RespRet<T>> action, int page = 1, int pageSize = 100)
        {
            var param = new Dictionary<string, string>
            {
                { "combatId", CombatId },
                { "pageNum", page.ToString() },
                { "pageSize", pageSize.ToString() }
            };

            AsyncSendGet(CST_URL_TIKTOK_FAIL_DATA, param: param, callback: action);
        }

        #region 请求底层

        private static async UniTask<T> AsyncSendPost<T>(string url, Dictionary<string, string> headers = null,
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


        private static async UniTask<string> AsyncSendPost(string url, Dictionary<string, string> headers = null,
            Dictionary<string, string> param = null, Dictionary<string, object> body = null, string bodyStr = "",
            int attempts = 3)
        {
            if (headers == null) headers = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(Token)) headers.TryAdd("Authorization", Token);
            if (body != null)
            {
                if (!string.IsNullOrEmpty(bodyStr))
                {
                    Debug.LogError($"有数据 !!! {bodyStr} body也有这样bodyStr会被丢弃");
                }

                bodyStr = JsonMapper.ToJson(body);
            }

            var kv = SortKey(headers, bodyStr, param);
            // LogKV(kv);
            url = url.StartsWith("/") ? $"{BaseUrl}{url}" : $"{BaseUrl}/{url}";

            // if (!string.IsNullOrEmpty(kv))
            // {
            //     url = $"{url}?{kv}";
            // }


            do
            {
                Log($"PostUrl {url} Body : {bodyStr}");
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
                            Log($"Response - {url} : {text}");
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
                                Log($"{url} -- 返回的字符串为空");
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

                    Log($"Pose {url} -- Response:{webRequest.result}");
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
            if (!string.IsNullOrEmpty(Token)) headers.TryAdd("Authorization", Token);

            var kv = SortKey(headers, param: param);
            url = url.StartsWith("/") ? $"{BaseUrl}{url}" : $"{BaseUrl}/{url}";

            if (!string.IsNullOrEmpty(kv))
            {
                url = $"{url}?{kv}";
                LogKV(url);
            }


            do
            {
                Log($"GetUrl {url}");
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
                            Log($"{url} -- Response:{text}");
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
                                Log($"{url} -- 返回的字符串为空");
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

                    Log($"Get {url} -- Response:{webRequest.result}");
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

        #region 加密Key

		//TODO 输入新密钥
        private const string Secret = "";
        private const string ClientId = "game";

        private static string SortKey(Dictionary<string, string> head, string bodyStr = "",
            Dictionary<string, string> param = null)
        {
            var uuid = Guid.NewGuid().ToString();
            var timestamp = DateTimeHelper.Timestamp;
            var kv = "";

            if (param != null)
            {
                var sortParam = new SortedDictionary<string, string>(param);
                var i = 0;
                foreach (var (k, v) in sortParam)
                {
                    if (i++ != 0) kv += "&";
                    kv += $"{k}={v}";
                }
            }

            var val = $"{ClientId}|{uuid}|{timestamp}|{bodyStr}|{kv}|{Secret}";
            LogKV(val);
            var key = val.CalculateMD5Hash().ToUpper();
            head.Add("nonstr", uuid);
            head.Add("timestamp", timestamp.ToString());
            head.Add("sign", key);
            head.Add("clientid", ClientId);
            LogKV(JsonMapper.ToJson(head));
            return kv;
        }

        #endregion
    }
}