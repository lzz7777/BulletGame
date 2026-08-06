using System;
using System.Collections.Generic;

namespace ByteDance.CloudSync
{
    /// <summary>
    /// 环境变量容器，用于取命令行参数值
    /// </summary>
    public interface IEnv
    {
        bool HasKey(string key);

        int GetIntValue(string key)
        {
            if (TryGetIntValue(key, out var value))
                return value;
            return default;
        }

        string GetStringValue(string key)
        {
            if (TryGetStringValue(key, out var value))
                return value;
            return null;
        }

        bool TryGetIntValue(string key, out int value);

        bool TryGetStringValue(string key, out string value);
    }

    internal interface IWritableEnv : IEnv
    {
        void SetValue(string key, string value);
    }

    internal interface ISdkEnv : IWritableEnv
    {
        void Merge(EnvParams additional);
    }

    /// <summary>
    /// CloudGame API 底层维护的 Env
    /// </summary>
    internal class SdkEnv : ProxyEnv, ISdkEnv
    {
        private static readonly SdkDebugLogger Debug = new(nameof(SdkEnv));

        public bool IsEnvReady { get; private set; }

        private readonly BasicEnv _base = new();

        public SdkEnv()
        {
            Inner = _base;
            ParseArgs(Environment.GetCommandLineArgs());
        }

        public void Merge(EnvParams additional)
        {
            // startAppParam 特殊处理一下
            var startAppParam = additional.Values.GetValueOrDefault(SdkConsts.ArgStartAppParam);
            if (startAppParam != null)
                MergeAppParams(startAppParam);
            _base.Merge(additional);
        }

        /// <summary>
        /// AppParams 里的 key 重命名一下，加一个 app@ 前缀，防止和命令行的冲突
        /// 参考 <seealso cref="SdkConsts.ArgAppGameId"/> 等常量的值的前缀
        /// </summary>
        /// <param name="startAppParam"></param>
        private void MergeAppParams(string startAppParam)
        {
            var newParams = new EnvParams();
            var appParams = EnvParser.ParseAppParams(startAppParam);
            foreach (var (key, value) in appParams.Values)
            {
                newParams.WithValue($"app@{key}", value);
            }
            _base.Merge(newParams);
        }

        public void SetReady()
        {
            IsEnvReady = true;
        }

        public void ParseArgs(string[] args)
        {
            var envParams = EnvParser.ParseArgs(args);
            Merge(envParams);
        }

        public void ParseArgLine(string argLine)
        {
            var argLineParser = new ArgLineParser();
            var envParams = argLineParser.Parse(argLine);
            Merge(envParams);
        }
    }
}