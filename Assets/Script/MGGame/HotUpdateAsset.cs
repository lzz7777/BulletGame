using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;

namespace XN
{
    /// <summary>
    /// 热更层入口类：负责资源包（AssetPackage）和配置包（ConfigPackage）的初始化与更新，最终切换游戏场景
    /// 由 AOT 层的 LoadDll.cs 通过反射调用启动
    /// </summary>
    public class HotUpdateAsset : MonoBehaviour
    {
        private ResourcePackage _assetPackage; // 核心资源包实例

        private const string AssetPackageName = "DefaultPackage"; // 核心资源包名称
        private const string ConfigPackageName = "ConfigPackage"; // 配置文件包名称

        private void Start()
        {
            InitYooAssets();
        }

        /// <summary>
        /// AOT 层反射调用的唯一入口点
        /// 接收 AOT 层传来的运行模式，挂载自身以触发生命周期
        /// </summary>
        public static void StartLoadAssets(EPlayMode mode)
        {
            Debug.Log($"热更层接收到了当前运行模式: {mode}");
            GameConst.PlayMode = mode; // 缓存运行模式供后续资源初始化使用
            
            // 挂载到常驻节点上，触发 Start() 开始资源包热更流程
            GameObject.Find("Main").AddComponent<HotUpdateAsset>();
        }

        #region YooAsset初始化及资源热更流程

        private async UniTask InitYooAssets()
        {
            // 1. 初始化核心资源包 (AssetPackage)
            var (assetPackage, defaultSucceed) = await InitPackageSingle(AssetPackageName, PackageType.Asset, VersionType.AssetVersion);
            if (!defaultSucceed)
            {
                Debug.LogError($"{AssetPackageName} 初始化失败");
                return;
            }

            // 将核心资源包设为默认包，后续不带包名的 YooAssets 加载 API 都会默认走这里
            YooAssets.SetDefaultPackage(assetPackage);
            _assetPackage = assetPackage;

            // 2. 初始化配置包 (ConfigPackage)
            var (confPackage, confSucceed) = await InitPackageSingle(ConfigPackageName, PackageType.Config, VersionType.ConfigVersion);
            if (!confSucceed)
            {
                Debug.LogError($"{ConfigPackageName} 初始化失败");
                return;
            }

            // 3. 所有资源和配置更新完毕，进入游戏主场景
            LoadScene();
        }

        /// <summary>
        /// 单个 YooAsset 包的标准化初始化与更新流程
        /// </summary>
        private async UniTask<(ResourcePackage, bool)> InitPackageSingle(string packageName, PackageType packageType, VersionType versionType)
        {
            var package = YooAssets.CreatePackage(packageName);
            InitializationOperation initializationOperation = null;

            // 1. 根据模式进行本地配置初始化
            switch (GameConst.PlayMode)
            {
                case EPlayMode.EditorSimulateMode:
                    initializationOperation = await InitPackageEditorSimulateMode(package, packageName);
                    break;
                case EPlayMode.HostPlayMode:
                    initializationOperation = await InitPackageHostPlayMode(package, packageType, versionType);
                    break;
                case EPlayMode.OfflinePlayMode:
                    initializationOperation = await InitPackageOfflinePlayMode(package);
                    break;
            }

            // 校验初始化结果
            if (initializationOperation?.Status == EOperationStatus.Succeed)
            {
                Debug.Log($"{packageType} 包初始化成功！");
            }
            else
            {
                Debug.LogError($"{packageType} 包初始化失败：{initializationOperation?.Error}");
                return (package, false); // 失败阻断
            }

            // 2. 获取版本并激活清单 (所有模式都需要)
            if (GameConst.PlayMode == EPlayMode.HostPlayMode)
            {
                // 联机模式专属：执行网络热更下载
                await UpdatePackageHostPlayMode(package);
            }
            else
            {
                // 单机模式/编辑器模拟模式：也必须激活清单
                var versionOp = package.RequestPackageVersionAsync();
                await versionOp;
                if (versionOp.Status == EOperationStatus.Succeed)
                {
                    var manifestOp = package.UpdatePackageManifestAsync(versionOp.PackageVersion);
                    await manifestOp;
                    if (manifestOp.Status != EOperationStatus.Succeed)
                    {
                        Debug.LogError($"{packageType} 本地清单激活失败: {manifestOp.Error}");
                        return (package, false);
                    }
                }
                else
                {
                    Debug.LogError($"{packageType} 本地版本获取失败: {versionOp.Error}");
                    return (package, false);
                }
            }

            return (package, true);
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
                var operation = package.RequestPackageVersionAsync(false); // false：禁止追加时间戳破坏OSS签名
                await operation;

                if (operation.Status != EOperationStatus.Succeed)
                {
                    Debug.LogError($"获取资源版本失败: {operation.Error}");
                    await ShowRetryUIDialogAsync("获取资源版本失败，请检查网络并重试"); // 阻断等待重试
                }
                else
                {
                    packageVersion = operation.PackageVersion;
                    Debug.Log($"Updated {package.PackageName} Version : {packageVersion}");
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
                    Debug.LogError($"更新清单失败: {operation2.Error}");
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
                    Debug.LogError("下载热更包失败");
                    await ShowRetryUIDialogAsync("下载更新文件失败，请检查网络并重试");
                }
            }

            // [状态4] 清理旧版无用缓存（释放磁盘空间）
            await ClearPackageUnusedCacheBundleFiles(package);
        }

        // 模拟重试UI弹窗 (TODO: 需绑定真实UGUI界面与按钮)
        private async UniTask ShowRetryUIDialogAsync(string message)
        {
            Debug.LogWarning($"[UI Mock] 弹出错误提示面板: {message}");
            Debug.LogWarning("[UI Mock] 等待玩家点击重试...");
            await UniTask.Delay(TimeSpan.FromSeconds(2f));
        }

        private async UniTask<InitializationOperation> InitPackageEditorSimulateMode(ResourcePackage package, string packageName)
        {
            // 编辑器模拟：直接使用项目绝对路径读取Asset，无需打AB包
            var buildResult = EditorSimulateModeHelper.SimulateBuild(packageName);
            var fileSystemParams = FileSystemParameters.CreateDefaultEditorFileSystemParameters(buildResult.PackageRootDirectory);
            var createParameters = new EditorSimulateModeParameters { EditorFileSystemParameters = fileSystemParams };

            var initOperation = package.InitializeAsync(createParameters);
            await initOperation;
            return initOperation;
        }

        private async UniTask<InitializationOperation> InitPackageHostPlayMode(ResourcePackage package, PackageType packageType, VersionType versionType)
        {
            // 联机模式：内置目录兜底 + 沙盒缓存读写
            var packVersion = OnlineConfig.Data[versionType.ToString()].ToString();
            Debug.Log($"Init HostPlayMode: {versionType} = {packVersion}");
            
            IRemoteServices remoteServices = new RemoteServices(packVersion, packageType);

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

        private async UniTask ClearPackageUnusedCacheBundleFiles(ResourcePackage package)
        {
            var operation = package.ClearCacheFilesAsync(EFileClearMode.ClearUnusedBundleFiles);
            await operation;

            if (operation.Status == EOperationStatus.Succeed)
                Debug.Log($"{package.PackageName} 缓存清理成功");
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
            int failedTryAgain = 3;     // 失败自动重试次数

            var downloader = package.CreateResourceDownloader(downloadingMaxNum, failedTryAgain);

            // 无需下载时直接通过
            if (downloader.TotalDownloadCount == 0)
            {
                Debug.Log($"{package.PackageName} 已是最新，无需下载");
                return true;
            }

            float totalDownloadMb = downloader.TotalDownloadBytes * 1.0f / (1024 * 1024);
            Debug.Log($"{package.PackageName} 需下载文件数: {downloader.TotalDownloadCount}\n 总大小: {totalDownloadMb:F2} MB");

            // 绑定回调委托
            downloader.DownloadErrorCallback = OnDownloadErrorFunction;
            downloader.DownloadUpdateCallback = OnDownloadProgressUpdateFunction;
            downloader.DownloadFinishCallback = OnDownloadOverFunction;
            downloader.DownloadFileBeginCallback = OnStartDownloadFileFunction;

            // 开启并等待下载完成
            downloader.BeginDownload();
            await downloader;

            if (downloader.Status == EOperationStatus.Succeed)
            {
                Debug.Log($"{package.PackageName} 下载完成");
                return true;
            }
            else
            {
                Debug.Log($"{package.PackageName} 下载失败");
                return false;
            }
        }

        private void OnStartDownloadFileFunction(DownloadFileData data) => Debug.Log($"开始下载：{data.FileName}，大小：{data.FileSize}");
        private void OnDownloadOverFunction(DownloaderFinishData data) => Debug.Log("下载" + (data.Succeed ? "成功" : "失败"));
        private void OnDownloadErrorFunction(DownloadErrorData data) => Debug.Log($"下载出错：{data.FileName}，错误：{data.ErrorInfo}");
        
        private void OnDownloadProgressUpdateFunction(DownloadUpdateData data)
        {
            // TODO: 发送给UI层更新 Slider 进度条
            // Debug.Log($"已下载文件数：{data.CurrentDownloadCount}/{data.TotalDownloadCount}");
        }

        #endregion

        #region 启动游戏

        /// <summary>
        /// 资源全部就绪，切换到游戏主场景
        /// </summary>
        private async UniTask LoadScene()
        {
            string location = "Game";
            var sceneMode = LoadSceneMode.Single;
            var physicsMode = LocalPhysicsMode.None;
            bool suspendLoad = false; // 是否在加载到 90% 时挂起，这里选择直接加载完切场景
            
            // 使用 YooAsset 提供的场景异步加载接口
            SceneHandle handle = _assetPackage.LoadSceneAsync(location, sceneMode, physicsMode, suspendLoad);
            await handle;
            
            Debug.Log($"场景切换成功: {handle.SceneName}");
        }

        #endregion
    }
}