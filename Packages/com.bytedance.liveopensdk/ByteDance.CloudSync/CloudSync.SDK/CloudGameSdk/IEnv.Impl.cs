using System.Collections.Generic;
using ByteDance.CloudSync.CloudGameAndroid;

namespace ByteDance.CloudSync
{
    public class EnvParams
    {
        private static readonly SdkDebugLogger Debug = new(nameof(EnvParams));

        public Dictionary<string, string> Values { get; } = new();

        public void Add(EnvParams other)
        {
            foreach (var (k, v) in other.Values)
            {
                if (Values.TryGetValue(k, out var value))
                {
                    Debug.Log($"Override string value, key: {k}, old-value: '{value}', new-value: '{v}'");
                }
                Values[k] = v;
            }
        }

        public EnvParams WithValue(string key, string value)
        {
            Values.Add(key, value);
            return this;
        }

        public override string ToString()
        {
            var list = new List<string>();
            foreach (var (k, v) in Values)
            {
                list.Add($"{k}={v}");
            }

            return string.Join("\n", list);
        }
    }

    /// <summary>
    /// 用于子类继承，简化代理实现
    /// </summary>
    internal abstract class ProxyEnv : IWritableEnv
    {
        protected IWritableEnv Inner { get; set; }

        public bool HasKey(string key) => Inner.HasKey(key);

        public bool TryGetIntValue(string key, out int value) => Inner.TryGetIntValue(key, out value);

        public bool TryGetStringValue(string key, out string value) => Inner.TryGetStringValue(key, out value);

        public void SetValue(string key, string value) => Inner.SetValue(key, value);
    }

    /// <summary>
    /// 两个 Env 合并后的 Env，优先用第一个 Env 的值
    /// </summary>
    internal class OverrideEnv : IWritableEnv
    {
        private readonly IWritableEnv _overrider;
        private readonly IWritableEnv _basic;

        public OverrideEnv(IWritableEnv overrider, IWritableEnv basic)
        {
            _overrider = overrider;
            _basic = basic;
        }

        public bool HasKey(string key) => _overrider.HasKey(key) || _basic.HasKey(key);

        public bool TryGetIntValue(string key, out int value)
        {
            return _overrider.TryGetIntValue(key, out value) || _basic.TryGetIntValue(key, out value);
        }

        public bool TryGetStringValue(string key, out string value)
        {
            return _overrider.TryGetStringValue(key, out value) || _basic.TryGetStringValue(key, out value);
        }

        public void SetValue(string key, string value)
        {
            _overrider.SetValue(key, value);
        }
    }

    /// <summary>
    /// 基础的 Env 实现
    /// </summary>
    internal class BasicEnv : IWritableEnv
    {
        private static readonly SdkDebugLogger Debug = new(nameof(BasicEnv));

        private readonly EnvParams _envParams;

        public BasicEnv(EnvParams envParams = null)
        {
            _envParams = envParams ?? new EnvParams();
        }

        public bool HasKey(string key)
        {
            return _envParams.Values.ContainsKey(key);
        }

        public bool IsEmpty()
        {
            return _envParams.Values.Keys.Count == 0;
        }

        public bool TryGetIntValue(string key, out int value)
        {
            if (_envParams.Values.TryGetValue(key, out var str))
                return int.TryParse(str, out value);

            value = default;
            return false;
        }

        public bool TryGetStringValue(string key, out string value)
        {
            return _envParams.Values.TryGetValue(key, out value);
        }

        public void SetValue(string key, string value)
        {
            Debug.LogDebug($"Set value: k = {key}, v = {value}");
            _envParams.Values[key] = value;
        }

        public BasicEnv Merge(EnvParams otherParams)
        {
            _envParams.Add(otherParams);
            return this;
        }
    }

    internal static class SdkEnvExtensions
    {
        /// <summary>
        /// 两个 Env 合并，优先用 first 的值
        /// </summary>
        /// <param name="overrider"></param>
        /// <param name="basic"></param>
        /// <returns></returns>
        public static IWritableEnv Override(this IWritableEnv overrider, IWritableEnv basic)
        {
            return new OverrideEnv(overrider, basic);
        }

        public static void SetValue(this IWritableEnv env, string key, int value)
        {
            env.SetValue(key, value.ToString());
        }

        /// <summary>
        /// 直播间启动token。 从抖音直播或PC伴侣启动时，应当为有效值。
        /// </summary>
        public static string GetLaunchToken(this IEnv env)
        {
            return env.GetStringValue(SdkConsts.ArgLaunchToken);
        }

        /// <summary>
        /// 是否从云游戏启动
        /// </summary>
        /// <returns></returns>
        public static bool IsCloud(this IEnv env)
        {
            var cloudGame = env.GetIntValue(SdkConsts.ArgCloudGame) == 1;
            if (SdkConsts.IsAndroidPlayer)
            {
                return cloudGame || CloudGameSDK.IsRunningCloud();
            }

            return cloudGame;
        }

        public static bool IsDouyin(this IEnv env)
        {
            var token = env.GetLaunchToken();
            // note: 此处不判断`StartAppParam`，因为在云游戏且不在抖音时，`StartAppParam`也可能不为空
            var isDouyin = !string.IsNullOrEmpty(token);
            return isDouyin;
        }

        /// <summary>
        /// 是否从云游戏Mobile启动
        /// </summary>
        /// <returns></returns>
        public static bool IsStartFromMobile(this IEnv env)
        {
            var mobile = env.GetIntValue(SdkConsts.ArgMobile);
            return mobile == 1;
        }

        /// <summary>
        /// 客户端启动参数StartAppParam。 单实例下用的是 -StartAppParam=
        /// </summary>
        public static string GetStartAppParam(this IEnv env)
        {
            return env.GetStringValue(SdkConsts.ArgStartAppParam);
        }
    }
}