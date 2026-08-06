// Copyright@www.bytedance.com
// Author: Admin
// Date: 2024/06/07
// Description:

using System.Collections.Generic;
using System.Linq;

namespace ByteDance.CloudSync
{
    public interface IVirtualDeviceSystem
    {
        IVirtualDevice CreateDevice(SeatIndex index, IVirtualDeviceFactory creator, IRenderSettings settings);

        void EnableInputDebug();
    }

    public class VirtualDeviceSystem : IVirtualDeviceSystem
    {
        private static VirtualDeviceSystem _instance;

        public static int CurrentOperateFrame; // 当前 operate 消息的帧号，debug 用

        public static IVirtualDeviceSystem Instance => _instance ??= new VirtualDeviceSystem();

        private readonly Dictionary<SeatIndex, VirtualDevice> _deviceMap = new();

        public static IVirtualDevice Find(SeatIndex index)
        {
            return _instance?._deviceMap.GetValueOrDefault(index);
        }

        public IVirtualDevice CreateDevice(SeatIndex index, IVirtualDeviceFactory creator, IRenderSettings settings)
        {
            var device = new VirtualDevice(index, creator, settings);
            _deviceMap.Add(index, device);
            VirtualScreenSystem.Instance.Create(index, device.Screen as VirtualScreen);
            return device;
        }

        internal static void Destroy(IVirtualDevice device)
        {
            _instance?._deviceMap.Remove(device.Index);
            ((VirtualDevice)device).Dispose();
        }

        /// <summary>
        /// 销毁所有设备，在 CloudSyncSdk 退出时调用
        /// </summary>
        internal static void DestroyAll()
        {
            if (_instance != null)
            {
                var all = _instance._deviceMap.Values.ToArray();
                foreach (var device in all)
                {
                    Destroy(device);
                }

                _instance = null;
            }
        }

        public void EnableInputDebug()
        {
            foreach (var device in _deviceMap.Values)
            {
                device.Input.EnableDebugger();
            }
        }
    }
}