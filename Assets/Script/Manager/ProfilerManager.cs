using Unity.Profiling;

namespace Script.Manager
{
    public class ProfilerManager
    {
        public static readonly ProfilerMarker pm = new("创建飞机");
        public static readonly ProfilerMarker shieldDataSystem = new("删除盾牌系统");
    }
}