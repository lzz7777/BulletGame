using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YooAsset;
using UnityEngine.Networking;
using System.Linq;

namespace XN
{
    public class YooAssetManager : MonoSingleton<YooAssetManager>
    {
        /// <summary>
        /// 远端资源地址查询服务类
        /// </summary>
        public class RemoteServices : IRemoteServices
        {
            private readonly string _defaultHostServer;
            private readonly string _fallbackHostServer;

            public RemoteServices(string defaultHostServer, string fallbackHostServer)
            {
                _defaultHostServer = defaultHostServer;
                _fallbackHostServer = fallbackHostServer;
            }

            string IRemoteServices.GetRemoteMainURL(string fileName)
            {
                return $"{_defaultHostServer}/{fileName}";
            }

            string IRemoteServices.GetRemoteFallbackURL(string fileName)
            {
                return $"{_fallbackHostServer}/{fileName}";
            }
        }

        public static string DefaultPackageName = "DefaultPackage";
        public static string ConfigPackageName = "ConfigPackage";
        /// <summary>
        /// 默认包,大部分资源都在
        /// </summary>
        public static ResourcePackage DefaultPackage => YooAssets.GetPackage(DefaultPackageName);
        public static ResourcePackage ConfigPackage => YooAssets.GetPackage(ConfigPackageName);
        public bool IsInitialized { get; private set; }
        private UniTaskCompletionSource<bool> _initTcs = new ();
        private readonly Dictionary<string, Texture2D> _httpTextureCache = new ();
        /// <summary>
        /// 主要是拿头像
        /// </summary>
        private readonly Dictionary<string, Sprite> _httpSpriteCache = new ();

        protected override void OnInit()
        {
            InitPackage();
        }

        protected override void OnRemove()
        {
            
        }
        
        /// <summary>
        /// 核心包初始化流程。
        /// 负责初始化资源系统、创建资源包、设置运行模式，并更新资源清单。
        /// </summary>
        public async UniTask InitPackage()
        {
            // 如果 YooAssets 已经初始化过，标记当前管理器为已初始化并直接返回
            if (YooAssets.Initialized)
            {
                IsInitialized = true;
                return;
            }
            
            // 1. 基础系统初始化
            YooAssets.Initialize();

            //初始化
            var (defaultPackage, defaultSucceed) = await InitPackageSingle(DefaultPackageName);
            if (!defaultSucceed)
            {
                Debug.LogError($"{DefaultPackageName} init failed");
                return;
            }
            // 设置 DefaultPackage 为默认包，后续不传包名的加载接口默认从这里读
            YooAssets.SetDefaultPackage(defaultPackage);

            var (confPackage, confSucceed) = await InitPackageSingle(ConfigPackageName);
            if (!confSucceed)
            {
                Debug.LogError($"{ConfigPackageName} init failed");
                return;
            }
            
            // 6. 初始化全部完成，标记状态并通知等待的任务继续执行
            IsInitialized = true;
            _initTcs.TrySetResult(true);
            // 额外延迟 1 秒，确保底层状态稳定
            await UniTask.Delay(1000);
        }
    
        private async UniTask<(ResourcePackage, bool)> InitPackageSingle(string packageName)
        {
            var package = YooAssets.CreatePackage(packageName);

            // 3. 根据当前运行环境设置资源的加载模式并执行包的初始化
#if UNITY_EDITOR
            // 编辑器下：使用 Simulate 模式，直接读取 Asset 目录下的文件，无需构建 Bundle
            await InitPackageEditorSimulateMode(package, packageName);
#else
            // 非编辑器下（真机/打包版）：使用 OfflinePlayMode 模式，读取内置的 StreamingAssets
            await InitPackageOfflinePlayMode(package);
#endif

            // 4. 更新 DefaultPackage 的资源清单
            // 向服务器（或本地）请求最新的资源版本号，必须传入 false 关闭时间戳，防止破坏 OSS 签名验证
            var operation1 = package.RequestPackageVersionAsync(false);
            await operation1;
            if (operation1.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"请求资源清单的版本信息失败：{operation1.Error}");
                return (package, false);
            }
            
            // 使用获取到的版本号更新资源清单 manifest
            var operation2 = package.UpdatePackageManifestAsync(operation1.PackageVersion, 60);
            await operation2;
            if (operation2.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"传入的版本信息更新资源清单失败：{operation2.Error}");
                return (package, false);
            }

            return (package, true);
        }

        /// <summary>
        /// 初始化编辑器模拟模式。
        /// 通过模拟构建拿到虚拟的 PackageRoot，直接读取源码资产。
        /// </summary>
        /// <param name="package">要初始化的资源包</param>
        /// <param name="packageName">包名，用于模拟构建参数</param>
        private IEnumerator InitPackageEditorSimulateMode(ResourcePackage package, string packageName)
        {  
            // 获取模拟构建结果
            var buildResult = EditorSimulateModeHelper.SimulateBuild(packageName);    
            var packageRoot = buildResult.PackageRootDirectory;
            // 创建编辑器专用的文件系统参数
            var fileSystemParams = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
    
            var createParameters = new EditorSimulateModeParameters();
            createParameters.EditorFileSystemParameters = fileSystemParams;
            
            // 执行包的异步初始化
            var initOperation = package.InitializeAsync(createParameters);
            yield return initOperation;
    
            if(initOperation.Status == EOperationStatus.Succeed)
                Debug.Log($"{packageName} 资源包初始化成功！");
            else 
                Debug.LogError($"{packageName} 资源包初始化失败：{initOperation.Error}");
        }
        
        /// <summary>
        /// 初始化离线运行模式（单机模式）。
        /// 仅从内置的 StreamingAssets 目录加载资源，不涉及网络下载。
        /// </summary>
        /// <param name="package">要初始化的资源包</param>
        private IEnumerator InitPackageOfflinePlayMode(ResourcePackage package)
        {
            // 创建内置文件系统参数（默认指向 StreamingAssets 下的 yoo 目录）
            var fileSystemParams = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
    
            var createParameters = new OfflinePlayModeParameters();
            createParameters.BuildinFileSystemParameters = fileSystemParams;
    
            // 执行包的异步初始化
            var initOperation = package.InitializeAsync(createParameters);
            yield return initOperation;
    
            if(initOperation.Status == EOperationStatus.Succeed)
                Debug.Log($"{package.PackageName} 资源包初始化成功！");
            else 
                Debug.LogError($"{package.PackageName} 资源包初始化失败：{initOperation.Error}");
        }   
        
        /// <summary>
        /// 初始化联机运行模式（热更模式）。
        /// 支持从远端 CDN 下载最新资源，并缓存在本地。
        /// </summary>
        /// <param name="package">要初始化的资源包</param>
        private IEnumerator InitPackageHostPlayMode(ResourcePackage package)
        {
            // 配置主力和备用的远端下载服务器地址
            string defaultHostServer = "http://127.0.0.1/CDN/Android/v1.0";
            string fallbackHostServer = "http://127.0.0.1/CDN/Android/v1.0";
            
            // 实例化远程服务类，供底层拼接下载 URL
            IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
            
            // 创建缓存文件系统（用于存取下载到沙盒的资源）和内置文件系统（用于存取 StreamingAssets 里的首包资源）
            var cacheFileSystemParams = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);
            var buildinFileSystemParams = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();   
    
            var createParameters = new HostPlayModeParameters();
            createParameters.BuildinFileSystemParameters = buildinFileSystemParams; 
            createParameters.CacheFileSystemParameters = cacheFileSystemParams;
    
            // 执行包的异步初始化
            var initOperation = package.InitializeAsync(createParameters);
            yield return initOperation;
    
            if(initOperation.Status == EOperationStatus.Succeed)
                Debug.Log($"{package.PackageName} 资源包初始化成功！");
            else 
                Debug.LogError($"{package.PackageName} 资源包初始化失败：{initOperation.Error}");
        }
        
        private async UniTask EnsureInitialized()
        {
            if (IsInitialized) return;
            await _initTcs.Task;
        }

        // 补充YooAsset下加载接口（通用封装）

        /// <summary>
        /// 异步加载任意资源（返回对象）。可选自动释放句柄。
        /// </summary>
        public async UniTask<T> LoadAssetAsync<T>(string location, bool autoRelease = false, CancellationToken token = default)
            where T : Object
        {
            await EnsureInitialized();
            var package = DefaultPackage;
            var handle = package.LoadAssetAsync<T>(location);
            await handle.Task;
            if (token.IsCancellationRequested)
            {
                handle.Release();
                return null;
            }
            if (handle.Status != EOperationStatus.Succeed || handle.AssetObject == null)
            {
                Debug.LogError($"LoadAssetAsync 失败：{location} | {handle.LastError}");
                handle.Release();
                return null;
            }
            var obj = handle.AssetObject as T;
            if (autoRelease) handle.Release();
            return obj;
        }

        /// <summary>
        /// 同步加载（谨慎使用，建议仅在初始化时）。
        /// </summary>
        public T LoadAssetSync<T>(string location, bool autoRelease = false) where T : Object
        {
            var package = DefaultPackage;
            var handle = package.LoadAssetSync<T>(location);
            if (handle.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"LoadAssetSync 失败：{location}");
                if (!autoRelease) handle.Release();
                return null;
            }
            var obj = handle.AssetObject as T;
            if (autoRelease) handle.Release();
            return obj;
        }

        /// <summary>
        /// 异步实例化游戏对象（Prefab）。
        /// </summary>
        public async UniTask<GameObject> InstantiateAsync(string location, Transform parent = null, bool instantiateInWorldSpace = false)
        {
            await EnsureInitialized();
            var handle = DefaultPackage.LoadAssetAsync<GameObject>(location);
            await handle.Task;
            if (handle.Status != EOperationStatus.Succeed || handle.AssetObject == null)
            {
                Debug.LogError($"InstantiateAsync 失败：{location} | {handle.LastError}");
                handle.Release();
                return null;
            }
            var prefab = handle.AssetObject as GameObject;
            GameObject go = parent != null ? Object.Instantiate(prefab, parent, instantiateInWorldSpace) : Object.Instantiate(prefab);
            handle.Release();
            return go;
        }

        public GameObject InstantiateSync(string location, Transform parent = null, bool instantiateInWorldSpace = false)
        {
            var prefab = LoadAssetSync<GameObject>(location);
            GameObject go = parent != null ? Object.Instantiate(prefab, parent, instantiateInWorldSpace) : Object.Instantiate(prefab);
            return go;
        }
        
        /// <summary>
        /// 加载场景。
        /// </summary>
        public async UniTask<bool> LoadSceneAsync(string location, LoadSceneMode mode = LoadSceneMode.Additive, LocalPhysicsMode physicsMode = LocalPhysicsMode.None)
        {
            await EnsureInitialized();
            var package = DefaultPackage;
            var op = package.LoadSceneAsync(location, mode, physicsMode);
            await op.Task;
            if (op.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"LoadSceneAsync 失败：{location} | {op.LastError}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 异步加载原始文件（如配置/音频等非资源对象），返回字节数组。
        /// </summary>
        public async UniTask<byte[]> LoadRawBytesAsync(string location)
        {
            // 为兼容 2.3.12：使用 TextAsset 方式读取原始字节，更稳妥
            var ta = await LoadAssetAsync<TextAsset>(location, autoRelease: true);
            return ta != null ? ta.bytes : null;
        }

        /// <summary>
        /// 常用类型快捷方法：Sprite、Texture2D、AudioClip、TextAsset、Material。
        /// </summary>
        public async UniTask<Sprite> LoadSpriteAsync(string location, bool autoRelease = false, CancellationToken token = default)
        {
            var loc = location ?? string.Empty;
            if (loc.StartsWith("http://") || loc.StartsWith("https://"))
            {
                var key = loc.ToLowerInvariant();
                if (_httpSpriteCache.TryGetValue(key, out var cached)) return cached;
                var tex = await LoadTextureFromUrlAsync(loc, token);
                if (token.IsCancellationRequested || tex == null) return null;
                var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                if (!autoRelease) _httpSpriteCache[key] = sprite;
                return sprite;
            }
            return await LoadAssetAsync<Sprite>(location, autoRelease, token);
        }

        public async UniTask<Sprite> LoadSpriteAsync(string location, Image image, bool setNative = false)
        {
            var sprite = await LoadSpriteAsync(location);
            if (sprite == null) return null;
            image.sprite = sprite;
            if (setNative)
            {
                image.SetNativeSize();
            }
            return sprite;
        }
        
        public async UniTask<Sprite> LoadSpriteAsync(string location, SpriteRenderer spriteRand)
        {
            var sprite = await LoadSpriteAsync(location);
            if (sprite == null) return null;
            spriteRand.sprite = sprite;
            return sprite;
        }
        
        public UniTask<Texture2D> LoadTextureAsync(string location, bool autoRelease = false, CancellationToken token = default) => LoadAssetAsync<Texture2D>(location, autoRelease, token);

        private async UniTask<Texture2D> LoadTextureFromUrlAsync(string url, CancellationToken token = default)
        {
            var key = (url ?? string.Empty).ToLowerInvariant();
            if (_httpTextureCache.TryGetValue(key, out var cached)) return cached;
            using var req = UnityWebRequestTexture.GetTexture(url, true);
            try
            {
                await req.SendWebRequest();
                if (token.IsCancellationRequested) { req.Abort(); return null; }
                if (req.result == UnityWebRequest.Result.Success)
                {
                    var tex = DownloadHandlerTexture.GetContent(req);
                    if (tex != null) _httpTextureCache[key] = tex;
                    return tex;
                }
                else
                {
                    Debug.Log("Download Error:" + req.error);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning(e);
            }
            return null;
        }
        public UniTask<AudioClip> LoadAudioAsync(string location, bool autoRelease = false, CancellationToken token = default) => LoadAssetAsync<AudioClip>(location, autoRelease, token);
        public UniTask<TextAsset> LoadTextAsync(string location, bool autoRelease = false, CancellationToken token = default) => LoadAssetAsync<TextAsset>(location, autoRelease, token);
        public UniTask<Material> LoadMaterialAsync(string location, bool autoRelease = false, CancellationToken token = default) => LoadAssetAsync<Material>(location, autoRelease, token);

        public UniTask<SkeletonDataAsset> LoadSkeletonAsync(string location, bool autoRelease = false, CancellationToken token = default) => LoadAssetAsync<SkeletonDataAsset>(location, autoRelease, token);
        public UniTask<GameObject> LoadGameObjectAsync(string location, bool autoRelease = false, CancellationToken token = default) => LoadAssetAsync<GameObject>(location, autoRelease, token);

        /// <summary>
        /// 判断资源是否存在于包内。
        /// </summary>
        public bool Exists(string location)
        {
            // 兼容旧版：没有 IsContainsAsset 时，尝试同步加载判断是否存在
            var handle = DefaultPackage.LoadAssetSync<Object>(location);
            bool ok = handle.Status == EOperationStatus.Succeed && handle.AssetObject != null;
            handle.Release();
            return ok;
        }

        /// <summary>
        /// 释放未使用资源并进行 GC（建议在场景切换后调用）。
        /// </summary>
        public async UniTask ReleaseUnusedAsync()
        {
            // 兼容 2.3.12：不调用 YooAssets.UnloadUnusedAssets
            await Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

        /// <summary>
        /// 通过标签获取文件数量
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public int GetGroupTagFileNum(string tag)
        {
            var package = DefaultPackage;
            var assetInfos = package.GetAssetInfos(tag);
            
            Debug.Log($"yooasset GetGroupTagFileNum:{assetInfos.Length}");
            
            return assetInfos.Length;
        }
    }
}
