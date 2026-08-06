// Copyright@www.bytedance.com
// Author: zhouxu.ken
// Date: 2024/06/08
// Description:

using System.Threading.Tasks;
using ByteDance.CloudSync.Mock;

namespace ByteDance.CloudSync
{
    /// <summary>
    /// Mock类型。 用于非真实环境的模拟调试。
    /// </summary>
    public enum MockType
    {
        /// 全面的 Mock 调试方案。可以在本地或局域网中模拟调试多个用户的画面、输入、和匹配同玩流程，有接近真实环境的RTC推流。
        Full,

        /// 简单的 Mock 调试方案。可以在本地Editor中快速预览多个用户的画面。
        Simple,

        /// 模拟本地启动单人模式，不使用云同步、不使用云启动
        LocalSingle,

        /// 不模拟 Mock。按真实运行环境。
        None,
    }

    public static class MockCloudSyncExtensions
    {
        /// <summary>
        /// 设置Mock方式。
        /// 注：如果是真实环境，不会生效。
        /// </summary>
        public static void SetMockType(this ICloudSync _, MockType mockType)
        {
            if (ICloudSync.Env.IsRealEnv())
            {
                CGLogger.Log("真实线上环境，不设置Mock");
                return;
            }

            MockCloudSync.Instance.Setup(mockType);
        }

        /// <summary>
        /// 获取当前Mock方式。
        /// </summary>
        public static MockType GetMockType(this ICloudSync _)
        {
            return MockCloudSync.Instance.MockType;
        }


        /// <summary>
        /// 是否在Mock模拟环境。（不在真实线上环境）
        /// </summary>
        public static bool IsMockEnv(this ICloudSyncEnv self)
        {
            return !self.IsRealEnv();
        }

        /// <summary>
        /// 获取Mock接口
        /// </summary>
        public static IMockCloudSync GetMockInterface(this ICloudSyncEnv self) => MockCloudSync.Instance;
    }

    /// <summary>
    /// Mock云同步系统
    /// </summary>
    public interface IMockCloudSync
    {
        MockType MockType { get; }

        void Setup(MockType mockType);
    }

    internal class MockCloudSync : IMockCloudSync
    {
        private static readonly SdkDebugLogger Debug = new("MockCloudGameSystem");

        private MockType _mockType;
        internal static readonly MockCloudSync Instance = new MockCloudSync();

        public MockType MockType => _mockType;

        public void Setup(MockType mockType = MockType.Simple)
        {
            Debug.Log($"Setup MockType: {mockType}");
            _mockType = mockType;
            switch (mockType)
            {
                case MockType.Full:
                    FullMock.Setup();
                    break;
                case MockType.Simple:
                    SimpleMock.Setup();
                    break;
                case MockType.None:
                    break;
                case MockType.LocalSingle:
                    break;
            }
        }

        /// <summary>
        /// 请不要主动调用此接口，直接打开 Screen - 2 可自动 MockJoin
        /// </summary>
        internal async Task MockJoin(SeatIndex index)
        {
            if (_mockType == MockType.Full)
            {
                Debug.LogError("Current mock 'MockType.Full' not support MockJoin");
                return;
            }

            await SimpleMock.MockJoin(index);
        }

        internal void MockExit(SeatIndex index)
        {
            if (_mockType == MockType.Full)
            {
                Debug.LogError("Current mock 'MockType.Full' not support MockExit");
                return;
            }

            SimpleMock.MockExit(index);
        }
    }
}