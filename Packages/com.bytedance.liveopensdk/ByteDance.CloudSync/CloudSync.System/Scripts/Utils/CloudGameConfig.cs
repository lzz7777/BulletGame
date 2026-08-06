using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace ByteDance.CloudSync
{
    [Serializable]
    internal class CloudGameConfig
    {
        [SerializeField] [JsonProperty("appId")]
        private string appId;

        public string AppId => appId;
    }

    internal class CloudGameConfigLoader
    {
        private string PlayerConfigPath => "cloud_game_config.json";
        private string AssetConfigPath => $"{Application.dataPath}/../cloud_game_config.json";

        protected virtual string ConfigPath => Application.isEditor ? AssetConfigPath : PlayerConfigPath;

        public CloudGameConfig Load()
        {
            var configPath = ConfigPath;
            if (!File.Exists(configPath))
                return null;

            try
            {
                var json = File.ReadAllText(configPath);
                Debug.Log($"config file loaded, {configPath}");
                var config = JsonUtil.ToObject<CloudGameConfig>(json);
                Debug.Log($"config json loaded, appId: {config.AppId}");
                return config;
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }

            return null;
        }

        public BasicEnv LoadAsEnv()
        {
            var config = Load();
            if (config == null)
                return null;

            var env = new BasicEnv();
            if (!string.IsNullOrEmpty(config.AppId))
                env.SetValue(SdkConsts.GameArgAppId, config.AppId);

            return env.IsEmpty() ? null : env;
        }

        public static void TryOverrideEnv(CloudSyncEnv env)
        {
            var loader = new CloudGameConfigLoader();
            var newEnv = loader.LoadAsEnv();
            if (newEnv == null)
                return;
            env.OverrideWith(newEnv);
            Debug.Log($"config json override env, appId: {env.AppId}");
        }
    }
}