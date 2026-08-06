using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Aliyun.OSS;
using Aliyun.OSS.Common;
using UnityEditor;
using UnityEngine;

namespace Aliyun.Editor
{
    public class OssManager
    {
        /// <summary>
        /// 
        /// </summary>
        private OssClient client;

        private OssManager()
        {
            client = new OssClient(AliyunConfig.Endpoint, AliyunConfig.AccessKeyId, AliyunConfig.AccessKeySecret);
        }

        private static OssManager m_Instance;

        public static OssManager Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new OssManager();
                }

                return m_Instance;
            }
        }

        /// <summary>
        /// 相关控制台输出
        /// </summary>
        public string logMessages = string.Empty;

        /// <summary>
        /// 判断文件在oss上是否存在
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="osspath"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public bool FileExist(string bucketName, string osspath, string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            // 只有bundle校验，远端有同名不传了，其他Manifest相关文件啥的都传
            if (!fileName.EndsWith(".bundle")) return false;
            osspath += "/" + fileName;
            try
            {
                this.client.GetObjectMetadata(bucketName, osspath);
                return true;
            }
            catch (Exception )
            {
                return false;
            }
        }

        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="bucketName">bucketName</param>
        /// <param name="osspath">oss路径</param>
        /// <param name="filePath">文件路径</param>
        public bool PutFile(string bucketName, string osspath, string filePath)
        {
            try
            {
                // 判断文件在oss上是否存在
                if (FileExist(bucketName, osspath, filePath)) return true;
                osspath += "/" + Path.GetFileName(filePath);
                this.client.PutObject(bucketName, osspath, filePath);
                return true;
            }
            catch (OssException ex)
            {
                Debug.LogError($"打包的Bundle上传失败  code:{ex.ErrorCode},info:{ex.Message},requestid:{ex.RequestId},hostid:{ex.HostId},filePath:{filePath}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"打包的Bundle上传失败  info:{ex.Message},filePath:{filePath}");
                return false;
            }
        }

        public List<PutObjetKeyData> PutFilesByFailDatas(string bucketName, List<PutObjetKeyData> keyAndFiles, Action<int, int> progressUpdate = null)
        {
            List<PutObjetKeyData> failDatas = new List<PutObjetKeyData>();
            int index = 0;
            progressUpdate?.Invoke(index, keyAndFiles.Count);
            foreach (var data in keyAndFiles)
            {
                if (!PutFile(bucketName, data.key, data.file))
                {
                    failDatas.Add(data);
                }

                progressUpdate?.Invoke(++index, keyAndFiles.Count);
            }

            return failDatas;
        }

        /// <summary>
        /// 上传文件夹
        /// </summary>
        /// <param name="bucketName">bucketName</param>
        /// <param name="osspath">oss路径</param>
        /// <param name="folderPath">文件夹路径</param>
        /// <param name="progressUpdate">进度更新,第一个参数，完成的数量,第二个参数总共需要上传的数量</param>
        /// <returns>上传失败的文件</returns>
        public List<PutObjetKeyData> PutFolder(string bucketName, string osspath, string folderPath, Action<int, int> progressUpdate = null)
        {
            List<PutObjetKeyData> failDatas = new List<PutObjetKeyData>();
            List<PutObjetKeyData> keyAndFiles = new List<PutObjetKeyData>();
            GetFolderAssets(keyAndFiles, osspath, folderPath, true);
            int index = 0;
            progressUpdate?.Invoke(index, keyAndFiles.Count);
            foreach (var data in keyAndFiles)
            {
                if (!PutFile(bucketName, data.key, data.file))
                {
                    failDatas.Add(data);
                }

                progressUpdate?.Invoke(++index, keyAndFiles.Count);
            }

            return failDatas;
        }

        /// <summary>
        /// 收集文件上传时候的日志 
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="stackTrace"></param>
        /// <param name="type"></param>
        public void HandleLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Log)
            {
                string message = "==================================\n";
                if (type == LogType.Log)
                {
                    message = string.Format("condition = {0} \n stackTrace = {1} \n type = {2}", condition, stackTrace,
                        type);
                }
                else
                {
                    message = "<color=red>" + string.Format("condition = {0} \n stackTrace = {1} \n type = {2}", condition,
                        stackTrace,
                        type) + "</color>";
                }

                //log += message + "\n==================================\n";
                logMessages += message + "\n";
            }
        }

        private void GetFolderAssets(List<PutObjetKeyData> keyAndFilePath, string osspath, string folderPath, bool isfirst)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);
            if (!isfirst)
            {
                osspath += "/" + directoryInfo.Name;
            }

            //Debug.Log(directoryInfo.Name);

            FileInfo[] files = directoryInfo.GetFiles();
            for (int i = 0; i < files.Length; i++)
            {
                //string key = osspath + "/" + files[i].Name;
                keyAndFilePath.Add(new PutObjetKeyData() { key = osspath, file = files[i].FullName });
            }

            DirectoryInfo[] directoryInfos = directoryInfo.GetDirectories();
            for (int i = 0; i < directoryInfos.Length; i++)
            {
                GetFolderAssets(keyAndFilePath, osspath, directoryInfos[i].FullName, false);
            }
        }

        public struct PutObjetKeyData
        {
            public string key;
            public string file;
        }
    }
}