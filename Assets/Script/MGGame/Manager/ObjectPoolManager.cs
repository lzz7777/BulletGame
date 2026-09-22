using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace XN
{
    public enum PrefabType
    {
        None = 0,
        Effect,
    }

    public class PoolData
    {
        public Queue<GameObject> GoQueue = new();
        public int Count;
        public PrefabType PrefabType;
        public AssetHandle PrefabHandle;
        public GameObject Prefab;
    }

    public class ObjectPoolManager : MonoSingleton<ObjectPoolManager>
    {
        private Dictionary<string, PoolData> _poolDictionary; // 对象池字典

        // 新增：记录 InstanceID 到 Tag 的映射，用于 Recycle 时消除 string.Replace 的 GC
        private Dictionary<int, string> _instanceIdToTag;
        private GameObject _poolRoot;
        public bool IsInitialized { get; private set; }

        //特效定制预制体数量限制
        public Dictionary<string, int> goMaxNumDic = new();

        //特效通用预制体数量限制
        public int commonGoMaxNum = 0;

        protected override async void OnInit()
        {
            await UniTask.WaitUntil(() => YooAssetManager.Instance.IsInitialized);
            await UniTask.WaitUntil(() => TotalConfigManager.Instance.IsLoadOver);

            // 预分配字典容量，减少扩容 GC
            _poolDictionary = new(512);
            _instanceIdToTag = new(512);

            _poolRoot = new GameObject("PoolRoot");
            DontDestroyOnLoad(_poolRoot);
            _poolRoot.SetActive(false);

            IsInitialized = true;
        }

        protected override void OnRemove()
        {
            if (_poolDictionary != null)
            {
                foreach (var pair in _poolDictionary)
                {
                    pair.Value?.PrefabHandle?.Release();
                }
            }
        }

        public UniTask<List<GameObject>> GetFromPool<T>(int num, Transform parentRoot, bool setZero = true)
        {
            List<GameObject> gos = new(num); // 预分配 List 容量消除扩容 GC

            string tag = typeof(T).Name;
            for (int i = 0; i < num; i++)
            {
                var go = GetFromPoolSync(tag, parentRoot);

                if (setZero)
                {
                    go.transform.localScale = Vector3.zero;
                }

                if (go != null)
                {
                    gos.Add(go);
                }
            }

            return UniTask.FromResult(gos);
        }

        public UniTask<GameObject> GetFromPool<T>(Transform parentRoot) =>
            UniTask.FromResult(GetFromPoolSync(typeof(T).Name, parentRoot));

        public async UniTask AdvanceAddRes<T>(int num, PrefabType prefabType = PrefabType.None) =>
            AdvanceAddRes(typeof(T).Name, num, prefabType);

        /// <summary>
        /// 预加载接口
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="num"></param>
        /// <param name="prefabType"></param>
        /// <param name="cb">预加载回调</param>
        public async UniTask AdvanceAddRes(string tag, int num, PrefabType prefabType = PrefabType.None,
            Action<GameObject> cb = null, Transform parentRoot = null)
        {
            var poolData = GetOrCreatePoolData(tag, prefabType);
            if (!await EnsurePrefabLoadedAsync(tag, poolData))
            {
                return;
            }

            for (int i = 0; i < num; i++)
            {
                parentRoot ??= _poolRoot.transform;
                var obj = InstantiateFromPoolData(poolData, parentRoot);

                if (!obj)
                {
                    return;
                }

                if (cb != null)
                {
                    cb(obj);
                }

                obj.transform.localScale = Vector3.zero;
                // obj.SetActive(false);

                _instanceIdToTag[obj.GetInstanceID()] = tag; // 记录映射，消除 Return 时的 GC

                poolData.GoQueue.Enqueue(obj);
                poolData.Count++;
            }
        }

        // 兼容原有的异步获取接口，底层转为完全无 GC 的同步调用
        public UniTask<GameObject> GetFromPool(string tag, Transform parentRoot,
            PrefabType prefabType = PrefabType.None)
        {
            return UniTask.FromResult(GetFromPoolSync(tag, parentRoot, prefabType));
        }

        public GameObject GetFromPoolSync<T>(Transform parentRoot) => GetFromPoolSync(typeof(T).Name, parentRoot);

        /// <summary>
        /// 核心出池逻辑，提取公共代码以复用
        /// </summary>
        private GameObject InternalDequeue(string tag, Transform parentRoot, PoolData poolData)
        {
            GameObject objectToSpawn = poolData.GoQueue.Dequeue();

            if (parentRoot)
                objectToSpawn.transform.SetParent(parentRoot);

            objectToSpawn.transform.localPosition = Vector3.zero;
            objectToSpawn.transform.localScale = Vector3.one;

            return objectToSpawn;
        }

        /// <summary>
        /// 同步获取（仅当预热过，或者不需要等待 YooAsset 加载时使用）
        /// 彻底消除 UniTask 状态机和 Awaiter 产生的 GC
        /// </summary>
        public GameObject GetFromPoolSync(string tag, Transform parentRoot, PrefabType prefabType = PrefabType.None)
        {
            var poolData = GetOrCreatePoolData(tag, prefabType);

            if (poolData.GoQueue.Count == 0)
            {
                if (!CanCreateNewInstance(tag, poolData))
                    return null;

                if (!parentRoot) Debug.LogError("parentRoot is null");

                if (!EnsurePrefabLoadedSync(tag, poolData))
                    return null;

                var newObj = InstantiateFromPoolData(poolData, parentRoot);
                if (!newObj)
                    return null;

                newObj.transform.localScale = Vector3.zero;

                _instanceIdToTag[newObj.GetInstanceID()] = tag;

                poolData.GoQueue.Enqueue(newObj);
                poolData.Count++;
            }

            return InternalDequeue(tag, parentRoot, poolData);
        }

        /// <summary>
        /// 异步获取（如果没有预热，将等待 YooAsset 异步加载完成）
        /// 解决未预热直接滑动导致的 InstantiateSync 卡顿问题
        /// </summary>
        public async UniTask<GameObject> GetFromPoolAsync(string tag, Transform parentRoot, PrefabType prefabType = PrefabType.None)
        {
            var poolData = GetOrCreatePoolData(tag, prefabType);

            if (poolData.GoQueue.Count == 0)
            {
                if (!CanCreateNewInstance(tag, poolData))
                    return null;

                if (!parentRoot) Debug.LogError("parentRoot is null");

                if (!await EnsurePrefabLoadedAsync(tag, poolData))
                    return null;

                var newObj = InstantiateFromPoolData(poolData, parentRoot);
                if (!newObj) return null;

                newObj.transform.localScale = Vector3.zero;
                _instanceIdToTag[newObj.GetInstanceID()] = tag;

                poolData.GoQueue.Enqueue(newObj);
                poolData.Count++;
            }

            return InternalDequeue(tag, parentRoot, poolData);
        }

        public void ReturnToPool(List<GameObject> gos)
        {
            // 防止 null 和使用 for 循环减少 foreach 迭代器可能存在的隐性 GC
            if (gos == null) return;
            for (int i = 0; i < gos.Count; i++)
            {
                ReturnToPool(gos[i]);
            }
        }

        /// <summary>
        /// 将对象放回对象池
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="obj"></param>
        public void ReturnToPool(GameObject obj, bool noSetParent = false)
        {
            if (!obj)
            {
                return;
            }

            int instanceId = obj.GetInstanceID();
            string key;

            // 1. 优先使用 InstanceID 获取 Tag，实现 0 GC 字符串匹配
            if (_instanceIdToTag.TryGetValue(instanceId, out var cachedTag))
            {
                key = cachedTag;
            }
            else
            {
                // 2. 兜底方案（比如场景里手摆的非池化预制体）
                key = obj.name.Replace("(Clone)", "");
            }

            if (!_poolDictionary.TryGetValue(key, out var poolData))
            {
                Debug.LogError("对象池标签不存在: " + key);
                return;
            }

            obj.transform.localScale = Vector3.zero;
            // obj.SetActive(false); // 禁用对象

            if (!noSetParent)
                obj.transform.SetParent(_poolRoot.transform);

            poolData.GoQueue.Enqueue(obj); // 放回队列
        }

        private PoolData GetOrCreatePoolData(string tag, PrefabType prefabType)
        {
            if (_poolDictionary.TryGetValue(tag, out var poolData))
            {
                if (poolData.PrefabType == PrefabType.None && prefabType != PrefabType.None)
                {
                    poolData.PrefabType = prefabType;
                }
                return poolData;
            }

            poolData = new PoolData { PrefabType = prefabType };
            _poolDictionary.Add(tag, poolData);
            return poolData;
        }

        private bool CanCreateNewInstance(string tag, PoolData poolData)
        {
            goMaxNumDic.TryGetValue(tag, out var goMaxNum);
            if (goMaxNum == -1) return false;
            if (goMaxNum != 0 && poolData.Count >= goMaxNum) return false;

            if (poolData.PrefabType == PrefabType.Effect)
            {
                if (commonGoMaxNum == -1) return false;
                if (commonGoMaxNum != 0 && poolData.Count >= commonGoMaxNum) return false;
            }

            return true;
        }

        private async UniTask<bool> EnsurePrefabLoadedAsync(string tag, PoolData poolData)
        {
            if (poolData.Prefab != null && poolData.PrefabHandle != null)
            {
                return true;
            }

            var handle = await YooAssetManager.Instance.LoadAssetHandleAsync<GameObject>(tag);
            if (handle == null)
            {
                return false;
            }

            poolData.PrefabHandle?.Release();
            poolData.PrefabHandle = handle;
            poolData.Prefab = handle.AssetObject as GameObject;
            if (poolData.Prefab == null)
            {
                poolData.PrefabHandle.Release();
                poolData.PrefabHandle = null;
                return false;
            }
            return poolData.Prefab != null;
        }

        private bool EnsurePrefabLoadedSync(string tag, PoolData poolData)
        {
            if (poolData.Prefab != null && poolData.PrefabHandle != null)
            {
                return true;
            }

            var handle = YooAssetManager.Instance.LoadAssetHandleSync<GameObject>(tag);
            if (handle == null)
            {
                return false;
            }

            poolData.PrefabHandle?.Release();
            poolData.PrefabHandle = handle;
            poolData.Prefab = handle.AssetObject as GameObject;
            if (poolData.Prefab == null)
            {
                poolData.PrefabHandle.Release();
                poolData.PrefabHandle = null;
                return false;
            }
            return poolData.Prefab != null;
        }

        private static GameObject InstantiateFromPoolData(PoolData poolData, Transform parentRoot)
        {
            if (poolData?.Prefab == null)
            {
                return null;
            }

            return parentRoot != null
                ? UnityEngine.Object.Instantiate(poolData.Prefab, parentRoot, false)
                : UnityEngine.Object.Instantiate(poolData.Prefab);
        }
    }
}
