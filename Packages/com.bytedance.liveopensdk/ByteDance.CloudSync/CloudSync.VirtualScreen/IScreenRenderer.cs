// Copyright@www.bytedance.com
// Author: zhouxu.ken
// Date: 2024/06/08
// Description:

using UnityEngine;

namespace ByteDance.CloudSync
{
    public enum ScreenTextureType
    {
        Base,
        Tex1,
        Tex2,
        Tex3
    }

    public interface IScreenTextureCollection
    {
        Texture Get(ScreenTextureType type, IVirtualScreen screen);
        void SetObserver(IScreenTextureCollectionObserver observer);
    }

    public interface IScreenTextureCollectionObserver
    {
        void OnCollectionChanged();
    }

    public interface IRenderSettings
    {
        Vector2Int Resolution { get; set; }
    }

    public interface IScreenRenderer
    {
        void Init(IVirtualScreen screen);

        void SetMode(VideoStreamMode mode);

        void OnEnable();

        bool IsReady { get; }

        TextureAndHandle Frame { get; }

        TextureAndHandle Render();
    }
}