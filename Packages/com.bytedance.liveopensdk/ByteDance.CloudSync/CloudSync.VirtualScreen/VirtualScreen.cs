// Copyright@www.bytedance.com
// Author: zhouxu.ken
// Date: 2024/06/07
// Description:

using UnityEngine;

namespace ByteDance.CloudSync
{
    /// <summary>
    /// 云游戏推流画面
    /// </summary>
    public interface IVirtualScreen
    {
        /// <summary>
        /// 设备分辨率
        /// </summary>
        Vector2Int Resolution { get; }

        Texture RenderTexture { get; }

        SeatIndex Index { get; }

        bool Enable { get; set; }

        object View { get; }

        void Init();
    }

    /// <summary>
    /// 虚拟 Screen。
    /// 负责控制一个用户的 View 视图、 ScreenRenderer 渲染混合器、 发起推流。
    /// </summary>
    public class VirtualScreen : IVirtualScreen
    {
        private readonly IRenderSettings _settings;
        private readonly IScreenRenderer _screenRenderer;
        private bool _enable = false;
        private bool _firstFrame = false;

        public SeatIndex Index { get; }

        public Vector2Int Resolution => _settings.Resolution;

        public Texture RenderTexture => _screenRenderer.Frame.texture;



        internal IScreenRenderer ScreenRenderer => _screenRenderer;

        public VirtualScreen(SeatIndex index, object view, IRenderSettings settings, IScreenRenderer renderer)
        {
            Index = index;
            View = view;
            _settings = settings;
            _screenRenderer = renderer;
        }

        public object View { get; }

        public void Init()
        {
            _screenRenderer.Init(this);
        }

        public void OnCameraRender()
        {
            if (!Enable || !_screenRenderer.IsReady)
                return;
            if (!_firstFrame)
            {
                CGLogger.Log($"VirtualScreen OnCameraRender 用户第一次推流 Index: {Index} 推流分辨率为{RenderTexture.width}x{RenderTexture.height}");

                _firstFrame = true;
            }
            var frame = _screenRenderer.Render();
            VirtualScreenSystem.VideoStream.Write(this, frame);
        }

        /// <summary>
        /// 对应 ClientHandlerBase.OnEnable/OnDisable
        /// </summary>
        public virtual bool Enable
        {
            set
            {
                _enable = value;
                if (value)
                {
                    if (!_screenRenderer.IsReady)
                    {
                        ResolutionUpdatable.CheckStreamTexture((int)Index, Resolution);
                        if (!Index.IsHost())
                        {
                            if (ResolutionUpdatable.TryGetHostResolution(out Vector2Int resolution))
                            {
                                CGLogger.Log($"VirtualScreen Create 设置推流分辨率 Index: {Index} 推流分辨率为{resolution.x}x{resolution.y}");
                                _settings.Resolution = resolution;
                            }
                        }
                    }
                    _screenRenderer.OnEnable();
                }

            }
            get => _enable;
        }
    }
}