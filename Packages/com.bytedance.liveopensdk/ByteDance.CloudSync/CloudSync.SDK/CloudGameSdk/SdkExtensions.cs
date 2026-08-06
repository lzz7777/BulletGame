// Copyright@www.bytedance.com
// Author: DONEY Dong
// Date: 2024/05/16
// Description:

namespace ByteDance.CloudSync
{
    internal static class SdkExtensions
    {
        public static bool IsSuccessOrAlready(this ICloudGameAPI.ErrorCode code)
        {
            // ReSharper disable once MergeIntoLogicalPattern
            return code == ICloudGameAPI.ErrorCode.Success || code == ICloudGameAPI.ErrorCode.Success_AlreadyInited;
        }

        public static ByteCloudGameSdk.ByteCloudGameSdkErrorCode ToSdkCode(this ICloudGameAPI.ErrorCode code)
        {
            return (ByteCloudGameSdk.ByteCloudGameSdkErrorCode)code;
        }

        public static ICloudGameAPI.ErrorCode ConvertSdkCode(ByteCloudGameSdk.ByteCloudGameSdkErrorCode sdkErrorCode)
        {
            return (ICloudGameAPI.ErrorCode)sdkErrorCode;
        }
    }

    internal static class SdkErrorCodeExtension
    {
        public static ICloudGameAPI.ErrorCode ToApiCode(this ByteCloudGameSdk.ByteCloudGameSdkErrorCode sdkErrorCode)
        {
            return SdkExtensions.ConvertSdkCode(sdkErrorCode);
        }
    }

    internal static class ByteCloudGameSdkResponseExtension
    {
        public static ICloudGameAPI.Response ToApiResponse(this ByteCloudGameSdk.ByteCloudGameSdkResponse response)
        {
            return new ICloudGameAPI.Response(response.code.ToApiCode(), response.message);
        }
    }

    internal static class PodMessageExtension
    {
        public static ByteCloudGameSdk.PodMessage ToSdkPodMessage(this ApiPodMessageData messageData)
        {
            return new ByteCloudGameSdk.PodMessage
            {
                from = messageData.from,
                message = messageData.message
            };
        }
    }
}