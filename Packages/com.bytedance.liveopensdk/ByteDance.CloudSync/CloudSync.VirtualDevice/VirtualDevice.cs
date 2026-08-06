// Copyright@www.bytedance.com
// Author: zhouxu.ken
// Date: 2024/06/07
// Description:

using System;

namespace ByteDance.CloudSync
{
    /// <summary>
    /// 云游戏远程设备（抽象概念）
    /// </summary>
    public interface IVirtualDevice
    {
        SeatIndex Index { get; }

        /// <summary>
        /// 设备推流画面
        /// </summary>
        IVirtualScreen Screen { get; }

        /// <summary>
        /// 设备用户输入
        /// </summary>
        IVirtualDeviceInput Input { get; }

        void Init();
    }

    public struct VirtualDeviceParts
    {
        public IVirtualDeviceInput Input;
        public IVirtualScreen Screen;
    }

    /// <summary>
    /// 虚拟设备工厂，创建 DeviceInput & Screen
    /// </summary>
    public interface IVirtualDeviceFactory
    {
        VirtualDeviceParts Create(IVirtualDevice device, IRenderSettings settings);
    }

    public class DeviceCreateErrorException : Exception
    {
        public DeviceCreateErrorException(string s) : base(s)
        {
        }
    }

    public class DeviceCreateViewErrorException : DeviceCreateErrorException
    {
        public DeviceCreateViewErrorException(SeatIndex deviceIndex, string viewProvider) : base($"Create view error! index: {deviceIndex}, viewProvider: {viewProvider}")
        {
        }
    }

    internal class VirtualDevice : IVirtualDevice, IDisposable
    {
        private readonly IVirtualDeviceInput _input;
        private readonly IVirtualScreen _screen;
        private bool _enable;

        /// <summary>
        /// 设备对应的座位号 Index
        /// </summary>
        public SeatIndex Index { get; }

        /// <summary>
        /// 设备推流画面
        /// </summary>
        public IVirtualScreen Screen => _screen;

        /// <summary>
        /// 设备用户输入
        /// </summary>
        public IVirtualDeviceInput Input => _input;

        public VirtualDevice(SeatIndex index, IVirtualDeviceFactory factory, IRenderSettings settings)
        {
            Index = index;
            var parts = factory.Create(this, settings);
            _input = parts.Input;
            _screen = parts.Screen;
        }

        public void Init()
        {
            _screen.Init();
        }

        public void Dispose()
        {
            VirtualScreenSystem.Destroy(_screen);
            _input.Close();
        }
    }
}