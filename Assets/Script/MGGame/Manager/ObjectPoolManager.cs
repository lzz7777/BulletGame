using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

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
            _poolDictionary = new();

            _poolRoot = new GameObject("PoolRoot");
            DontDestroyOnLoad(_poolRoot);
            _poolRoot.SetActive(false);
            
            IsInitialized = true;
        }

        protected override void OnRemove()
        {
        }

        public async UniTask<List<GameObject>> GetFromPool(string tag, int num, Transform parentRoot)
        {
            List<GameObject> gos = new();

            for (int i = 0; i < num; i++)
            {
                var go = await GetFromPool(tag, parentRoot);
                gos.Add(go);
            }

            return gos;
        }

        public async UniTask<GameObject> GetFromPool<T>(Transform parentRoot) => await GetFromPool(typeof(T).Name, parentRoot);

        /// <summary>
        /// 预加载接口
        /// </summary>
        /// <param name="tag"></param>
        public async UniTask AdvanceAddRes(string tag, int num, PrefabType prefabType = PrefabType.None)
        {
            return;
            _poolDictionary.TryAdd(tag, new()
            {
                PrefabType = prefabType
            });

            for (int i = 0; i < num; i++)
            {
                var obj = await YooAssetManager.Instance.InstantiateAsync(tag, _poolRoot.transform);

                if (!obj)
                {
                    return;
                }
                
                obj.transform.localScale = Vector3.zero;
                // obj.SetActive(false);

                var poolData = _poolDictionary[tag];
                poolData.GoQueue.Enqueue(obj);
                poolData.Count++;
            }
        }

        // 从对象池中获取对象
        public async UniTask<GameObject> GetFromPool(string tag, Transform parentRoot, PrefabType prefabType = PrefabType.None)
        {
            if (parentRoot == null)
            {
                Debug.LogError(tag + " : Parent root is null");
            }

            _poolDictionary.TryAdd(tag, new()
            {
                PrefabType = prefabType
            });

            var poolData = _poolDictionary[tag];

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
                
                // var newObj = await YooAssetManager.Instance.InstantiateAsync(tag, parentRoot);
                var newObj = YooAssetManager.Instance.InstantiateSync(tag, parentRoot);
                newObj.transform.localScale = Vector3.zero;
                // newObj.SetActive(false);
                
                poolData.GoQueue.Enqueue(newObj);
                poolData.Count++;
            }

            GameObject objectToSpawn = poolData.GoQueue.Dequeue();
            // objectToSpawn.SetActive(true); // 激活对象
            objectToSpawn.transform.SetParent(parentRoot);
            objectToSpawn.transform.localPosition = Vector3.zero;
            objectToSpawn.transform.localScale = Vector3.one;
            
            await UniTask.CompletedTask;
            return objectToSpawn;
        }

        public void ReturnToPool(List<GameObject> gos)
        {
            foreach (var go in gos)
            {
                ReturnToPool(go);
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
            
            string key = obj.name.Replace("(Clone)", "");
            if (!_poolDictionary.ContainsKey(key))
            {
                Debug.LogError("对象池标签不存在: " + key);
                return;
            }

            obj.transform.localScale = Vector3.zero;
            // obj.SetActive(false); // 禁用对象
            obj.transform.SetParent(_poolRoot.transform);
            _poolDictionary[key].GoQueue.Enqueue(obj); // 放回队列
        }
    }
}