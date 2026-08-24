using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

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
        public async UniTask AdvanceAddRes(string tag, int num, PrefabType prefabType = PrefabType.None)
        {
            // 修复 TryAdd 传 new PoolData() 导致的每帧 GC Alloc
            if (!_poolDictionary.TryGetValue(tag, out var poolData))
            {
                poolData = new PoolData { PrefabType = prefabType };
                _poolDictionary.Add(tag, poolData);
            }

            for (int i = 0; i < num; i++)
            {
                var obj = await YooAssetManager.Instance.InstantiateAsync(tag, _poolRoot.transform);

                if (!obj)
                {
                    return;
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

        // 新增的完全同步获取接口，彻底消除 UniTask 状态机和 Awaiter 产生的 GC
        public GameObject GetFromPoolSync(string tag, Transform parentRoot, PrefabType prefabType = PrefabType.None)
        {
            if (parentRoot == null)
            {
                Debug.LogError(tag + " : Parent root is null");
            }

            // 修复 TryAdd(tag, new PoolData()) 导致的严重 GC Alloc 泄漏
            if (!_poolDictionary.TryGetValue(tag, out var poolData))
            {
                poolData = new PoolData { PrefabType = prefabType };
                _poolDictionary.Add(tag, poolData);
            }

            // 如果对象池为空，可以动态扩展（这里简单处理：重新创建一个对象）
            if (poolData.GoQueue.Count == 0)
            {
                goMaxNumDic.TryGetValue(tag, out var goMaxNum);
                if (goMaxNum == -1)
                    return null;

                if (goMaxNum != 0)
                {
                    //个别限制
                    if (poolData.Count > goMaxNum)
                    {
                        // Debug.LogError(tag + " : num is max");
                        return null;
                    }
                }

                if (poolData.PrefabType == PrefabType.Effect)
                {
                    if (commonGoMaxNum == -1)
                        return null;

                    if (commonGoMaxNum != 0)
                    {
                        //通用限制
                        if (poolData.Count > commonGoMaxNum)
                        {
                            // Debug.LogError(tag + " : num is max");
                            return null;
                        }
                    }
                }

                // 完全同步实例化
                var newObj = YooAssetManager.Instance.InstantiateSync(tag, parentRoot);
                newObj.transform.localScale = Vector3.zero;
                // newObj.SetActive(false);

                _instanceIdToTag[newObj.GetInstanceID()] = tag; // 记录映射，消除 Return 时的 GC

                poolData.GoQueue.Enqueue(newObj);
                poolData.Count++;
            }

            GameObject objectToSpawn = poolData.GoQueue.Dequeue();
            // objectToSpawn.SetActive(true); // 激活对象
            objectToSpawn.transform.SetParent(parentRoot);
            objectToSpawn.transform.localPosition = Vector3.zero;
            objectToSpawn.transform.localScale = Vector3.one;

            return objectToSpawn;
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
        public void ReturnToPool(GameObject obj)
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
            obj.transform.SetParent(_poolRoot.transform);
            poolData.GoQueue.Enqueue(obj); // 放回队列
        }
    }
}