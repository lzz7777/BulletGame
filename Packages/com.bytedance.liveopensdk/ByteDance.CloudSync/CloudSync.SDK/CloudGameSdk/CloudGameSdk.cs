namespace ByteDance.CloudSync
{
    internal class CloudGameSdk
    {
        /// 是否全部调试log开启，对于Input输入
        public static bool IsVerboseLogForInput;

        /// <summary>
        /// 封装的Sdk API入口
        /// </summary>
        public static ICloudGameAPI API
        {
            get
            {
                if (_mockApi != null)
                    return _mockApi;
                if (SdkConsts.IsAndroidPlayer)
                    return CloudGameAPIAndroid.Instance;
                return CloudGameAPIWindows.Instance;
            }
        }

        private static ICloudGameAPI _mockApi;

        public static void SetupMockApi(ICloudGameAPI mockApi)
        {
            CGLogger.Log($"CloudGameSdk SetMock: {mockApi != null} {mockApi}");
            _mockApi = mockApi;
        }
    }
}