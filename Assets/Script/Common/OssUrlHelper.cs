using System;
using Aliyun.Editor;
using Aliyun.OSS;

namespace XN
{
    public static class OssUrlHelper
    {
        /// <summary>
        /// 获取资源服务器地址
        /// </summary>
        /// <returns>服务器URL</returns>
        public static string GetSignedUrl(string objectKey, int expireMinutes = 60)
        {
            // 1. 实例化 OssClient
            OssClient client = new OssClient(AliyunConfig.Endpoint, AliyunConfig.AccessKeyId,
                AliyunConfig.AccessKeySecret);

            // 2. 计算过期时间 (必须使用 UtcNow，避免本地时区差异导致签名过期)
            DateTime expiration = DateTime.UtcNow.AddMinutes(expireMinutes);

            // 3. 生成预签名 URI (默认是 GET 请求)
            // 注意：生成 URI 时传递的 ObjectKey 不能以 '/' 开头
            var request =
                new GeneratePresignedUriRequest(AliyunConfig.Bucket_HotFixeBundle, objectKey, SignHttpMethod.Get);
            request.Expiration = expiration;
            var uri = client.GeneratePresignedUri(request);

            // 4. 返回生成的完整字符串 URL (必须使用 AbsoluteUri 防止二次转义破坏签名)
            return uri.AbsoluteUri;
        }
    }
}