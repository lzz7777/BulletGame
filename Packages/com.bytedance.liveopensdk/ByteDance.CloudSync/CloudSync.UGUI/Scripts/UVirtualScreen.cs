using UnityEngine;

namespace ByteDance.CloudSync.UGUI
{
    /// <summary>
    /// 虚拟 Screen Provider 接口，可用于实现自定义的 Screen 设备、及其行为
    /// </summary>
    public interface IUVirtualScreenProvider
    {
        UVirtualScreen Provide(UCloudView view, SeatIndex index, IRenderSettings renderSettings, DefaultScreenRenderer renderer);
    }

    /// <summary>
    /// 默认的 UVirtualScreenProvider
    /// </summary>
    public class DefaultUVirtualScreenProvider : IUVirtualScreenProvider
    {
        public UVirtualScreen Provide(UCloudView view, SeatIndex index, IRenderSettings renderSettings, DefaultScreenRenderer renderer)
        {
            return new UVirtualScreen(view, index, renderSettings, renderer);
        }
    }

    /// <summary>
    /// UGUI 下默认的虚拟 Screen。
    /// 负责控制一个用户的 CloudView 视图 、 ScreenRenderer 渲染混合器、 发起推流。
    /// </summary>
    /// <remarks>
    /// 若要实现自定义的虚拟 Screen：可以继承此<see cref="UVirtualScreen"/>实现一个子类，并在云同步初始化、传入 new <see cref="UDeviceFactory"/> 构造参数时，使用自定义实现的<see cref="IUVirtualScreenProvider"/>并令其创建和返回你自定义实现的 <see cref="UVirtualScreen"/> 子类对象。
    /// </remarks>
    public class UVirtualScreen : VirtualScreen
    {
        private readonly UCloudView _view;

        /// <summary>
        /// 视图
        /// </summary>
        public UCloudView CloudView => _view;

        /// <summary>
        /// 房主的View保持Active
        /// </summary>
        /// <remarks>默认 true: 保持Active会避免房主<see cref="ICloudSeat.OnWillDestroy"/>的倒计时过程中，意外地禁用掉了房主的gameObject及其可能待处理的逻辑、协程。</remarks>
        public bool KeepHostViewActive { get; set; } = true;

        public UVirtualScreen(UCloudView view, SeatIndex index, IRenderSettings settings, IScreenRenderer renderer)
            : base(index, view, settings, renderer)
        {
            _view = view;
        }

        public override bool Enable
        {
            get => base.Enable;
            set
            {
                base.Enable = value;
                SetViewActive(value);
            }
        }

        protected virtual void SetViewActive(bool value)
        {
            if (_view == null)
            {
                Debug.LogWarning("UVirtualScreen view (UCloudView) has been destroyed");
                return;
            }

            if (!value && KeepHostViewActive && Index.IsHost())
                return;

            Debug.Log($"UVirtualScreen view SetActive: {value}");
            _view.gameObject.SetActive(value);
        }
    }
}