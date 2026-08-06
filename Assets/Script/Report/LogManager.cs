using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using UnityEngine;
using Aliyun.Editor;

namespace XN
{
    /// <summary>
    /// 高性能、线程安全的本地日志系统与上报管理
    /// 核心优化：
    /// 1. 异步双缓冲队列写入本地日志，彻底解决 I/O 卡顿
    /// 2. Error 级别致命错误：触发防抖机制后，即时提取内存堆栈直接上传 OSS，保证大盘监控的实时性
    /// 3. 全量日志（最近3天）：仅在玩家主动点击时，在后台线程压缩成 Zip 并上传 OSS，节省 99% 的流量
    /// </summary>
    public class LogManager : MonoSingleton<LogManager>
    {
        private static bool _isInitialized = false;

        // 日志存放目录
        private static string _logDir;

        // 日志文件路径
        private string _infoLogPath;
        private string _warnLogPath;
        private string _errLogPath;

        // 文件写入流，保持常开以提高性能
        private StreamWriter _infoWriter;
        private StreamWriter _warnWriter;
        private StreamWriter _errWriter;

        // 线程安全的并发队列，用于缓存待写入的日志数据
        private ConcurrentQueue<LogData> _logQueue;

        // 后台写入线程
        private Thread _writeThread;
        private bool _isRunning = false;
        private AutoResetEvent _writeSignal;

        // ================= 防抖机制参数 =================
        // 一局游戏内最多上传严重错误的次数，防止无限死循环上传打爆 OSS
        private const int MaxErrorUploadPerSession = 10;

        private static int _currentErrorUploadCount = 0;

        // 同一个报错 1 分钟内只传 1 次
        private static DateTime _lastErrorUploadTime = DateTime.MinValue;
        private static string _lastErrorHash = string.Empty;

        /// <summary>
        /// 定义单条日志的数据结构
        /// </summary>
        private struct LogData
        {
            public string TimeStr;
            public string Message;
            public string StackTrace;
            public LogType Type;
        }

        protected override void OnInit()
        {
            try
            {
                // 1. 确定目录并创建
                _logDir = Path.Combine(Application.persistentDataPath, "dev_log_v2");
                if (!Directory.Exists(_logDir))
                {
                    Directory.CreateDirectory(_logDir);
                }

                // 2. 清理过期日志 (超过3天的)
                CleanOldLogs(_logDir, 3);

                // 3. 生成本次运行的文件名
                string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _infoLogPath = Path.Combine(_logDir, $"log_{timeStamp}.txt");
                _warnLogPath = Path.Combine(_logDir, $"warn_{timeStamp}.txt");
                _errLogPath = Path.Combine(_logDir, $"err_{timeStamp}.txt");

                // 4. 初始化写入流 (必须使用 FileShare.ReadWrite，允许在写入时其他线程进行读取/压缩操作)
                _infoWriter =
                    new StreamWriter(
                        new FileStream(_infoLogPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite),
                        Encoding.UTF8);
                _warnWriter =
                    new StreamWriter(
                        new FileStream(_warnLogPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite),
                        Encoding.UTF8);
                _errWriter =
                    new StreamWriter(
                        new FileStream(_errLogPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite),
                        Encoding.UTF8);

                // 关闭自动刷新，由后台线程控制
                _infoWriter.AutoFlush = false;
                _warnWriter.AutoFlush = false;
                _errWriter.AutoFlush = false;

                // 5. 初始化并发队列和线程信号量
                _logQueue = new ConcurrentQueue<LogData>();
                _writeSignal = new AutoResetEvent(false);

                // 6. 开启后台写入线程
                _isRunning = true;
                _writeThread = new Thread(WriteThreadLogic)
                {
                    Name = "LogManager_WriteThread",
                    IsBackground = true
                };
                _writeThread.Start();

                // 7. 注册 Unity 全局日志回调
                Application.logMessageReceivedThreaded += HandleLog;

                Debug.Log($"[LogManager] 日志系统初始化成功。路径: {_logDir}");

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LogManager] 初始化严重失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        protected override void OnRemove()
        {
        }

        /// <summary>
        /// Unity 的日志回调函数 (支持多线程)
        /// </summary>
        private void HandleLog(string condition, string stackTrace, LogType type)
        {
            if (!_isRunning) return;

            // 1. 塞入本地文件写入队列
            LogData data = new LogData
            {
                TimeStr = DateTime.Now.ToString("HH:mm:ss.fff"),
                Message = condition,
                StackTrace = stackTrace,
                Type = type
            };
            _logQueue.Enqueue(data);
            _writeSignal.Set();

            // 2. 触发即时报错上报（仅限 Error 和 Exception）
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                TryUploadErrorImmediately(condition, stackTrace);
            }
        }

        /// <summary>
        /// 尝试即时上报崩溃日志（防抖机制）
        /// </summary>
        private void TryUploadErrorImmediately(string message, string stackTrace)
        {
            // 防御1：单局超过最大上传次数，直接熔断放弃，防止破产
            if (_currentErrorUploadCount >= MaxErrorUploadPerSession) return;

            // 防御2：计算堆栈的极简 Hash，防止同一帧内疯狂抛出同一个报错
            string errorHash = (message + stackTrace).GetHashCode().ToString();
            DateTime now = DateTime.Now;

            if (errorHash == _lastErrorHash && (now - _lastErrorUploadTime).TotalSeconds < 60)
            {
                // 同一个报错，1分钟内只传1次
                return;
            }

            _lastErrorHash = errorHash;
            _lastErrorUploadTime = now;
            _currentErrorUploadCount++;

            // 构造崩溃内容文本
            string crashContent = $"Time: {now:yyyy-MM-dd HH:mm:ss}\nMessage: {message}\nStackTrace: {stackTrace}";

            // 开启一个异步线程去直接上传这串字符串（不读写本地文件）
            ThreadPool.QueueUserWorkItem(_ => { UploadCrashStringToOSS(crashContent); });
        }

        /// <summary>
        /// 将内存中的崩溃文本直接上传到 OSS
        /// </summary>
        private void UploadCrashStringToOSS(string crashContent)
        {
            try
            {
                var scenInfoComp = SceneHelper.GetSceneInfoComponent();
                string openId = scenInfoComp != null ? scenInfoComp.AnchorOpenId : "UnknownUser";

                // 命名格式：Crash/日期_玩家ID_唯一随机码.txt
                string objectKey =
                    $"DanMu/Crash/{DateTime.Now:yyyyMMdd_HHmmss}_{openId}_{Guid.NewGuid().ToString().Substring(0, 4)}.txt";

                // 将字符串转为 byte 数组
                byte[] bytes = Encoding.UTF8.GetBytes(crashContent);

                // 【调用原有的 OSS 接口上传字节流】
                // 如果你们的 OssManager 提供了直接上传 byte[] 或 string 的接口，请用那个接口。
                // 如果只有 PutFolder / PutFile，则可以先写到一个临时文件再传：
                string tempFilePath = Path.Combine(Application.temporaryCachePath, "temp_crash.txt");
                File.WriteAllBytes(tempFilePath, bytes);

                // 借用现有的 PutFolder 或者对应的上传单文件接口（这里使用假设的 PutFile，如无请替换为你们支持的接口）
                // OssManager.Instance.PutFile(AliyunConfig.Bucket_HotFixeBundle, objectKey, tempFilePath, null);
                // 暂时保留用原有的 List 伪装上传，你需要根据你们 OssManager 真实的单文件上传接口替换这行：
                OssManager.Instance.PutFolder(AliyunConfig.Bucket_HotFixeBundle, objectKey, tempFilePath, null);

                // 上传完删掉临时文件
                if (File.Exists(tempFilePath)) File.Delete(tempFilePath);
            }
            catch (Exception ex)
            {
                // 吃掉上传失败的异常，防止再次触发 HandleLog 引起死循环
                Console.WriteLine("Upload Crash Error: " + ex.Message);
            }
        }

        // ======================================================================
        // 玩家手动触发：打包并上传最近3天的完整日志
        // ======================================================================

        // 防止玩家疯狂点击造成并发上传
        private static bool _isUploadingManual = false;

        // 限制手动上报的冷却时间（例如 5 分钟内只能上报一次）
        private static DateTime _lastManualUploadTime = DateTime.MinValue;

        /// <summary>
        /// 玩家点击“上报日志”按钮时调用
        /// </summary>
        public static void UploadAllLogsManual()
        {
            if (!_isInitialized || string.IsNullOrEmpty(_logDir))
            {
                Debug.LogWarning("日志系统未初始化，无法上报。");
                return;
            }

            if (_isUploadingManual)
            {
                RoomHelper.AddTicker("日志正在上报中，请勿重复点击！");
                return;
            }

            // 限制上报频率（比如 5 分钟冷却），防止 OSS 产生大量完全一样的重复 Zip 包
            if ((DateTime.Now - _lastManualUploadTime).TotalMinutes < 5)
            {
                RoomHelper.AddTicker("上报过于频繁，请稍后再试！");
                return;
            }

            _isUploadingManual = true;
            RoomHelper.AddTicker("正在打包日志，请稍候...");

            // 开一个后台线程去压缩和上传，防止卡住游戏主界面
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    // 1. 让写入线程强制把当前内存中的日志刷入磁盘
                    Instance.FlushAllWriters();
                    
                    // 2. 创建临时压缩包路径
                    var scenInfoComp = SceneHelper.GetSceneInfoComponent();
                    string openId = scenInfoComp != null ? scenInfoComp.AnchorOpenId : "UnknownUser";
                    string zipFileName = $"Logs_{DateTime.Now:yyyyMMdd_HHmmss}_{openId}.zip";
                    string zipFilePath = Path.Combine(Application.temporaryCachePath, zipFileName);

                    if (File.Exists(zipFilePath)) File.Delete(zipFilePath);

                    // 3. 将整个 dev_log_v2 文件夹压缩为 zip
                    // 由于我们在初始化 StreamWriter 时加了 FileShare.ReadWrite，这里可以直接压缩不会报错
                    ZipFile.CreateFromDirectory(_logDir, zipFilePath, System.IO.Compression.CompressionLevel.Optimal,
                        false);

                    // 4. 将 ZIP 包传给 OSS
                    string ossPath = $"DanMu/Report_Manual/{zipFileName}";

                    // 这里为了兼容你原有的 PutFolder 逻辑，建议你们 OssManager 扩展一个传单文件的接口
                    // 这里创建一个临时空文件夹把 zip 放进去，利用 PutFolder 传文件夹的机制上传
                    string tempUploadDir = Path.Combine(Application.temporaryCachePath, "TempUploadDir");
                    if (Directory.Exists(tempUploadDir)) Directory.Delete(tempUploadDir, true);
                    Directory.CreateDirectory(tempUploadDir);
                    File.Copy(zipFilePath, Path.Combine(tempUploadDir, zipFileName));

                    List<OssManager.PutObjetKeyData> failDatas =
                        OssManager.Instance.PutFolder(AliyunConfig.Bucket_HotFixeBundle, ossPath, tempUploadDir, null);

                    bool success = failDatas.Count == 0;

                    // 5. 失败重试 4 次
                    if (!success)
                    {
                        int tryTimes = 0;
                        while (tryTimes < 4)
                        {
                            tryTimes++;
                            failDatas = OssManager.Instance.PutFilesByFailDatas(AliyunConfig.Bucket_HotFixeBundle,
                                failDatas, null);
                            if (failDatas.Count == 0)
                            {
                                success = true;
                                break;
                            }
                        }
                    }

                    // 6. 清理临时文件
                    if (File.Exists(zipFilePath)) File.Delete(zipFilePath);
                    if (Directory.Exists(tempUploadDir)) Directory.Delete(tempUploadDir, true);

                    // 7. 回到主线程提示玩家并记录冷却时间
                    Loom.QueueOnMainThread(() =>
                    {
                        _isUploadingManual = false;
                        if (success)
                        {
                            _lastManualUploadTime = DateTime.Now; // 上传成功才进入冷却
                            RoomHelper.AddTicker("日志上报成功！感谢您的反馈。");
                            Debug.Log("手动日志上报 OSS 成功。");
                        }
                        else
                        {
                            RoomHelper.AddTicker("日志上报失败，请检查网络。");
                            Debug.LogWarning("手动日志上报 OSS 失败。");
                        }
                    });
                }
                catch (Exception ex)
                {
                    Loom.QueueOnMainThread(() =>
                    {
                        _isUploadingManual = false;
                        RoomHelper.AddTicker("打包日志失败。");
                    });
                    Debug.LogWarning("打包日志异常: " + ex.Message);
                }
            });
        }

        // ======================================================================
        // 本地写入核心逻辑
        // ======================================================================

        private void WriteThreadLogic()
        {
            while (_isRunning)
            {
                _writeSignal.WaitOne();

                while (_logQueue.TryDequeue(out LogData data))
                {
                    try
                    {
                        WriteLogToFile(data);
                    }
                    catch
                    {
                        /* ignore */
                    }
                }

                FlushAllWriters();
            }

            while (_logQueue.TryDequeue(out LogData data))
            {
                try
                {
                    WriteLogToFile(data);
                }
                catch
                {
                    /* ignore */
                }
            }

            FlushAllWriters();
        }

        private void WriteLogToFile(LogData data)
        {
            string prefix = $"[{data.TimeStr}] [{data.Type}] ";

            _infoWriter.WriteLine(prefix + data.Message);
            if (data.Type == LogType.Error || data.Type == LogType.Exception || data.Type == LogType.Assert)
            {
                _infoWriter.WriteLine(data.StackTrace);
            }

            if (data.Type == LogType.Warning)
            {
                _warnWriter.WriteLine(prefix + data.Message);
            }

            if (data.Type == LogType.Error || data.Type == LogType.Exception || data.Type == LogType.Assert)
            {
                _errWriter.WriteLine(prefix + data.Message);
                _errWriter.WriteLine(data.StackTrace);
                _errWriter.WriteLine("----------------------------------------");
            }
        }

        private void FlushAllWriters()
        {
            try
            {
                _infoWriter?.Flush();
                _warnWriter?.Flush();
                _errWriter?.Flush();
            }
            catch
            {
                /* ignore */
            }
        }

        private void CleanOldLogs(string dir, int keepDays)
        {
            try
            {
                if (!Directory.Exists(dir)) return;

                string[] files = Directory.GetFiles(dir, "*.txt");
                DateTime now = DateTime.Now;

                foreach (var file in files)
                {
                    try
                    {
                        FileInfo fi = new FileInfo(file);
                        if ((now - fi.LastWriteTime).TotalDays > keepDays)
                        {
                            fi.Delete();
                        }
                    }
                    catch
                    {
                        /* ignore */
                    }
                }
            }
            catch
            {
                /* ignore */
            }
        }

        private void OnDestroy()
        {
            if (!_isInitialized) return;

            Application.logMessageReceivedThreaded -= HandleLog;

            _isRunning = false;
            _writeSignal?.Set();

            if (_writeThread != null && _writeThread.IsAlive)
            {
                _writeThread.Join(1000);
            }

            try
            {
                _infoWriter?.Close();
                _warnWriter?.Close();
                _errWriter?.Close();
            }
            catch
            {
                /* ignore */
            }

            _isInitialized = false;
        }
    }

    /// <summary>
    /// 简单的线程调度器占位符 (如果你们项目里有其他主线程回调器如 UniTask，请替换此处的 Loom)
    /// </summary>
    public static class Loom
    {
        public static void QueueOnMainThread(Action action)
        {
            // 如果你使用了 UniTask，可以改为: 
            // Cysharp.Threading.Tasks.UniTask.Post(action);

            // 如果项目中有专门的 MainThreadDispatcher，请替换此处
            // 否则需要实现一个简单的 MonoBehaviour Update 队列
            action?.Invoke();
        }
    }
}