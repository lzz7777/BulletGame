//====================================================
//Author:AS
//Time  :2025/09/04 16:09:12
//Desc  :
//====================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Aliyun.Editor;
using UnityEngine;

namespace XN
{
    /// <summary>
    /// 本地日志捕获与上报核心类（静态类实现）
    /// 主要职责：拦截 Unity 运行时日志，分类写入 txt，清理过期文件，并支持一键上传阿里云 OSS
    /// 【注意】：此脚本存在同步 I/O 阻塞、丢失堆栈信息等问题，建议仅做参考或历史版本维护
    /// </summary>
    public static class LocalLog
    {
        // 标记是否已初始化，防止在编辑器模式下反复 Play/Stop 导致重复注册（受 Domain Reload 设置影响）
        static bool isInited = false;
        
        // 日志存放的根目录 (Application.persistentDataPath/dev_log)
        static public string dir;
        
        // 三个分类日志的绝对文件路径
        static string log_name;       // 记录所有级别日志的全量文件
        static string warn_log_name;  // 仅记录警告级别以上的日志文件
        static string err_log_name;   // 仅记录错误级别（Error/Exception/Assert）的日志文件

        /// <summary>
        /// 日志系统启动入口
        /// </summary>
        public static void LaunchHandleLog()
        {
            if(isInited) return; // 已经初始化过则直接返回
            isInited = true;
            
            // 订阅 Unity 全局日志事件，将每一次 Debug.Log 路由到 HandleLog 方法
            Application.logMessageReceived += HandleLog;
            
            // 生成本次运行的时间戳前缀（注意：这里使用了全角的冒号 '：'，以绕过 Windows 文件名不能包含半角冒号的限制）
            string time = DateTime.Now.ToString("yyyy-MM-dd_HH：mm：ss");
            dir = Application.persistentDataPath + "/dev_log";

            // 确保沙盒日志目录存在
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            // 每次启动时，清理3天前的陈旧日志文件
            CleanOldLogs(dir);

            // 分别生成三个分类文件的路径，并初始化空文件
            log_name = dir + "/log" + time + ".txt";
            if (!System.IO.File.Exists(log_name))
            {
                System.IO.FileStream fs = System.IO.File.Create(log_name);
                fs.Close(); // 立即关闭流，释放文件占用
            }
            
            warn_log_name = dir + "/warn_log" + time + ".txt";
            if (!System.IO.File.Exists(warn_log_name))
            {
                System.IO.FileStream fs = System.IO.File.Create(warn_log_name);
                fs.Close();
            }

            err_log_name = dir + "/err_log" + time + ".txt";
            if (!System.IO.File.Exists(err_log_name))
            {
                System.IO.FileStream fs = System.IO.File.Create(err_log_name);
                fs.Close();
            }
        }

        /// <summary>
        /// 清理指定目录中生成时间超过 3 天的旧日志文件
        /// 【脆弱点】：通过截取文件名并反向解析全角冒号时间戳来判断文件寿命，一旦时区或语言环境异常极易解析失败
        /// </summary>
        /// <param name="dir">日志根目录</param>
        static void CleanOldLogs(string dir)
        {
            try
            {
                if (!System.IO.Directory.Exists(dir))
                    return;

                string[] logFiles = System.IO.Directory.GetFiles(dir, "*.txt");
                DateTime now = DateTime.Now;
                TimeSpan threeDays = TimeSpan.FromDays(3);

                foreach (string filePath in logFiles)
                {
                    try
                    {
                        string fileName = System.IO.Path.GetFileName(filePath);
                        
                        // 尝试从文件名中剔除前缀，提取纯时间字符串
                        string timeStr = null;
                        if (fileName.StartsWith("log") && fileName.Length > 3)
                            timeStr = fileName.Substring(3); // 去掉 "log"
                        else if (fileName.StartsWith("warn_log") && fileName.Length > 8)
                            timeStr = fileName.Substring(8); // 去掉 "warn_log"
                        else if (fileName.StartsWith("err_log") && fileName.Length > 7)
                            timeStr = fileName.Substring(7); // 去掉 "err_log"
                        else
                            continue; // 不是约定前缀的文件，跳过
                        
                        // 去掉末尾的 .txt 扩展名
                        if (timeStr.EndsWith(".txt"))
                            timeStr = timeStr.Substring(0, timeStr.Length - 4);
                        
                        // 将生成时使用的全角冒号 '：' 替换回半角冒号 ':' 以便 DateTime 解析
                        timeStr = timeStr.Replace("：", ":");
                        
                        // 严格解析提取出的时间字符串
                        if (DateTime.TryParseExact(timeStr, "yyyy-MM-dd_HH:mm:ss",
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None, out DateTime fileTime))
                        {
                            // 检查文件时间是否已经超过 3 天，超过则从磁盘物理删除
                            if (now - fileTime > threeDays)
                            {
                                System.IO.File.Delete(filePath);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // 吞掉单个文件处理失败的异常，保证循环继续清理下一个文件
                        Debug.LogWarning($"清理日志文件失败 {filePath}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"清理日志目录失败 {dir}: {ex.Message}");
            }
        }

        /// <summary>
        /// 日志回调处理核心函数：将拦截到的日志分类写入本地文件
        /// 【性能隐患】：频繁调用同步的 File.AppendAllText 会导致主线程极度卡顿（频繁开关流）
        /// 【逻辑缺陷】：直接抛弃了 stackTrace 参数，导致致命错误的堆栈上下文全部丢失
        /// </summary>
        static void HandleLog(string logString, string stackTrace, LogType type)
        {
            // 仅拼接了原始字符串和一个换行符，没有时间戳前缀，也没有堆栈信息
            string outString = logString + "\n";
            
            // 全量日志：无脑追加写入
            System.IO.File.AppendAllText(log_name,outString);
            
            // 警告日志：仅在级别为 Warning 时追加
            if (type == LogType.Warning)
            {
                System.IO.File.AppendAllText(warn_log_name,outString);
            }
            
            // 错误日志：在级别为 Error/Exception/Assert 时追加
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) 
            {
                System.IO.File.AppendAllText(err_log_name,outString);
            }
        }

        /// <summary>
        /// 用于接收 OSS 上传进度的回调方法
        /// </summary>
        public static void ProgressBarUpdate(int c, int a)
        {
            Debug.Log($"ProgressBarUpdate:{a}/{c}");
        }
        
        /// <summary>
        /// 一键打包上报整个日志文件夹到阿里云 OSS
        /// </summary>
        public static bool UploadServer()
        {
            // 获取当前玩家的标识（如主播ID），用于拼接 OSS 的子目录
            var scenInfoComp = SceneHelper.GetSceneInfoComponent();
            string openId = scenInfoComp != null ? scenInfoComp.AnchorOpenId : "UnknownUser";
            
            // 构造 OSS 上的目标路径结构，例如：DanMu/Report/20250904_160912_123456
            string osspath = $"DanMu/Report/{DateTime.Now.ToString("yyyyMMdd_HHmmss")}_{openId}";

            // 调用 OssManager 的批量文件夹上传接口，将本地的整个 dev_log 目录传上去
            List<OssManager.PutObjetKeyData> failDatas = OssManager.Instance.PutFolder(AliyunConfig.Bucket_HotFixeBundle, osspath, dir, null);
    
            // 如果失败列表长度为 0，说明全部文件上传成功
            bool success = failDatas.Count == 0;

            // 如果有部分文件上传失败，进入重试机制（最多重试 4 次）
            if (!success)
            {
                int tryTimes = 0;
                while (tryTimes < 4)
                {
                    tryTimes++;
                    // 仅针对失败的文件列表发起续传请求
                    failDatas = OssManager.Instance.PutFilesByFailDatas(AliyunConfig.Bucket_HotFixeBundle, failDatas, ProgressBarUpdate);
                    
                    if (failDatas.Count != 0) continue; // 如果还是没传完，继续下一轮 while 循环
                    
                    success = true;
                    break;
                }
            }
    
            // 如果重试了 4 次依然有文件失败，则在控制台打印出最终失败的键值和文件名
            if (!success)
            {
                string failMessage = failDatas.Aggregate("OSS有文件上传失败\n", (current, data) => current + $"key = {data.key}  file = {data.file}");
                Debug.Log(failMessage);
            }
    
            // 触发游戏内的飘字提示，告知玩家上报结果（无论部分失败与否这里都弹了成功）
            string tickerContent = "上报成功";
            RoomHelper.AddTicker(tickerContent);
            
            return true;
        }
    }
}

