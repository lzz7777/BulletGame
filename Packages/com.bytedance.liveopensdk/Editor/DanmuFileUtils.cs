using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Douyin.LiveOpenSDK.Editor
{
    public class DanmuConfig
    {
        [JsonProperty(PropertyName = "cloudsync", Required = Required.Always)]
        public bool cloudsync = false;

        private bool? _mate_live_local;

        [JsonProperty(PropertyName = "mate_live_local", NullValueHandling = NullValueHandling.Ignore)]
        public bool? mate_live_local
        {
            get => _mate_live_local;
            set => _mate_live_local = value;
        }

        public static int NullableBoolToInt(bool? value)
        {
            if (value == null)
                return 0;
            var b = value.Value;
            return b ? 1 : 2;
        }

        public static bool? IntToNullableBool(int value)
        {
            if (value == 1)
                return true;
            if (value == 2)
                return false;
            return null;
        }

        public string ToStr() => SerializeToJson(this);

        public static string SerializeToJson(DanmuConfig data)
        {
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }
    }

    // 定义接口，明确对外的公共接口
    public interface IDanmuFileUtils
    {
        bool IsConfigExist();
        DanmuFileUtils.FileTask<DanmuConfig> SaveConfig(DanmuConfig data);
        DanmuFileUtils.FileTask<DanmuConfig> LoadConfig();
        string ConfigFilePath { get; }
        string ConfigFileName { get; }
    }

    public class DanmuFileUtils : IDanmuFileUtils
    {
        private static readonly DanmuFileUtils _instance = new DanmuFileUtils();

        public static DanmuFileUtils Instance
        {
            get { return _instance; }
        }

        protected DanmuFileUtils()
        {
            ConfigFilePath = DefaultConfigFilePath;
        }

        public class FileTask<T>
        {
            public bool IsSuccess { get; set; }
            public string Path { get; set; }
            public string JsonString { get; set; }

            public T Data;
            public string ErrorMsg;
        }

        private const string _configFileName = "interact_danmu.json";

        private static string _configDirectoryPath =>
            Path.Combine(Path.GetFullPath(Application.dataPath), "LiveOpenSdkConfig");

        private static string DefaultConfigFilePath => Path.Combine(_configDirectoryPath, _configFileName);

        public string ConfigFilePath
        {
            get;
            protected set;
        }

        public string ConfigDirectoryPath => _configDirectoryPath;
        public string ConfigFileName => _configFileName;

        public virtual bool IsConfigExist()
        {
            var exists = File.Exists(ConfigFilePath);
            Debug.LogFormat("{0} 文件 {1}。", ConfigFilePath, exists ? "存在" : "不存在");
            return exists;
        }

        public virtual FileTask<DanmuConfig> SaveConfig(DanmuConfig data)
        {
            if (!Directory.Exists(ConfigDirectoryPath))
            {
                Directory.CreateDirectory(ConfigDirectoryPath);
            }

            var path = ConfigFilePath;
            var task = new FileTask<DanmuConfig>();
            try
            {
                task.Path = path;
                var json = DanmuConfig.SerializeToJson(data);
                task.JsonString = json;
                File.WriteAllText(path, json);
                task.Data = data;
                task.IsSuccess = true;
                Debug.Log("JSON file saved successfully.");
                return task;
            }
            catch (Exception ex)
            {
                task.IsSuccess = false;
                task.ErrorMsg = $"An error occurred while saving the config: {ex.Message}";
                Debug.LogError(task.ErrorMsg);
                return task;
            }
        }

        public virtual FileTask<DanmuConfig> LoadConfig()
        {
            var path = ConfigFilePath;
            var task = new FileTask<DanmuConfig>();
            try
            {
                task.Path = path;
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    task.JsonString = json;
                    var config = JsonConvert.DeserializeObject<DanmuConfig>(json);
                    task.IsSuccess = true;
                    task.Data = config;
                    return task;
                }
                else
                {
                    task.IsSuccess = false;
                    task.ErrorMsg = "JSON file not found.";
                    return task;
                }
            }
            catch (Exception ex)
            {
                task.IsSuccess = false;
                task.ErrorMsg = $"Error while loading config, path: {path}, {ex}";
                return task;
            }
        }
    }
}