using Aliyun.Editor;
using YooAsset;

namespace XN
{
    public enum VersionType
    {
        AssetVersion,
        ConfigVersion,
        CodeVersion,
    }

    public enum PackageType
    {
        Asset,
        Config,
        Code,
    }

    /// <summary>
    /// 远端资源地址查询服务类
    /// 实现IRemoteServices接口，用于YooAsset获取资源的下载地址
    /// </summary>
    public class RemoteServices : IRemoteServices
    {
        private readonly string _packageVersion;
        private PackageType _packageType;

        public RemoteServices(string packageVersion, PackageType packageType)
        {
            _packageVersion = packageVersion;
            _packageType = packageType;
        }

        // 获取主服务器的资源下载地址
        string IRemoteServices.GetRemoteMainURL(string fileName)
        {
            // 1. 拼出 OSS 上的文件相对路径 (ObjectKey)
            // 例如: Asset/2026-03-25-962/DefaultPackage.version
            string objectKey = $"{AliyunConfig.Path}/Asset/{_packageType}/{_packageVersion}/{fileName}";

            // 2. 使用 OssClient 生成签名 URL
            return OssUrlHelper.GetSignedUrl(objectKey);
        }

        // 获取备用服务器的资源下载地址
        string IRemoteServices.GetRemoteFallbackURL(string fileName)
        {
            return ((IRemoteServices)this).GetRemoteMainURL(fileName);
        }
    }
}