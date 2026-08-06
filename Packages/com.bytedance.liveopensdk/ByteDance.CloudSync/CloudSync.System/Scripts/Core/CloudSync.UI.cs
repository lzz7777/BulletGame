// Copyright@www.bytedance.com
// Author: zhouxu.ken
// Date: 2024/06/11
// Description:

namespace ByteDance.CloudSync
{
    /// <summary>
    /// 处理报错
    /// </summary>
    public interface ICloudGameErrorHandler
    {
        void HandleError(CloudGameError error);
    }

    /// <summary>
    /// 报错信息
    /// </summary>
    public struct CloudGameError
    {
        public bool Fatal;
        public int Code;
        public string Message;
        public string CustomTitle;
        public ShowsButtonLimiter ShowsButton;

        // 是否显示按钮的限制器。
        // @param seat 席位号
        // @returns bool 是否显示
        public delegate bool ShowsButtonLimiter(SeatIndex seat);
    }

    internal partial class CloudSyncSdk
    {
        /// <summary>
        /// 错误处理接口，默认只打 Error LOG
        /// </summary>
        private static ICloudGameErrorHandler _errorHandler = new DefaultErrorHandler();

        private static string _displayVersion;

        /// <summary>
        /// 设置自定义错误处理
        /// </summary>
        public static void SetErrorHandler(ICloudGameErrorHandler errorHandler)
        {
            _errorHandler = errorHandler;
        }

        public static void NotifyError(CloudGameError error)
        {
            _errorHandler.HandleError(error);
        }

        public static void NotifyFatalError(string msg, int code = -1, bool fatal = true, string customTitle = null)
        {
            _errorHandler.HandleError(new CloudGameError
            {
                Message = msg,
                Code = code,
                Fatal = fatal,
                CustomTitle = customTitle,
                ShowsButton = fatal ? FatalErrorShowsButton : NormalErrorShowsButton
            });
        }

        private static bool FatalErrorShowsButton(SeatIndex seat)
        {
            return seat.IsValid();
        }

        private static bool NormalErrorShowsButton(SeatIndex seat)
        {
            return true;
        }

        /// <summary>
        /// 空实现，目前需要从外部提供能力，后续可以实现一个默认
        /// </summary>
        private class DefaultErrorHandler : ICloudGameErrorHandler
        {
            public void HandleError(CloudGameError error)
            {
                CGLogger.LogError(error.Message);
            }
        }

        public string GetDisplayVersion()
        {
            return _displayVersion;
        }

        public void SetDisplayVersion(string version)
        {
            CGLogger.Log($"SetDisplayVersion {version}");
            _displayVersion = version;
        }
    }
}