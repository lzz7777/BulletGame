using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Aliyun.Editor;
using Aliyun.OSS;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

namespace XN
{
    public static class OnlineConfigHelper
    {
        private static OssClient _ossClient = new(AliyunConfig.Endpoint, AliyunConfig.AccessKeyId, AliyunConfig.AccessKeySecret);

        public static async UniTask Init(string name)
        {
            try
            {
                // 获取对象
                string path = $"{AliyunConfig.Path}/Config/{name}";
                using (var response = _ossClient.GetObject(AliyunConfig.Bucket_HotFixeBundle, path))
                {
                    // 读取内容
                    using (var stream = response.Content)
                    {
                        var reader = new StreamReader(stream, Encoding.UTF8);
                        string content = reader.ReadToEnd();
                        Debug.Log("JSON内容:" + content);

                        OnlineConfig.Data = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("错误: " + ex.Message);
            }
        }
    }
}