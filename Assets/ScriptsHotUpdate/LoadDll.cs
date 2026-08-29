using HybridCLR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace XN
{
    /// <summary>
    /// 热更新核心启动类：初始化YooAsset -> 下载热更资源 -> 补充AOT元数据 -> 启动热更程序集
    /// </summary>
    public class LoadDll : MonoBehaviour
    {
        /// <summary>
        /// 运行模式：
        /// EditorSimulateMode: 编辑器模拟（读本地，免打包）
        /// HostPlayMode: 联机模式（连CDN更新包，真机标准模式）
        /// OfflinePlayMode: 单机模式（读包体内置资源，不联网）
        /// </summary>
        public EPlayMode playMode = EPlayMode.HostPlayMode;

        private ResourcePackage _codesPackage; // 代码资源包实例
        public const string CodesPackageName = "CodesPackage"; // YooAsset中代码包的名称
        private const string OnlineConfigName = "BulletGame_1.json"; // 在线配置文件名

        void Start()
        {
            // 强行在AOT主工程中调用，彻底断绝IL2CPP剥离此类的念想
            XN.AOT.AtlasEventWrapper.Preserve();

            Init();
            
            DontDestroyOnLoad(this);
        }

        #region YooAsset初始化及核心热更流程

        private async UniTask Init()
        {
            // 1. 初始化在线配置（获取最新版本号）
            if (playMode == EPlayMode.HostPlayMode)
                await OnlineConfigHelper.Init(OnlineConfigName);
            // 2. 启动核心热更流程
            InitYooAssets();
        }

        /// <summary>
        /// 核心流程：初始化YooAsset -> [更新资源] -> 加载DLL -> 进入游戏
        /// </summary>
        private async UniTask InitYooAssets()
        {
            // 1. 全局初始化
            YooAssets.Initialize();

            // 2. 创建并初始化代码包
            var package = YooAssets.CreatePackage(CodesPackageName);
            InitializationOperation initializationOperation = null;

            switch (playMode)
            {
                case EPlayMode.EditorSimulateMode:
                    initializationOperation = await InitPackageEditorSimulateMode(package);
                    break;
                case EPlayMode.HostPlayMode:
                    initializationOperation = await InitPackageHostPlayMode(package);
                    break;
                case EPlayMode.OfflinePlayMode:
                    initializationOperation = await InitPackageOfflinePlayMode(package);
                    break;
            }


            // 3. 校验初始化结果
            if (initializationOperation?.Status == EOperationStatus.Succeed)
            {
                Debug.Log("代码包初始化成功！");
            }
            else
            {
                Debug.LogError($"代码包初始化失败：{initializationOperation?.Error}");
                return; // 失败阻断
            }

            _codesPackage = package;

            // 4. 获取版本并激活清单 (所有模式都需要激活 Manifest)
            if (playMode == EPlayMode.HostPlayMode)
            {
                // 联机模式专属流程：执行网络热更（版本请求->清单更新->资源下载）
                await UpdatePackageHostPlayMode(package);
            }
            else
            {
                // 单机模式/编辑器模拟模式：也必须去获取本地版本并激活清单，否则后续 LoadAsset 会报 Can not found active package manifest 错
                var versionOp = package.RequestPackageVersionAsync();
                await versionOp;
                if (versionOp.Status == EOperationStatus.Succeed)
                {
                    var manifestOp = package.UpdatePackageManifestAsync(versionOp.PackageVersion);
                    await manifestOp;
                    if (manifestOp.Status != EOperationStatus.Succeed)
                    {
                        Debug.LogError($"本地清单激活失败: {manifestOp.Error}");
                        return;
                    }
                }
                else
                {
                    Debug.LogError($"本地版本获取失败: {versionOp.Error}");
                    return;
                }
            }

            // 5. 加载所有需要的DLL资源（含热更DLL与AOT元数据DLL）
            var assets = new List<string> { "HotUpdate.dll" }.Concat(AOTMetaAssemblyFiles);
            foreach (var asset in assets)
            {
                var handle = package.LoadAssetAsync<TextAsset>(asset);
                await handle;

                // 缓存加载的DLL文本流，供后续HybridCLR使用
                var assetObj = handle.AssetObject as TextAsset;
                s_assetDatas[asset] = assetObj;
                Debug.Log($"dll:{asset}   {assetObj == null}");
            }

            Debug.Log($"更新成功，版本 {_codesPackage.GetPackageVersion()}");

            // 6. 开始游戏逻辑交接
            StartGame();
        }

        /// <summary>
        /// 联机模式网络热更状态机：获取版本 -> 更新清单 -> 下载包 -> 清理缓存
        /// </summary>
        private async UniTask UpdatePackageHostPlayMode(ResourcePackage package)
        {
            string packageVersion = string.Empty;

            // [状态1] 获取资源版本号
            bool requestVersionSuccess = false;
            while (!requestVersionSuccess)
            {
                // false：禁止追加时间戳，避免破坏OSS签名
                var operation = package.RequestPackageVersionAsync(false);
                await operation;

                if (operation.Status != EOperationStatus.Succeed)
                {
                    Debug.LogError($"获取资源版本失败: {operation.Error}，准备弹出重试UI");
                    await ShowRetryUIDialogAsync("获取资源版本失败，请检查网络并重试"); // 阻断等待重试
                }
                else
                {
                    packageVersion = operation.PackageVersion;
                    Debug.Log($"Updated code package Version : {packageVersion}");
                    requestVersionSuccess = true;
                }
            }

            // [状态2] 更新补丁清单
            bool updateManifestSuccess = false;
            while (!updateManifestSuccess)
            {
                var operation2 = package.UpdatePackageManifestAsync(packageVersion, 60);
                await operation2;

                if (operation2.Status != EOperationStatus.Succeed)
                {
                    Debug.LogError($"更新清单失败: {operation2.Error}，准备弹出重试UI");
                    await ShowRetryUIDialogAsync("更新补丁清单失败，请检查网络并重试");
                }
                else
                {
                    updateManifestSuccess = true;
                }
            }

            // [状态3] 下载热更资源包
            bool downloadSuccess = false;
            while (!downloadSuccess)
            {
                downloadSuccess = await Download(package);

                if (!downloadSuccess)
                {
                    Debug.LogError("下载热更包失败，准备弹出重试UI");
                    await ShowRetryUIDialogAsync("下载更新文件失败，请检查网络并重试");
                }
            }

            // [状态4] 清理旧版无用缓存（释放磁盘空间）
            await ClearPackageUnusedCacheBundleFiles();
        }

        private async UniTask<InitializationOperation> InitPackageEditorSimulateMode(ResourcePackage package)
        {
            // 编辑器模拟：直接使用项目绝对路径读取Asset，无需打AB包
            var buildResult = EditorSimulateModeHelper.SimulateBuild(CodesPackageName);
            var fileSystemParams =
                FileSystemParameters.CreateDefaultEditorFileSystemParameters(buildResult.PackageRootDirectory);
            var createParameters = new EditorSimulateModeParameters { EditorFileSystemParameters = fileSystemParams };

            var initOperation = package.InitializeAsync(createParameters);
            await initOperation;
            return initOperation;
        }

        private async UniTask<InitializationOperation> InitPackageHostPlayMode(ResourcePackage package)
        {
            // 联机模式：内置目录(StreamingAssets)兜底 + 缓存目录(persistentDataPath)沙盒读写
            var packVersion = OnlineConfig.Data[nameof(VersionType.CodeVersion)].ToString();
            IRemoteServices remoteServices = new RemoteServices(packVersion, PackageType.Code);

            var createParameters = new HostPlayModeParameters
            {
                BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters(),
                CacheFileSystemParameters = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices)
            };

            var initOperation = package.InitializeAsync(createParameters);
            await initOperation;
            return initOperation;
        }

        private async UniTask<InitializationOperation> InitPackageOfflinePlayMode(ResourcePackage package)
        {
            // 单机模式：只读取内置首包目录(StreamingAssets)，无网络请求
            var fileSystemParams = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
            var createParameters = new OfflinePlayModeParameters { BuildinFileSystemParameters = fileSystemParams };

            var initOperation = package.InitializeAsync(createParameters);
            await initOperation;
            return initOperation;
        }

        // 模拟AOT层的重试UI弹窗 (TODO: 需绑定真实UGUI界面与按钮)
        private async UniTask ShowRetryUIDialogAsync(string message)
        {
            Debug.LogWarning($"[AOT UI Mock] 弹出错误提示面板: {message}");
            Debug.LogWarning("[AOT UI Mock] 等待玩家点击重试...");
            await UniTask.Delay(TimeSpan.FromSeconds(2f)); // 模拟等待玩家点击
        }

        private async UniTask ClearPackageUnusedCacheBundleFiles()
        {
            var operation = _codesPackage.ClearCacheFilesAsync(EFileClearMode.ClearUnusedBundleFiles);
            await operation;

            if (operation.Status == EOperationStatus.Succeed)
                Debug.Log("缓存清理成功");
            else
                Debug.LogError(operation.Error);
        }

        #endregion

        #region 下载热更资源

        /// <summary>
        /// 封装YooAsset的下载器逻辑
        /// </summary>
        async UniTask<bool> Download(ResourcePackage package)
        {
            int downloadingMaxNum = 10; // 最大并发下载数
            int failedTryAgain = 3; // 失败自动重试次数

            var downloader = package.CreateResourceDownloader(downloadingMaxNum, failedTryAgain);

            // 无需下载时直接通过
            if (downloader.TotalDownloadCount == 0)
            {
                Debug.Log("当前已是最新版本，无文件需下载");
                return true;
            }

            float totalDownloadMb = downloader.TotalDownloadBytes * 1.0f / (1024 * 1024);
            Debug.Log($"{package.PackageName} 需下载文件数: {downloader.TotalDownloadCount}\n 总大小: {totalDownloadMb:F2} MB");

            // 绑定回调委托 (可用于UI进度条更新)
            downloader.DownloadErrorCallback = OnDownloadErrorFunction;
            downloader.DownloadUpdateCallback = OnDownloadProgressUpdateFunction;
            downloader.DownloadFinishCallback = OnDownloadOverFunction;
            downloader.DownloadFileBeginCallback = OnStartDownloadFileFunction;

            // 开启并等待下载完成
            downloader.BeginDownload();
            await downloader;

            if (downloader.Status == EOperationStatus.Succeed)
            {
                Debug.Log("更新包下载完成");
                return true;
            }
            else
            {
                Debug.Log("更新包下载失败");
                return false;
            }
        }

        private void OnStartDownloadFileFunction(DownloadFileData data) =>
            Debug.Log($"开始下载：{data.FileName}，大小：{data.FileSize}");

        private void OnDownloadOverFunction(DownloaderFinishData data) =>
            Debug.Log("下载" + (data.Succeed ? "成功" : "失败"));

        private void OnDownloadErrorFunction(DownloadErrorData data) =>
            Debug.Log($"下载出错：{data.FileName}，错误：{data.ErrorInfo}");

        private void OnDownloadProgressUpdateFunction(DownloadUpdateData data)
        {
            // TODO: 发送给UI层更新 Slider 进度条
            // Debug.Log($"已下载文件数：{data.CurrentDownloadCount}/{data.TotalDownloadCount}");
        }

        #endregion

        #region 补充元数据 (HybridCLR核心)

        // 需要补充元数据的 AOT DLL 列表
        // 作用：如果热更代码使用了 AOT 层的泛型实例化（如 List<AOT类型>），必须补充对应的 AOT 元数据给 HybridCLR，否则闪退
        private static List<string> AOTMetaAssemblyFiles { get; } =
            new() { "mscorlib.dll", "System.dll", "System.Core.dll", "UniTask.dll" };

        // 缓存 DLL 字节码
        private static Dictionary<string, TextAsset> s_assetDatas = new();

        // 热更主程序集对象
        private static Assembly _hotUpdateAss;

        public static byte[] ReadBytesFromStreamingAssets(string dllName)
        {
            if (s_assetDatas.ContainsKey(dllName))
                return s_assetDatas[dllName].bytes;
            return Array.Empty<byte>();
        }

        /// <summary>
        /// 遍历 AOT DLL 列表，通过 HybridCLR API 注入元数据
        /// </summary>
        private static void LoadMetadataForAOTAssemblies()
        {
            // SuperSet模式：支持运行时泛型实例化的超集解析
            HomologousImageMode mode = HomologousImageMode.SuperSet;

            foreach (var aotDllName in AOTMetaAssemblyFiles)
            {
                byte[] dllBytes = ReadBytesFromStreamingAssets(aotDllName);

                // 将 AOT 的二进制数据交由 HybridCLR 接管泛型分发
                LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, mode);
                Debug.Log($"LoadMetadataForAOTAssembly:{aotDllName}. mode:{mode} ret:{err}");
            }
        }

        #endregion

        #region 运行测试与启动交接

        /// <summary>
        /// 游戏启动收尾：处理AOT泛型 -> 加载热更DLL -> 移交控制权
        /// </summary>
        void StartGame()
        {
            // 1. 预热泛型，减少运行时性能开销
            AOTGenericWarmup.Init();

            // 2. 为 mscorlib 等 AOT DLL 补充元数据，完善 HybridCLR 运行环境
            LoadMetadataForAOTAssemblies();

            // 3. 加载热更业务逻辑 DLL
#if !UNITY_EDITOR
            // 真机环境：利用底层 API 从字节码直接加载程序集
            _hotUpdateAss = Assembly.Load(ReadBytesFromStreamingAssets("HotUpdate.dll"));
#else
            // 编辑器环境：由于未走打包流程，直接从当前 AppDomain 抓取已编译好的程序集
            _hotUpdateAss = System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "HotUpdate");
#endif
            Debug.Log("热更准备完毕，即将移交控制权并切换场景");

            // 4. 反射启动热更层入口
            LoadAsset();
        }

        /// <summary>
        /// 跨层调用：利用反射技术触发 HotUpdateAsset.StartLoadAssets
        /// </summary>
        private void LoadAsset()
        {
            // 获取热更程序集中的入口类 Type
            Type mainType = _hotUpdateAss.GetType("XN.HotUpdateAsset");

            // 严谨查找：带有一个 EPlayMode 参数的 StartLoadAssets 方法（防止重载混淆）
            MethodInfo startMethod = mainType.GetMethod("StartLoadAssets", new Type[] { typeof(YooAsset.EPlayMode) });

            if (startMethod != null)
            {
                // 触发方法执行，将当前 AOT 层的运行模式传递给热更层
                startMethod.Invoke(null, new object[] { this.playMode });
            }
            else
            {
                Debug.LogError("致命错误：未找到匹配的 StartLoadAssets(EPlayMode) 入口方法！");
            }
        }

        #endregion
    }
}