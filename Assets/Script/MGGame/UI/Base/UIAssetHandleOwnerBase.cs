using System.Collections.Generic;
using UnityEngine;
using YooAsset;

namespace XN
{
    /// <summary>
    /// UI 侧共享的 YooAsset 句柄持有基类。
    /// 负责替换、释放以及对象销毁时的统一回收。
    /// </summary>
    public abstract class UIAssetHandleOwnerBase : MonoBehaviour, IYooAssetHandleOwner
    {
        private Dictionary<string, AssetHandle> _managedAssetHandles;

        public void ReplaceManagedAssetHandle(string key, AssetHandle handle)
        {
            if (string.IsNullOrEmpty(key))
            {
                handle?.Release();
                return;
            }

            _managedAssetHandles ??= new Dictionary<string, AssetHandle>(4);
            if (_managedAssetHandles.TryGetValue(key, out var oldHandle) && oldHandle != null)
            {
                oldHandle.Release();
            }

            if (handle == null)
            {
                _managedAssetHandles.Remove(key);
                return;
            }

            _managedAssetHandles[key] = handle;
        }

        public void ReleaseManagedAssetHandle(string key)
        {
            if (_managedAssetHandles == null || string.IsNullOrEmpty(key))
            {
                return;
            }

            if (_managedAssetHandles.TryGetValue(key, out var handle) && handle != null)
            {
                handle.Release();
            }

            _managedAssetHandles.Remove(key);
        }

        public void ReleaseAllManagedAssetHandles()
        {
            if (_managedAssetHandles == null)
            {
                return;
            }

            foreach (var pair in _managedAssetHandles)
            {
                pair.Value?.Release();
            }

            _managedAssetHandles.Clear();
        }

        protected virtual void OnDestroy()
        {
            ReleaseAllManagedAssetHandles();
        }
    }
}
