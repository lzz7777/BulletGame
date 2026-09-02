using System.Threading;

namespace XN
{
    public static class ComponentTypeManager
    {
        private static long _nextId = -1; // 初始值为 -1

        // 提供一个线程安全的递增属性
        public static long NextId => Interlocked.Increment(ref _nextId);
    }

    public static class ComponentTypeId<T> where T : ComponentBase
    {
        public static readonly long Id = ComponentTypeManager.NextId; // 静态构造只执行一次
    }
}
