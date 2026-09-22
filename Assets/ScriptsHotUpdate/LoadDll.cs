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
    /// AOT 层热更启动入口。
    /// 负责在主工程中完成以下职责：
    /// 1. 初始化代码包（CodesPackage）。
    /// 2. 在联机模式下拉取最新 DLL 与 AOT 元数据资源。
    /// 3. 为 HybridCLR 补充 AOT 元数据，规避泛型实例化缺失导致的运行时问题。
    /// 4. 加载 HotUpdate 程序集，并把控制权移交给热更层入口。
    ///
    /// 这个类本身不负责业务资源和配置包更新，
    /// 它的任务是先把“代码执行环境”准备好，再交给 HotUpdateAsset 继续后续流程。
    /// </summary>
    public class LoadDll : MonoBehaviour
    {
        /// <summary>
        /// 当前运行模式。
        /// EditorSimulateMode：编辑器模拟模式，直接读工程资源，适合开发调试。
        /// HostPlayMode：联机模式，从远端拉取版本、Manifest 和差异包。
        /// OfflinePlayMode：离线模式，仅使用包体内置资源，不访问网络。
        /// </summary>
        public EPlayMode playMode = EPlayMode.HostPlayMode;

        /// <summary>
        /// 代码包实例。
        /// CodesPackage 中存放 HotUpdate.dll 以及补充元数据所需的 AOT DLL。
        /// </summary>
        private ResourcePackage _codesPackage;

        /// <summary>
        /// YooAsset 中代码包的固定包名。
        /// </summary>
        public const string CodesPackageName = "CodesPackage";

        /// <summary>
        /// 在线配置文件名，用于在联机模式下拿到远端版本配置。
        /// </summary>
        private const string OnlineConfigName = "BulletGame_1.json";

        void Start()
        {
            // 强制保留 AOT 桥接层代码，避免 IL2CPP 因静态裁剪把必要类型删除。
            XN.AOT.AtlasEventWrapper.Preserve();

            // 启动代码热更主流程。
            Init();
            
            // 启动节点常驻，后续热更层会继续挂载组件并接管启动流程。
            DontDestroyOnLoad(this);
        }

        #region 启动主流程

        /// <summary>
        /// 启动前置初始化。
        /// 联机模式先读取在线配置，拿到代码包版本，再进入 YooAsset 代码热更流程。
        /// </summary>
        private async UniTask Init()
        {
            // 只有联机模式才需要先从服务端拉在线配置。
            if (playMode == EPlayMode.HostPlayMode)
                await OnlineConfigHelper.Init(OnlineConfigName);

            // 在线配置就绪后，继续初始化代码包并加载热更 DLL。
            InitYooAssets();
        }

        /// <summary>
        /// 初始化代码包并加载热更程序集。
        /// 这是“先代码、后资源”双段式启动的第一段：
        /// 先保证热更代码已经可执行，后续资源包和配置包才交由热更层接手。
        /// </summary>
        private async UniTask InitYooAssets()
        {
            // YooAsset 全局初始化只需要做一次。
            YooAssets.Initialize();

            // 创建代码包，并按照当前运行模式选择不同的初始化参数。
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


            // 先确保代码包初始化成功，否则后续版本请求、清单更新和 DLL 加载都没有意义。
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

            // 所有模式都必须激活 Manifest。
            // 否则后续 LoadAssetAsync 会因为没有活动清单而报错。
            if (playMode == EPlayMode.HostPlayMode)
            {
                // 联机模式需要真正走远端热更流程。
                await UpdatePackageHostPlayMode(package);
            }
            else
            {
                // 编辑器模拟 / 离线模式虽然不下载远端资源，
                // 但仍然要激活本地版本对应的 Manifest。
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

            // 代码包里不仅有 HotUpdate.dll，
            // 还有 HybridCLR 运行时补元数据所需的几个 AOT 程序集。
            var assets = new List<string> { "HotUpdate.dll" }.Concat(AOTMetaAssemblyFiles);
            foreach (var asset in assets)
            {
                var handle = package.LoadAssetAsync<TextAsset>(asset);
                await handle;

                // 缓存 DLL 二进制资源。
                // 后面无论是 Assembly.Load 还是 RuntimeApi.LoadMetadataForAOTAssembly，
                // 都从这里统一取字节流。
                var assetObj = handle.AssetObject as TextAsset;
                s_assetDatas[asset] = assetObj;
                Debug.Log($"dll:{asset}   {assetObj == null}");
            }

            Debug.Log($"更新成功，版本 {_codesPackage.GetPackageVersion()}");

            // 代码环境已准备完毕，开始把控制权移交给热更层。
            StartGame();
        }

        /// <summary>
        /// 联机模式下的代码包热更状态机。
        /// 标准流程：
        /// 1. 请求远端版本号。
        /// 2. 下载并激活对应 Manifest。
        /// 3. 计算差异并下载缺失文件。
        /// 4. 清理旧版本缓存。
        /// </summary>
        private async UniTask UpdatePackageHostPlayMode(ResourcePackage package)
        {
            string packageVersion = string.Empty;

            // 状态 1：请求代码包版本号。
            bool requestVersionSuccess = false;
            while (!requestVersionSuccess)
            {
                // false：禁止追加时间戳。
                // 某些 CDN / OSS 会对 URL 做签名校验，额外参数可能导致签名失效。
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

            // 状态 2：更新并激活对应版本的 Manifest。
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

            // 状态 3：根据 Manifest 差异下载代码包内容。
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

            // 状态 4：清理旧版缓存，避免沙盒目录无限膨胀。
            await ClearPackageUnusedCacheBundleFiles();
        }

        /// <summary>
        /// 编辑器模拟模式下初始化代码包。
        /// 直接读取工程资源数据库，不经过真实 AB 文件。
        /// </summary>
        private async UniTask<InitializationOperation> InitPackageEditorSimulateMode(ResourcePackage package)
        {
            // 编辑器模拟：直接映射到项目输出目录，适合本地开发快速验证。
            var buildResult = EditorSimulateModeHelper.SimulateBuild(CodesPackageName);
            var fileSystemParams =
                FileSystemParameters.CreateDefaultEditorFileSystemParameters(buildResult.PackageRootDirectory);
            var createParameters = new EditorSimulateModeParameters { EditorFileSystemParameters = fileSystemParams };

            var initOperation = package.InitializeAsync(createParameters);
            await initOperation;
            return initOperation;
        }

        /// <summary>
        /// 联机模式下初始化代码包。
        /// 组合“包体内置目录 + 缓存目录 + 远端服务”三部分文件系统。
        /// </summary>
        private async UniTask<InitializationOperation> InitPackageHostPlayMode(ResourcePackage package)
        {
            // 包体内置目录负责首包兜底，缓存目录负责存放热更后的最新文件。
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

        /// <summary>
        /// 离线模式下初始化代码包。
        /// 只使用包体内置资源，不做任何网络请求。
        /// </summary>
        private async UniTask<InitializationOperation> InitPackageOfflinePlayMode(ResourcePackage package)
        {
            // 离线模式适合无网络环境或本地首包验证。
            var fileSystemParams = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
            var createParameters = new OfflinePlayModeParameters { BuildinFileSystemParameters = fileSystemParams };

            var initOperation = package.InitializeAsync(createParameters);
            await initOperation;
            return initOperation;
        }

        /// <summary>
        /// AOT 层的重试弹窗占位实现。
        /// 当前仅用日志和延时模拟，后续可接入真实加载界面。
        /// </summary>
        private async UniTask ShowRetryUIDialogAsync(string message)
        {
            Debug.LogWarning($"[AOT UI Mock] 弹出错误提示面板: {message}");
            Debug.LogWarning("[AOT UI Mock] 等待玩家点击重试...");
            await UniTask.Delay(TimeSpan.FromSeconds(2f)); // 模拟等待玩家点击
        }

        /// <summary>
        /// 清理代码包旧缓存。
        /// 只清掉当前版本不再使用的 Bundle，避免误删仍在引用的内容。
        /// </summary>
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

        #region 下载代码包资源

        /// <summary>
        /// 执行代码包差异下载。
        /// 这里只关心“代码相关资源是否齐全”，并不处理业务资源包。
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

            // 挂接下载生命周期回调，后续可直接转发给 UI 层。
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

        #region HybridCLR 元数据补充

        /// <summary>
        /// 需要补充元数据的 AOT 程序集列表。
        /// 当热更代码引用这些 AOT 程序集中的泛型能力时，
        /// HybridCLR 需要它们的元数据来完成解释执行和泛型补全。
        /// </summary>
        private static List<string> AOTMetaAssemblyFiles { get; } =
            new() { "mscorlib.dll", "System.dll", "System.Core.dll", "UniTask.dll" };

        /// <summary>
        /// 已加载 DLL 资源缓存。
        /// Key 为资源名，Value 为 YooAsset 读取到的 TextAsset。
        /// </summary>
        private static Dictionary<string, TextAsset> s_assetDatas = new();

        /// <summary>
        /// 热更主程序集对象。
        /// 成功加载后，通过反射定位热更层入口。
        /// </summary>
        private static Assembly _hotUpdateAss;

        /// <summary>
        /// 从缓存中读取 DLL 字节流。
        /// </summary>
        public static byte[] ReadBytesFromStreamingAssets(string dllName)
        {
            if (s_assetDatas.ContainsKey(dllName))
                return s_assetDatas[dllName].bytes;
            return Array.Empty<byte>();
        }

        /// <summary>
        /// 遍历 AOT 程序集列表，向 HybridCLR 注入元数据。
        /// 这是热更项目里最关键的兜底步骤之一，
        /// 否则运行时遇到未提前生成的泛型实例时，可能直接抛异常甚至闪退。
        /// </summary>
        private static void LoadMetadataForAOTAssemblies()
        {
            // SuperSet 模式兼容性最高，适合热更场景。
            HomologousImageMode mode = HomologousImageMode.SuperSet;

            foreach (var aotDllName in AOTMetaAssemblyFiles)
            {
                byte[] dllBytes = ReadBytesFromStreamingAssets(aotDllName);

                // 把 AOT 程序集的元数据交给 HybridCLR，
                // 让运行时在需要时可以正确处理泛型共享与解释执行。
                LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, mode);
                Debug.Log($"LoadMetadataForAOTAssembly:{aotDllName}. mode:{mode} ret:{err}");
            }
        }

        #endregion

        #region 启动交接

        /// <summary>
        /// 代码热更阶段的收尾。
        /// 顺序固定：
        /// 1. 预热 AOT 泛型。
        /// 2. 补充 AOT 元数据。
        /// 3. 加载热更主程序集。
        /// 4. 通过反射调用热更层入口。
        /// </summary>
        void StartGame()
        {
            // 预热常用泛型，减少首次触发时的运行时开销。
            AOTGenericWarmup.Init();

            // 补充 AOT 元数据，避免热更层访问某些泛型能力时出问题。
            LoadMetadataForAOTAssemblies();

            // 加载热更程序集。
            // 编辑器和真机环境的加载方式不同：
            // - 真机：从下载到的 DLL 字节流动态装载。
            // - 编辑器：直接从当前 AppDomain 找已经编译好的程序集。
#if !UNITY_EDITOR
            _hotUpdateAss = Assembly.Load(ReadBytesFromStreamingAssets("HotUpdate.dll"));
#else
            _hotUpdateAss = System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "HotUpdate");
#endif
            Debug.Log("热更准备完毕，即将移交控制权并切换场景");

            // AOT 层到此为止，后续资源更新和业务场景加载交给热更层完成。
            LoadAsset();
        }

        /// <summary>
        /// 通过反射调用热更层入口。
        /// 这里显式按签名查找方法，避免未来重载导致入口歧义。
        /// </summary>
        private void LoadAsset()
        {
            // 约定热更层入口类为 XN.HotUpdateAsset。
            Type mainType = _hotUpdateAss.GetType("XN.HotUpdateAsset");

            // 约定入口方法签名为 StartLoadAssets(EPlayMode)。
            MethodInfo startMethod = mainType.GetMethod("StartLoadAssets", new Type[] { typeof(YooAsset.EPlayMode) });

            if (startMethod != null)
            {
                // 把当前运行模式透传给热更层，让后续资源包流程保持一致。
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
