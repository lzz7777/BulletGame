using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;

namespace XN
{
    /// <summary>
    /// 热更层启动入口。
    /// 该类由 AOT 层的 LoadDll 通过反射唤起，
    /// 负责接管后续业务资源与配置更新流程，并在准备完毕后进入主场景。
    ///
    /// 如果说 LoadDll 解决的是“代码能不能先跑起来”，
    /// 那么 HotUpdateAsset 解决的就是“资源和配置能不能切到最新版本并安全进入游戏”。
    /// </summary>
    public class HotUpdateAsset : MonoBehaviour
    {
        /// <summary>
        /// 主资源包实例。
        /// 默认包设置为它后，不带包名的加载接口都会优先走这里。
        /// </summary>
        private ResourcePackage _assetPackage;

        /// <summary>
        /// 主资源包名称，存放场景、Prefab、贴图、音频等核心业务资源。
        /// </summary>
        private const string AssetPackageName = "DefaultPackage";

        /// <summary>
        /// 配置包名称，存放配表、配置文件等轻量但高频迭代内容。
        /// </summary>
        private const string ConfigPackageName = "ConfigPackage";

        private void Start()
        {
            // 组件挂上后，正式开始热更层自己的资源初始化流程。
            InitYooAssets();
        }

        /// <summary>
        /// AOT 层通过反射调用的唯一入口。
        /// 这里只做两件事：
        /// 1. 接收并缓存运行模式。
        /// 2. 把 HotUpdateAsset 挂到常驻启动节点上，触发 MonoBehaviour 生命周期。
        /// </summary>
        public static void StartLoadAssets(EPlayMode mode)
        {
            Debug.Log($"热更层接收到了当前运行模式: {mode}");
            GameConst.PlayMode = mode;
            
            // 复用 AOT 层的启动节点，避免重复创建启动器对象。
            GameObject.Find("LoadDll").AddComponent<HotUpdateAsset>();
        }

        #region 资源与配置包启动流程

        /// <summary>
        /// 热更层总启动流程。
        /// 顺序为：
        /// 1. 初始化并更新主资源包。
        /// 2. 初始化并更新配置包。
        /// 3. 挂载资源管理器。
        /// 4. 切换到主场景。
        /// </summary>
        private async UniTask InitYooAssets()
        {
            // 资源包通常体积最大，且场景加载直接依赖它，所以优先初始化。
            var (assetPackage, defaultSucceed) = await InitPackageSingle(AssetPackageName, PackageType.Asset, VersionType.AssetVersion);
            if (!defaultSucceed)
            {
                Debug.LogError($"{AssetPackageName} 初始化失败");
                return;
            }

            // 默认包设置完成后，YooAssets.LoadAssetAsync 这类接口可以省略包名。
            YooAssets.SetDefaultPackage(assetPackage);
            _assetPackage = assetPackage;

            // 配置包与资源包拆开，方便小包高频热更。
            var (confPackage, confSucceed) = await InitPackageSingle(ConfigPackageName, PackageType.Config, VersionType.ConfigVersion);
            if (!confSucceed)
            {
                Debug.LogError($"{ConfigPackageName} 初始化失败");
                return;
            }

            // 资源管理器建立在热更层，后续业务统一走这一套加载入口。
            gameObject.AddComponent<YooAssetManager>();
            
            // 至此主资源和配置都已经可用，可以安全进入主场景。
            LoadScene();
        }

        /// <summary>
        /// 单个包的标准初始化流程。
        /// 该方法把“按模式初始化 + 激活 Manifest + 联机下载”三件事封装成统一模板，
        /// 避免 Asset 包和 Config 包重复写两套近似逻辑。
        /// </summary>
        private async UniTask<(ResourcePackage, bool)> InitPackageSingle(string packageName, PackageType packageType, VersionType versionType)
        {
            var package = YooAssets.CreatePackage(packageName);
            InitializationOperation initializationOperation = null;

            // 第一步：根据运行模式初始化文件系统。
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

            // 如果初始化失败，后续版本查询和资源访问都无法继续。
            if (initializationOperation?.Status == EOperationStatus.Succeed)
            {
                Debug.Log($"{packageType} 包初始化成功！");
            }
            else
            {
                Debug.LogError($"{packageType} 包初始化失败：{initializationOperation?.Error}");
                return (package, false); // 失败阻断
            }

            // 第二步：所有模式都必须激活 Manifest。
            // 只有联机模式才需要访问远端版本与下载差异包。
            if (GameConst.PlayMode == EPlayMode.HostPlayMode)
            {
                // 联机模式走完整热更状态机。
                await UpdatePackageHostPlayMode(package);
            }
            else
            {
                // 编辑器模拟 / 离线模式虽然不联网，
                // 但依然需要激活本地版本的 Manifest 才能正常加载资源。
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
        /// 联机模式下的标准热更状态机。
        /// 对 Asset 包和 Config 包都通用：
        /// 1. 拉远端版本号。
        /// 2. 更新 Manifest。
        /// 3. 下载差异资源。
        /// 4. 清理旧缓存。
        /// </summary>
        private async UniTask UpdatePackageHostPlayMode(ResourcePackage package)
        {
            string packageVersion = string.Empty;

            // 状态 1：请求远端版本号。
            bool requestVersionSuccess = false;
            while (!requestVersionSuccess)
            {
                // false：禁止追加时间戳，避免某些 OSS/CDN 的签名 URL 被破坏。
                var operation = package.RequestPackageVersionAsync(false);
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

            // 状态 2：下载并激活该版本对应的 Manifest。
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

            // 状态 3：按差异列表下载缺失资源。
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

            // 状态 4：清理历史无用缓存，避免持久化目录无限增长。
            await ClearPackageUnusedCacheBundleFiles(package);
        }

        /// <summary>
        /// 热更层的重试弹窗占位实现。
        /// 当前仅用于模拟真实 UI 阻塞等待流程。
        /// </summary>
        private async UniTask ShowRetryUIDialogAsync(string message)
        {
            Debug.LogWarning($"[UI Mock] 弹出错误提示面板: {message}");
            Debug.LogWarning("[UI Mock] 等待玩家点击重试...");
            await UniTask.Delay(TimeSpan.FromSeconds(2f));
        }

        /// <summary>
        /// 编辑器模拟模式初始化。
        /// 直接读取项目资源数据库，不依赖真实 AB 文件。
        /// </summary>
        private async UniTask<InitializationOperation> InitPackageEditorSimulateMode(ResourcePackage package, string packageName)
        {
            // 适合本地开发期高频验证，免去打包等待时间。
            var buildResult = EditorSimulateModeHelper.SimulateBuild(packageName);
            var fileSystemParams = FileSystemParameters.CreateDefaultEditorFileSystemParameters(buildResult.PackageRootDirectory);
            var createParameters = new EditorSimulateModeParameters { EditorFileSystemParameters = fileSystemParams };

            var initOperation = package.InitializeAsync(createParameters);
            await initOperation;
            return initOperation;
        }

        /// <summary>
        /// 联机模式初始化。
        /// 远端版本、包类型与本地缓存目录共同决定运行时读到的最终资源内容。
        /// </summary>
        private async UniTask<InitializationOperation> InitPackageHostPlayMode(ResourcePackage package, PackageType packageType, VersionType versionType)
        {
            // 内置目录负责首包可用，缓存目录负责接收热更后的最新资源。
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

        /// <summary>
        /// 离线模式初始化。
        /// 只读取包体内置资源，不与远端交互。
        /// </summary>
        private async UniTask<InitializationOperation> InitPackageOfflinePlayMode(ResourcePackage package)
        {
            // 常用于无网络环境或首包流程验证。
            var fileSystemParams = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
            var createParameters = new OfflinePlayModeParameters { BuildinFileSystemParameters = fileSystemParams };

            var initOperation = package.InitializeAsync(createParameters);
            await initOperation;
            return initOperation;
        }

        /// <summary>
        /// 清理指定包的旧缓存。
        /// 只移除当前版本不再需要的 Bundle 文件。
        /// </summary>
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

        #region 下载资源包内容

        /// <summary>
        /// 执行单个包的差异资源下载。
        /// 返回值表示这一轮下载是否完成成功，失败时由上层决定是否弹窗重试。
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

            // 挂上下载生命周期回调，方便后续接 UI 进度条或日志系统。
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

        #region 进入游戏

        /// <summary>
        /// 当资源包和配置包都准备完成后，异步切换到游戏主场景。
        /// 这里依赖 DefaultPackage 已经被设置为默认包。
        /// </summary>
        private async UniTask LoadScene()
        {
            string location = "Game";
            var sceneMode = LoadSceneMode.Single;
            var physicsMode = LocalPhysicsMode.None;
            bool suspendLoad = false; // 不做 90% 挂起，资源就绪后直接切场景。
            
            // 通过 YooAsset 场景接口加载，确保场景依赖资源也走统一资源体系。
            SceneHandle handle = _assetPackage.LoadSceneAsync(location, sceneMode, physicsMode, suspendLoad);
            await handle;
            
            Debug.Log($"场景切换成功: {handle.SceneName}");
        }

        #endregion
    }
}
