// ******************************************************************
// @file       MediaPlayerManager.cs
// @brief      视频播放辅助
// @author     SamuelZon, zonsamuel@gmail.com
//             
// @Modified   2024-07-29
// @Copyright  Copyright (c) 2024, BarrageKnight
// ******************************************************************

using System.Collections.Generic;
using System.Linq;
using RenderHeads.Media.AVProVideo;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public enum MediaPlayerTag
{
    [LabelText("大哥进场")] Boss = 0,
    [LabelText("加载页")] Loading = 1,
}

public enum BossJoinIndexTag
{
    [LabelText("第一名")] A = 0,
    [LabelText("第二名")] B,
    [LabelText("第三名")] C,
    [LabelText("第四-四十九名")] D,
    [LabelText("大于五十名")] E
}

public struct DoubleMediaReference
{
    public MediaReference Media;
    // public MediaReference Alpha;
}

public struct MediaPlayerInfo
{
    [LabelText("视频对象")] public ResolveToRenderTexture Render;
    [LabelText("视频宽度")] public int RenderTextureSizeX;
    [LabelText("视频高度")] public int RenderTextureSizeY;
    [ShowInInspector] public MediaPlayer Media => Render != null ? Render.MediaPlayer : null;
    [ShowInInspector] public RenderTexture Texture => Render != null ? Render.ExternalTexture : null;
}

public class MediaPlayerManager : MonoSingleton<MediaPlayerManager>
{
    [LabelText("对应视频组件")] [SerializeField]
    private readonly Dictionary<MediaPlayerTag, MediaPlayerInfo> _videoMap = new();

    [LabelText("对应透明通道")] [SerializeField] private readonly Dictionary<MediaPlayerTag, Texture> _alphaMap = new();

    [LabelText("大哥进场对应视频")] [SerializeField]
    private readonly Dictionary<BossJoinIndexTag, DoubleMediaReference> _bossVideoMap = new();

    protected override void OnInit()
    {
        var keys = _videoMap.Keys.ToArray();
        foreach (var key in keys)
        {
            var item = _videoMap[key];
            var renderTexture = RenderTexture.GetTemporary(item.RenderTextureSizeX, item.RenderTextureSizeY, 24,
                RenderTextureFormat.ARGB32);
            renderTexture.graphicsFormat = GraphicsFormat.R16G16B16A16_UNorm;
            renderTexture.antiAliasing = 8;
            renderTexture.filterMode = FilterMode.Trilinear;
            renderTexture.anisoLevel = 0;
            renderTexture.useMipMap = false;
            item.Render.ExternalTexture = renderTexture;
            _videoMap[key] = item;
        }
    }

    protected override void OnRemove()
    {
    }

    public static bool GetInfo(MediaPlayerTag tag, out MediaPlayerInfo info)
    {
        return Instance._videoMap.TryGetValue(tag, out info);
    }

    public static bool GetAlphaTexture(MediaPlayerTag tag, out Texture tex)
    {
        return Instance._alphaMap.TryGetValue(tag, out tex);
    }

    public static bool GetBossVideo(BossJoinIndexTag tag, out DoubleMediaReference info)
    {
        return Instance._bossVideoMap.TryGetValue(tag, out info);
    }

    public BossJoinIndexTag testBossTag;

    [Button("测试大哥视频")]
    public void TestBossVideo()
    {
        if (GetBossVideo(testBossTag, out var mediaReference))
        {
            if (GetInfo(MediaPlayerTag.Boss, out var bossJoinMediaPlayerInfo))
            {
                bossJoinMediaPlayerInfo.Media.OpenMedia(mediaReference.Media);
            }
        }
    }
}