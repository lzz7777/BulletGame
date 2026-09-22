using YooAsset;

namespace XN
{
    /// <summary>
    /// UI 资源句柄持有者。
    /// 由视图对象负责管理自身绑定的 YooAsset 句柄生命周期。
    /// </summary>
    public interface IYooAssetHandleOwner
    {
        void ReplaceManagedAssetHandle(string key, AssetHandle handle);
        void ReleaseManagedAssetHandle(string key);
        void ReleaseAllManagedAssetHandles();
    }
}
