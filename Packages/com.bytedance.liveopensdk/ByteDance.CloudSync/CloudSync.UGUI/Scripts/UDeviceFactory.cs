// Copyright@www.bytedance.com
// Author: zhouxu.ken
// Date: 2024/06/11
// Description:

namespace ByteDance.CloudSync.UGUI
{
    /// <summary>
    /// UGUI 系统下默认的 Device 部件工厂类，用于创建虚拟远程设备
    /// </summary>
    public class UDeviceFactory : IVirtualDeviceFactory
    {
        private readonly ICloudViewProvider<UCloudView> _viewProvider;
        private readonly IUVirtualScreenProvider _customScreenProvider;
        private readonly IUVirtualScreenProvider _defaultScreenProvider = new DefaultUVirtualScreenProvider();

        /// <param name="viewProvider">各主播画面视图的 View Provider。 可参考并使用 Demo 的 CloudViewProvider</param>
        /// <param name="customScreenProvider">可选，自定义的 Screen Provider。 默认 null: 会使用系统默认的 ScreenProvider</param>
        public UDeviceFactory(ICloudViewProvider<UCloudView> viewProvider, IUVirtualScreenProvider customScreenProvider = null)
        {
            _viewProvider = viewProvider;
            _customScreenProvider = customScreenProvider;
        }

        public VirtualDeviceParts Create(IVirtualDevice device, IRenderSettings settings)
        {
            var view = _viewProvider.CreateView(device.Index);
            if (view == null)
                throw new DeviceCreateViewErrorException(device.Index, $"{_viewProvider}");
            view.Index = device.Index;
            var input = new UVirtualDeviceInput();
            input.Init(device, view);

            var renderer = new DefaultScreenRenderer(view.providerCollection);
            var screenProvider = _customScreenProvider ?? _defaultScreenProvider;
            var screen = screenProvider?.Provide(view, device.Index, settings, renderer);

            return new VirtualDeviceParts
            {
                Input = input,
                Screen = screen
            };
        }
    }
}