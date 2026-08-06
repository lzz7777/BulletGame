using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace ByteDance.CloudSync
{
    public struct TextureAndHandle
    {
        public Texture texture;
        public long handle;
        public GraphicsFormat format;
    }

    internal interface IShareRenderTexture
    {
        TextureAndHandle Create(int width, int height);
    }

    internal abstract class ShareRenderTexture : IShareRenderTexture
    {
        public ColorSpace GetColorSpace()
        {
            return QualitySettings.activeColorSpace;
        }

        public virtual GraphicsFormat GetFormat()
        {
            var colorSpace = GetColorSpace();
            switch (colorSpace)
            {
                case ColorSpace.Gamma:
                    return GraphicsFormat.R8G8B8A8_UNorm;
                case ColorSpace.Linear:
                case ColorSpace.Uninitialized:
                default:
                    return GraphicsFormat.R8G8B8A8_SRGB;
            }
        }

        public abstract TextureAndHandle Create(int width, int height);
    }

    /// <summary>
    /// 用于推流的缓冲池，避免C++(云游戏sdk) 和 C#(UGC) 同时对同一张贴图进行操作引发冲突
    /// </summary>
    internal class PCShareRenderTexture : ShareRenderTexture
    {
        public override TextureAndHandle Create(int width, int height)
        {
            var format = GetFormat();
            CGLogger.Log($"CreateRenderTexture - PC, {GetColorSpace()}, {format}");
            var f = UnityNativeToolsCSharp.FromUnityFormat(format);
            var srv = UnityNativeToolsCSharp.createSharedTexture(out IntPtr handle, width, height, f);
            var tex = Texture2D.CreateExternalTexture(width, height, TextureFormat.RGBA32, false, false, srv);

            return new TextureAndHandle()
            {
                texture = tex,
                handle = (Int64)handle,
                format = format
            };
        }
    }


    internal class AndroidShareRenderTexture : ShareRenderTexture
    {
        public override TextureAndHandle Create(int width, int height)
        {
            var format = GetFormat();
            CGLogger.Log($"CreateRenderTexture - Android, {GetColorSpace()}, {format}");
            var tex = new RenderTexture(width, height, 0, format);
            //For RenderTexture you might need to access .colorBuffer property before you call RenderTexture.GetNativeTexturePtr() for the first time.
            return new TextureAndHandle()
            {
                texture = tex,
                handle = tex.GetNativeTexturePtr().ToInt64(),
                format = format
            };
        }
    }
}