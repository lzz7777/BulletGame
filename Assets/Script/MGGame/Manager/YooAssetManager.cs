using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Spine.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YooAsset;
using UnityEngine.Networking;
using UnityEngine.U2D;

namespace XN
{
    public class YooAssetManager : MonoSingleton<YooAssetManager>
    {
        public static string DefaultPackageName = "DefaultPackage";
        public static string ConfigPackageName = "ConfigPackage";

        /// <summary>
        /// 默认包,大部分资源都在
        /// </summary>
        public static ResourcePackage DefaultPackage => YooAssets.GetPackage(DefaultPackageName);

        public bool IsInitialized { get; private set; }
        private UniTaskCompletionSource<bool> _initTcs = new();
        private readonly Dictionary<string, Texture2D> _httpTextureCache = new();

        /// <summary>
        /// 主要是拿头像
        /// </summary>
        private readonly Dictionary<string, Sprite> _httpSpriteCache = new();

        private readonly Dictionary<string, AssetHandle> _atlasHandleCache = new();

        protected override void OnInit()
        {
            InitPackage();
            XN.AOT.AtlasEventWrapper.RemoveListener(OnAtlasRequested);
            XN.AOT.AtlasEventWrapper.AddListener(OnAtlasRequested);
        }

        protected override void OnRemove()
        {
            XN.AOT.AtlasEventWrapper.RemoveListener(OnAtlasRequested);

            foreach (var handle in _atlasHandleCache.Values)
            {
                handle?.Release();
            }

            _atlasHandleCache.Clear();
        }

        [UnityEngine.Scripting.Preserve]
        private void OnAtlasRequested(string atlasName, System.Action<UnityEngine.U2D.SpriteAtlas> callback)
        {
            LoadAtlasForUGUIAsync(atlasName, callback).Forget();
        }

        private async UniTaskVoid LoadAtlasForUGUIAsync(string atlasName,
            System.Action<UnityEngine.U2D.SpriteAtlas> callback)
        {
            await EnsureInitialized();
            var atlas = await GetAtlasAsync(atlasName);
            if (atlas != null)
            {
                callback(atlas);
                Debug.Log($"[SpriteAtlas] UI图集延迟绑定成功: {atlasName}");
            }
            else
            {
                Debug.LogError($"[SpriteAtlas] UI图集延迟绑定失败: {atlasName}");
            }
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

            // 编辑器下：使用 Simulate 模式，直接读取 Asset 目录下的文件，无需构建 Bundle
            await InitPackageEditorSimulateMode(package, packageName);

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

            if (initOperation.Status == EOperationStatus.Succeed)
                Debug.Log($"{packageName} 资源包初始化成功！");
            else
                Debug.LogError($"{packageName} 资源包初始化失败：{initOperation.Error}");
        }

        private async UniTask EnsureInitialized()
        {
            if (IsInitialized) return;
            await _initTcs.Task;
        }

        // 补充YooAsset下加载接口（通用封装）

        /// <summary>
        /// 异步加载任意资源（返回对象）。
        /// 例如 TextAsset.bytes。Sprite/Texture/Material/AudioClip/Prefab 等对象型资源请勿使用自动释放。
        /// </summary>
        public async UniTask<T> LoadAssetAsync<T>(string location, CancellationToken token = default)
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
            return obj;
        }

        /// <summary>
        /// 返回 AssetHandle，由调用方自行管理生命周期。
        /// </summary>
        public async UniTask<AssetHandle> LoadAssetHandleAsync<T>(string location, CancellationToken token = default)
            where T : Object
        {
            await EnsureInitialized();
            var handle = DefaultPackage.LoadAssetAsync<T>(location);
            await handle.Task;

            if (token.IsCancellationRequested)
            {
                handle.Release();
                return null;
            }

            if (handle.Status != EOperationStatus.Succeed || handle.AssetObject == null)
            {
                Debug.LogError($"LoadAssetHandleAsync 失败：{location} | {handle.LastError}");
                handle.Release();
                return null;
            }

            return handle;
        }

        /// <summary>
        /// 同步加载（谨慎使用，建议仅在初始化时）。
        /// </summary>
        public T LoadAssetSync<T>(string location) where T : Object
        {
            var package = DefaultPackage;
            var handle = package.LoadAssetSync<T>(location);
            if (handle.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"LoadAssetSync 失败：{location}");
                handle.Release();
                return null;
            }

            var obj = handle.AssetObject as T;
            return obj;
        }

        /// <summary>
        /// 同步加载并返回 AssetHandle，由调用方自行管理生命周期。
        /// </summary>
        public AssetHandle LoadAssetHandleSync<T>(string location) where T : Object
        {
            var handle = DefaultPackage.LoadAssetSync<T>(location);
            if (handle.Status != EOperationStatus.Succeed || handle.AssetObject == null)
            {
                Debug.LogError($"LoadAssetHandleSync 失败：{location}");
                handle.Release();
                return null;
            }

            return handle;
        }

        /// <summary>
        /// 异步实例化游戏对象（Prefab）。
        /// </summary>
        public async UniTask<GameObject> InstantiateAsync(string location, Transform parent = null,
            bool instantiateInWorldSpace = false)
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
            GameObject go = parent != null
                ? Object.Instantiate(prefab, parent, instantiateInWorldSpace)
                : Object.Instantiate(prefab);
            handle.Release();
            return go;
        }

        public GameObject InstantiateSync(string location, Transform parent = null,
            bool instantiateInWorldSpace = false)
        {
            var prefab = LoadAssetSync<GameObject>(location);
            GameObject go = parent != null
                ? Object.Instantiate(prefab, parent, instantiateInWorldSpace)
                : Object.Instantiate(prefab);
            return go;
        }

        /// <summary>
        /// 加载场景。
        /// </summary>
        public async UniTask<bool> LoadSceneAsync(string location, LoadSceneMode mode = LoadSceneMode.Additive,
            LocalPhysicsMode physicsMode = LocalPhysicsMode.None)
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
            var handle = await LoadAssetHandleAsync<TextAsset>(location);
            if (handle == null)
            {
                return null;
            }

            var ta = handle.AssetObject as TextAsset;
            byte[] bytes = ta != null ? ta.bytes : null;
            handle.Release();
            return bytes;
        }

        /// <summary>
        /// 常用类型快捷方法：Sprite、Texture2D、AudioClip、TextAsset、Material。
        /// </summary>
        public async UniTask<Sprite> LoadSpriteAsync(string location, CancellationToken token = default)
        {
            var loc = location ?? string.Empty;
            if (loc.StartsWith("http://") || loc.StartsWith("https://"))
            {
                var key = loc.ToLowerInvariant();
                if (_httpSpriteCache.TryGetValue(key, out var cached)) return cached;
                var tex = await LoadTextureFromUrlAsync(loc, token);
                if (token.IsCancellationRequested || tex == null) return null;
                var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                _httpSpriteCache[key] = sprite;
                return sprite;
            }

            return await LoadAssetAsync<Sprite>(location, token: token);
        }

        public async UniTask<Sprite> LoadSpriteAsync(string location, Image image, bool setNative = false,
            CancellationToken token = default)
        {
            if (image == null)
            {
                return null;
            }

            var owner = TryGetHandleOwner(image);
            if (owner != null)
            {
                return await LoadSpriteAsync(location, image, owner, setNative, token);
            }

            var sprite = await LoadSpriteAsync(location, token);
            if (token.IsCancellationRequested) return null;
            if (sprite == null) return null;

            image.sprite = sprite;
            if (setNative)
            {
                image.SetNativeSize();
            }

            return sprite;
        }

        public async UniTask<Sprite> LoadSpriteAsync(string location, Image image, IYooAssetHandleOwner owner,
            bool setNative = false, CancellationToken token = default)
        {
            if (image == null)
            {
                return null;
            }

            var bindingKey = GetBindingKey(image);
            var loc = location ?? string.Empty;
            if (string.IsNullOrEmpty(loc))
            {
                owner?.ReleaseManagedAssetHandle(bindingKey);
                image.sprite = null;
                return null;
            }

            if (loc.StartsWith("http://") || loc.StartsWith("https://"))
            {
                owner?.ReleaseManagedAssetHandle(bindingKey);
                var httpSprite = await LoadSpriteAsync(location, token);
                if (token.IsCancellationRequested || image == null)
                {
                    return null;
                }

                image.sprite = httpSprite;
                if (setNative)
                {
                    image.SetNativeSize();
                }

                return httpSprite;
            }

            if (owner == null)
            {
                return await LoadSpriteAsync(location, image, setNative, token);
            }

            var handle = await LoadAssetHandleAsync<Sprite>(location, token);
            if (handle == null)
            {
                return null;
            }

            if (token.IsCancellationRequested)
            {
                handle.Release();
                return null;
            }

            if (image == null)
            {
                handle.Release();
                return null;
            }

            var sprite = handle.AssetObject as Sprite;
            if (sprite == null)
            {
                handle.Release();
                return null;
            }

            owner.ReplaceManagedAssetHandle(bindingKey, handle);
            image.sprite = sprite;
            if (setNative)
            {
                image.SetNativeSize();
            }

            return sprite;
        }

        public async UniTask<Sprite> LoadSpriteAsync(string atlasName, string location, Image image,
            bool setNative = false, CancellationToken token = default)
        {
            if (image == null)
            {
                return null;
            }

            var owner = TryGetHandleOwner(image);
            if (owner != null)
            {
                return await LoadSpriteAsync(atlasName, location, image, owner, setNative, token);
            }

            var sprite = await GetSpriteFromAtlas(atlasName, location, token);
            if (token.IsCancellationRequested) return null;
            if (sprite == null) return null;

            image.sprite = sprite;
            if (setNative)
            {
                image.SetNativeSize();
            }

            return sprite;
        }

        public async UniTask<Sprite> LoadSpriteAsync(string atlasName, string location, Image image,
            IYooAssetHandleOwner owner, bool setNative = false, CancellationToken token = default)
        {
            if (image == null)
            {
                return null;
            }

            var bindingKey = GetBindingKey(image);
            if (string.IsNullOrEmpty(atlasName) || string.IsNullOrEmpty(location))
            {
                owner?.ReleaseManagedAssetHandle(bindingKey);
                image.sprite = null;
                return null;
            }

            if (owner == null)
            {
                var spriteWithoutOwner = await GetSpriteFromAtlas(atlasName, location, token);
                if (token.IsCancellationRequested || spriteWithoutOwner == null)
                {
                    return null;
                }

                image.sprite = spriteWithoutOwner;
                if (setNative)
                {
                    image.SetNativeSize();
                }

                return spriteWithoutOwner;
            }

            var handle = await LoadAssetHandleAsync<SpriteAtlas>(atlasName, token);
            if (handle == null)
            {
                return null;
            }

            if (token.IsCancellationRequested)
            {
                handle.Release();
                return null;
            }

            var atlas = handle.AssetObject as SpriteAtlas;
            if (atlas == null)
            {
                handle.Release();
                return null;
            }

            var sprite = atlas.GetSprite(location);
            if (sprite == null)
            {
                handle.Release();
                owner.ReleaseManagedAssetHandle(bindingKey);
                image.sprite = null;
                return null;
            }

            owner.ReplaceManagedAssetHandle(bindingKey, handle);
            image.sprite = sprite;
            if (setNative)
            {
                image.SetNativeSize();
            }

            return sprite;
        }

        public async UniTask<Sprite> LoadSpriteAsync(string location, SpriteRenderer spriteRand,
            CancellationToken token = default)
        {
            var sprite = await LoadSpriteAsync(location, token);
            if (token.IsCancellationRequested) return null;
            if (sprite == null) return null;
            if (spriteRand == null) return null;

            spriteRand.sprite = sprite;
            return sprite;
        }

        public async UniTask<Sprite> LoadSpriteAsync(string atlasName, string location, SpriteRenderer spriteRand,
            CancellationToken token = default)
        {
            var sprite = await GetSpriteFromAtlas(atlasName, location, token);
            if (token.IsCancellationRequested) return null;
            if (sprite == null) return null;
            if (spriteRand == null) return null;

            spriteRand.sprite = sprite;
            return sprite;
        }

        public async UniTask<Sprite> GetSpriteFromAtlas(string atlasName, string spriteName,
            CancellationToken token = default)
        {
            var atlas = await GetAtlasAsync(atlasName, token);
            return atlas != null ? atlas.GetSprite(spriteName) : null;
        }

        public UniTask<Texture2D> LoadTextureAsync(string location, CancellationToken token = default)
            => LoadAssetAsync<Texture2D>(location, token: token);

        private async UniTask<Texture2D> LoadTextureFromUrlAsync(string url, CancellationToken token = default)
        {
            var key = (url ?? string.Empty).ToLowerInvariant();
            if (_httpTextureCache.TryGetValue(key, out var cached)) return cached;
            using var req = UnityWebRequestTexture.GetTexture(url, true);
            try
            {
                await req.SendWebRequest();
                if (token.IsCancellationRequested)
                {
                    req.Abort();
                    return null;
                }

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

        public UniTask<AudioClip> LoadAudioAsync(string location, CancellationToken token = default)
            => LoadAssetAsync<AudioClip>(location, token: token);

        public UniTask<TextAsset> LoadTextAsync(string location, CancellationToken token = default)
            => LoadAssetAsync<TextAsset>(location, token: token);

        public UniTask<Material> LoadMaterialAsync(string location, CancellationToken token = default)
            => LoadAssetAsync<Material>(location, token: token);

        public UniTask<SkeletonDataAsset> LoadSkeletonAsync(string location, CancellationToken token = default)
            => LoadAssetAsync<SkeletonDataAsset>(location, token: token);

        public UniTask<GameObject> LoadGameObjectAsync(string location, CancellationToken token = default)
            => LoadAssetAsync<GameObject>(location, token: token);

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

        public Sprite LoadSpriteSync(string location, Image image, IYooAssetHandleOwner owner, bool setNative = false)
        {
            if (image == null)
            {
                return null;
            }

            var bindingKey = GetBindingKey(image);
            if (string.IsNullOrEmpty(location))
            {
                owner?.ReleaseManagedAssetHandle(bindingKey);
                image.sprite = null;
                return null;
            }

            var handle = LoadAssetHandleSync<Sprite>(location);
            if (handle == null)
            {
                return null;
            }

            var sprite = handle.AssetObject as Sprite;
            if (sprite == null)
            {
                handle.Release();
                return null;
            }

            owner?.ReplaceManagedAssetHandle(bindingKey, handle);
            image.sprite = sprite;
            if (setNative)
            {
                image.SetNativeSize();
            }

            return sprite;
        }

        private static string GetBindingKey(Component target)
        {
            return target == null ? string.Empty : target.GetInstanceID().ToString();
        }

        private static IYooAssetHandleOwner TryGetHandleOwner(Component target)
        {
            if (target == null)
            {
                return null;
            }

            Transform current = target.transform;
            while (current != null)
            {
                var behaviours = current.GetComponents<MonoBehaviour>();
                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] is IYooAssetHandleOwner owner)
                    {
                        return owner;
                    }
                }

                current = current.parent;
            }

            return null;
        }

        private async UniTask<SpriteAtlas> GetAtlasAsync(string atlasName, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(atlasName))
            {
                return null;
            }

            if (_atlasHandleCache.TryGetValue(atlasName, out var cachedHandle) && cachedHandle != null)
            {
                return cachedHandle.AssetObject as SpriteAtlas;
            }

            var handle = await LoadAssetHandleAsync<SpriteAtlas>(atlasName, token);
            if (handle == null)
            {
                return null;
            }

            _atlasHandleCache[atlasName] = handle;
            return handle.AssetObject as SpriteAtlas;
        }
    }
}
