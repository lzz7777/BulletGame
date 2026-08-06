using UnityEngine;

namespace ByteDance.CloudSync.TextureProvider
{
    /// <summary>
    /// Camera 渲染成 RT
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DllMonoBehaviour]
    public class CameraTextureProvider : ScreenTextureProvider
    {
        public Camera Camera => _camera;
        private Camera _camera;

        [Header("Debug only. Do Not Modify:")]
        public RenderTexture rt;

        public override Texture GetTexture(IVirtualScreen screen)
        {
            if (rt == null)
            {
                rt = new RenderTexture(screen.Resolution.x, screen.Resolution.y, 32, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                rt.antiAliasing = 2;
                rt.name = $"CameraTextureProvider.RT-{this.GetInstanceID()}";
                _camera = GetComponent<Camera>();
                _camera.targetTexture = rt;
            }

            return rt;
        }
    }
}