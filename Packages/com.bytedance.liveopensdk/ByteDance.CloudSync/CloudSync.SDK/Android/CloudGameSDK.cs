using System;
using System.Collections;
using System.Collections.Generic;
using ByteDance.CloudSync;
using UnityEngine;

namespace ByteDance.CloudSync.CloudGameAndroid
{
    public static class CloudGameSDK
    {
        private const string TAG = "CloudGameSDK";

        private static AndroidJavaObject _sdkobj;
        private static bool _init = false;


        public static void InitPlugin()
        {
            Debug.Log($"[{TAG}] InitPlugin Start");
            try
            {
                var sdkclass = new AndroidJavaClass("com.bytedance.cloudplay.gamesdk.api.ByteCloudGameSdk");
                sdkclass.CallStatic("initPlugin", AndroidContext.ApplicationContext);
            }
            catch (Exception e)
            {
                Debug.Log($"[{TAG}] {e}");
            }

            Debug.Log($"[{TAG}] InitPlugin End");
        }

        public static bool IsPluginLoaded()
        {
            var sdkclass = new AndroidJavaClass("com.bytedance.cloudplay.gamesdk.api.ByteCloudGameSdk");
            return sdkclass.CallStatic<bool>("isPluginLoaded");
        }

        public static void LoadPlugin(OnLoadSuccess onLoadSuccess, OnLoadFailed onLoadFailed)
        {
            Debug.Log($"[{TAG}] LoadPlugin Start");

            try
            {
                var sdkclass = new AndroidJavaClass("com.bytedance.cloudplay.gamesdk.api.ByteCloudGameSdk");
                _sdkobj = sdkclass.CallStatic<AndroidJavaObject>("inst");

                sdkclass.CallStatic("loadPlugin", new LoadPluginCallBackProxy(onLoadSuccess, onLoadFailed));
            }
            catch (Exception e)
            {
                Debug.Log($"[{TAG}] {e}");
            }

            Debug.Log($"[{TAG}] LoadPlugin End");
        }

        public static void InitEnv(string clientId, InitCallBack callback)
        {
            Debug.Log($"[{TAG}] InitEnv Start");
            //sdk的bug,初始化失败时会先调callback(false)再掉callback(true),这里防止被调两次
            bool callbackUsed = false;
            try
            {
                GetCloudGameSdkInstance()?.Call("initEnv", AndroidContext.ApplicationContext, clientId, new InitCallBackProxy(
                    (isSuccess) =>
                    {
                        if (callbackUsed) return;
                        _init = true;
                        callback?.Invoke(isSuccess);
                        callbackUsed = true;
                    }));
            }
            catch (Exception e)
            {
                Debug.Log($"[{TAG}] {e}");
            }

            Debug.Log($"[{TAG}] InitEnv End");
        }

        public static void InitCloudScene(string appId, InitCallBack callback)
        {
            Debug.Log($"[{TAG}] InitCloudScene Start");
            //sdk的bug,初始化失败时会先调callback(false)再掉callback(true),这里防止被调两次
            bool callbackUsed = false;
            try
            {
                GetCloudGameSdkInstance()?.Call("initCloudScene", appId, new InitCallBackProxy((isSuccess) =>
                {
                    if (callbackUsed) return;
                    callback?.Invoke(isSuccess);
                    callbackUsed = true;
                }));
            }
            catch (Exception e)
            {
                Debug.Log($"[{TAG}] {e}");
            }

            Debug.Log($"[{TAG}] InitCloudScene End");
        }

        public static void InitLocalScene(string appId, InitCallBack callback)
        {
            Debug.Log($"[{TAG}] InitLocalScene Start");
            //sdk的bug,初始化失败时会先调callback(false)再掉callback(true),这里防止被调两次
            bool callbackUsed = false;
            try
            {
                GetCloudGameSdkInstance()?.Call("initLocalScene", appId, new InitCallBackProxy((isSuccess) =>
                {
                    if (callbackUsed) return;
                    callback?.Invoke(isSuccess);
                    callbackUsed = true;
                }));
            }
            catch (Exception e)
            {
                Debug.Log($"[{TAG}] {e}");
            }

            Debug.Log($"[{TAG}] InitLocalScene End");
        }

        /// <summary>
        /// 初始化SDK
        /// </summary>
        /// <param name="appId">云游戏appid</param>
        /// <param name="clientId">客户id</param>
        /// <param name="callback">void (bool isSuccess)</param>
        public static void Init(string appId, string clientId, InitCallBack callback)
        {
            Debug.Log($"[{TAG}] Init Start");
            //sdk的bug,初始化失败时会先调callback(false)再掉callback(true),这里防止被调两次
            bool callbackUsed = false;
            try
            {
                GetCloudGameSdkInstance()?.Call("init", AndroidContext.ApplicationContext, appId, clientId, new InitCallBackProxy(
                    (isSuccess) =>
                    {
                        if (callbackUsed) return;
                        _init = true;
                        callback?.Invoke(isSuccess);
                        callbackUsed = true;
                    }));
            }
            catch (Exception e)
            {
                Debug.Log($"[{TAG}] {e}");
            }

            Debug.Log($"[{TAG}] Init End");
        }

        private static void CheckInit()
        {
            if (!_init)
                throw new Exception("You Need Init CloudGameSDK First!");
        }

        public static bool IsCloudInit()
        {
            return _init;
        }

        public static bool IsRunningCloud()
        {
            return LogUtils.WrapExceptionLog(() =>
            {
                CheckInit();
                return _sdkobj.Call<bool>("isRunningCloud");
            }, TAG);
        }

        public static AccountScene GetAccountScene()
        {
            return LogUtils.WrapExceptionLog(() =>
            {
                CheckInit();
                return new AccountScene(_sdkobj.Call<AndroidJavaObject>("getScene",
                    AccountScene.AccountSceneJavaClass));
            }, TAG);
        }

        public static PayScene GetPayScene()
        {
            return LogUtils.WrapExceptionLog(() =>
            {
                CheckInit();
                return new PayScene(_sdkobj.Call<AndroidJavaObject>("getScene", PayScene.PaySceneJavaClass));
            }, TAG);
        }

        public static MultiplayerScene GetMultiplayerScene()
        {
            return LogUtils.WrapExceptionLog(() =>
            {
                // CheckInit();
                return new MultiplayerScene(_sdkobj.Call<AndroidJavaObject>("getScene",
                    MultiplayerScene.MultiplayerSceneJavaClass));
            }, TAG);
        }

        public static string GetExtraInfo()
        {
            if (SdkConsts.IsAndroidPlayer)
            {
                try
                {
                    var sdkConfig = GetCloudGameSdkInstance()?.Call<UnityEngine.AndroidJavaObject>("getSdkConfig");
                    if (sdkConfig == null)
                    {
                        Debug.LogError("GetExtraInfo - getSdkConfig error: sdkConfig is null");
                        return null;
                    }

                    var jsonObj = sdkConfig.Call<UnityEngine.AndroidJavaObject>("getExtra");
                    if (jsonObj == null)
                    {
                        Debug.LogError("GetExtraInfo - getSdkConfig error: getExtra is null");
                        return null;
                    }

                    return jsonObj.Call<string>("toString");
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Error calling Java method: " + e.Message);
                    return null;
                }
            }

            Debug.LogWarning("Not running on Android device");
            return null;
        }

        private static AndroidJavaObject GetCloudGameSdkInstance()
        {
            try
            {
                if (_sdkobj == null)
                {
                    var sdkclass = new AndroidJavaClass("com.bytedance.cloudplay.gamesdk.api.ByteCloudGameSdk");
                    _sdkobj = sdkclass.CallStatic<AndroidJavaObject>("inst");
                }

                return _sdkobj;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Unable to get ByteCloudGameSdk instance, error: {exception}");
                return null;
            }
        }
    }
}