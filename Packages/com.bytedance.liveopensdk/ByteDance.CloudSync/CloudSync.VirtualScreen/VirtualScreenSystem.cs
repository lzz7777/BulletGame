// Copyright@www.bytedance.com
// Author: Admin
// Date: 2024/06/07
// Description:

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ByteDance.CloudSync
{
    public interface IVirtualScreenSystem
    {
        void Create(SeatIndex index, VirtualScreen screen);

        void SetStreamMode(VideoStreamMode mode);
    }

    public class VirtualScreenSystem : MonoBehaviour, IVirtualScreenSystem
    {
        private static VirtualScreenSystem _instance;

        public static IVirtualScreenSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject(nameof(VirtualScreenSystem));
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<VirtualScreenSystem>();
                }

                return _instance;
            }
        }

        public static IVirtualScreen Find(SeatIndex index)
        {
            return _instance?.FindImpl(index);
        }

        internal static void Destroy(IVirtualScreen screen)
        {
            var vs = (VirtualScreen)screen;
            _instance?._screens.Remove(vs);
            _instance?._screensMap.Remove(vs.Index);
        }

        /// <summary>
        /// 销毁所有 VirtualScreen，在 CloudSyncSdk 退出时调用
        /// </summary>
        internal static void DestroyAll()
        {
            if (!_instance) return;

            _instance.RemoveAll();
            Destroy(_instance);
            _instance = null;
        }

        /// <summary>
        /// 默认推流云游戏模式
        /// </summary>
        internal static IVideoStream VideoStream = Application.isEditor
            ? new LocalVideoStream()
            : new CloudGameVideoStream();

        internal static int TargetDisplayIndex => SupportsModifyTargetDisplay ? 4 : 0;

        private readonly List<VirtualScreen> _screens = new();
        private readonly Dictionary<SeatIndex, VirtualScreen> _screensMap = new();
        private static int _canUseTargetDisplay;

        private void Awake()
        {
            _instance = this;
        }

        private void LateUpdate()
        {
            foreach (var screen in _screens)
            {
                if (screen.Enable)
                {
                    screen.OnCameraRender();
                }
            }
        }

        /*private void OnEnable()
        {
            RenderPipelineManager.endCameraRendering -= OnCameraRendered;
            RenderPipelineManager.endCameraRendering += OnCameraRendered;
        }

        private void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= OnCameraRendered;
        }

        private void OnDestroy()
        {
            RenderPipelineManager.endCameraRendering -= OnCameraRendered;
        }

        private void OnCameraRendered(ScriptableRenderContext context, Camera c)
        {
            // todo: 待确认 main camera 未加载完成或缺失时，是否导致推流画面异常，是否需要在 main camera ready 后才推流

            // Main Camera 渲染在各 UI Camera 之前已经完成（前提）
            // 在 UI Camera 渲染完成后才发送当前帧，以保证场景和 UI 都是最新的
            foreach (var screen in _screens)
            {
                if (screen is { Enable: true } && screen.Canvas.UICamera == c)
                {
                    screen.OnCameraRender();
                }
            }
        }
        */

        public void Create(SeatIndex index, VirtualScreen screen)
        {
            _screens.Add(screen);
            _screensMap.Add(index, screen);
        }

        public void SetStreamMode(VideoStreamMode mode)
        {
            CGLogger.Log($"Change video stream mode to: {mode}");
            switch (mode)
            {
                case VideoStreamMode.CloudGame:
                    VideoStream = new CloudGameVideoStream();
                    break;
                case VideoStreamMode.Local:
                    VideoStream = new LocalVideoStream();
                    break;
                default: throw new NotSupportedException();
            }

            foreach (var screen in _screensMap.Values)
            {
                screen.ScreenRenderer.SetMode(mode);
            }
        }

        private void RemoveAll()
        {
            foreach (var screen in _screens.ToArray())
            {
                Destroy(screen);
            }

            _screens.Clear();
            _screensMap.Clear();
        }

        private IVirtualScreen FindImpl(SeatIndex index)
        {
            return _screensMap.GetValueOrDefault(index);
        }

        public static void SetTargetDisplay(Camera camera)
        {
            // 防止主屏响应离屏的触摸事件，和RemoteInput中的事件输入UnityInputSystem.QueueStateEvent里displayIndex对应上就可以
            if (CanUseCameraTargetDisplay)
                camera.targetDisplay = TargetDisplayIndex;
        }

        // note: 默认 false - 不修改 targetDisplay ，可避免bug例如：无法拖动ScrollView的scrollBar。
        public static bool SupportsModifyTargetDisplay = false;

        /// 是否可用camera的targetDisplay。 兼容性区别：Unity 2021 不设置, 而>=2022则应设置。（同为InputSystem 1.7.0的情况下）
        private static bool CanUseCameraTargetDisplay
        {
            get
            {
                if (!SupportsModifyTargetDisplay)
                    return false;
                if (_canUseTargetDisplay > 0)
                    return _canUseTargetDisplay == 1;

                var canUse = UnityUtil.IsUnityVersionGte(2022, out var major);
                CGLogger.LogDebug($"CanUseTargetDisplay: {canUse}, for unity: {major} ({Application.unityVersion})");
                _canUseTargetDisplay = canUse ? 1 : 2;
                return canUse;
            }
        }
    }
}