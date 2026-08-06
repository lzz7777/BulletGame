
// Copyright@www.bytedance.com
// Author: zhouxu.ken
// Date: 2024/06/08
// Description:

// #define SEND_VIDEO_FRAME_DEBUG
using UnityEngine;

namespace ByteDance.CloudSync
{
    public enum VideoStreamMode
    {
        /// <summary>
        /// 云游戏环境推流
        /// <seealso cref="CloudGameVideoStream"/>
        /// </summary>
        CloudGame,

        /// <summary>
        /// 本地推流，空实现
        /// <seealso cref="LocalVideoStream"/>
        /// </summary>
        Local
    }

    /// <summary>
    /// 云游戏画面推流
    /// </summary>
    internal interface IVideoStream
    {
        void Write(IVirtualScreen screen, in TextureAndHandle frame);
    }

#if SEND_VIDEO_FRAME_DEBUG
    internal class VideoSteamDebug
    {
        private static int TestCount = 0;

        public static void SaveDebugFrame(Texture texture)
        {
            if (TestCount < 100)
            {
                var outputDir = Application.persistentDataPath;
                var outputImagePath = $"{outputDir}/frame_{TestCount}.png";
                var texture2D = ConvertToTexture2D(texture);
                CGLogger.Log($"SendVideoFrame - save image to: {outputImagePath}," +
                             $"textureId: {texture.GetNativeTexturePtr()}, " +
                             $"texture2DId: {texture2D.GetNativeTexturePtr()}");
                SaveTextureAsPNG(texture2D, outputImagePath);
                ++TestCount;
            }
        }
        
        static Texture2D ConvertToTexture2D(Texture source)
        {
            RenderTexture renderTex = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0);

            Graphics.Blit(source, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            Texture2D readableText = new Texture2D(source.width, source.height);
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);

            return readableText;
        }

        static void SaveTextureAsPNG(Texture2D texture, string fullPath)
        {
            byte[] bytes = texture.EncodeToPNG();

            string directory = System.IO.Path.GetDirectoryName(fullPath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            System.IO.File.WriteAllBytes(fullPath, bytes);
            Debug.Log("Texture saved as PNG at " + fullPath);
        }
    }
#endif

    /// <summary>
    /// 云游戏环境推流
    /// </summary>
    internal class CloudGameVideoStream : IVideoStream
    {
        public void Write(IVirtualScreen screen, in TextureAndHandle textureAndHandle)
        {
            if (textureAndHandle.handle == 0)
            {
                CGLogger.Log($"textureAndHandle.handle is 0");
                return;
            }

            var result = CloudGameSdk.API.SendVideoFrame(screen.Index, textureAndHandle.handle);
            if (result != ICloudGameAPI.ErrorCode.Success)
            {
                CGLogger.LogError($"sendVideoFrame error: result code : {result} index : {screen.Index} ");
            }

#if SEND_VIDEO_FRAME_DEBUG
                VideoSteamDebug.SaveDebugFrame(textureAndHandle.texture);
#endif
        }
    }

    /// <summary>
    /// 本地推流，空实现
    /// </summary>
    internal class LocalVideoStream : IVideoStream
    {
        public void Write(IVirtualScreen screen, in TextureAndHandle frame)
        {
#if SEND_VIDEO_FRAME_DEBUG
            VideoSteamDebug.SaveDebugFrame(frame.texture);
#endif
        }
    }
}