//====================================================
//Author:lixin
//Time  :2025/11/27 18:13
//Desc  :
//====================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RenderHeads.Media.AVProVideo;
using Sirenix.OdinInspector;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XN
{
    public enum VideoType
    {
        [LabelText("开始")]
        Start,
        [LabelText("结束")]
        End,
        [LabelText("TODO联系程序增补")]
        ShowTodo,
    }
    
    public class VideoManager : MonoSingleton<VideoManager>
    {
        public bool IsInitialized { get; private set; }
        public ResolveToRenderTexture AllScreenResolveToRenderTexture;
        public ResolveToRenderTexture HalfScreenResolveToRenderTexture;
        public Material MatAllScreen;
        public Material MatHalfScreen;

        private MediaPlayer curMediaPlayer;
        private MediaPlayer curAllScreenMediaPlayer;
        public RawImage RawImage { get; set; }  // 在主战斗场景中绑定的RawImage
        public RawImage HaflScreenRawImage { get; set; }  // 在主战斗场景中绑定的RawImage
        [LabelText("流程视频列表")]
        public Dictionary<VideoType,MediaReference> MediaMap = new ();
        [LabelText("玩家入场资源列表"),ReadOnly]
        public Dictionary<string, MediaReference> PlayerMediaMap = new ();//  TODO 假装全是半屏视频
        
        private Dictionary<string, ResolveToRenderTexture> mediaToRenderTexturesDic = new ();
        private bool _isTest = true; // 测试 每个media 初始化示例
        
        // private List<MediaPlayer> _mediaPlayers;
        private MediaPlayer _mediaAll;
        private MediaPlayer _mediaHalf;

        private Action _callback1;
        private Action _callbackprocess;
        
        private List<string> isPlayingList = new();
        
#if UNITY_EDITOR
        [Button("收集资源")]
        public void CollectorPlayerMedia()
        {
            var dic = MediaMap.Values.Select(x=>x.name).ToArray();
            PlayerMediaMap.Clear();
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(MediaReference).FullName}",new[] { "Assets/Sources/Video" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path);
                if (!dic.Contains(name))
                {
                    var inst = AssetDatabase.LoadAssetAtPath<MediaReference>(path);
                    PlayerMediaMap.TryAdd(name, inst);
                }
            }
            Debug.Log($"PlayerMediaMap Count:{PlayerMediaMap.Count}", this);
        }
#endif
        protected override async void OnInit()
        {
            await UniTask.WaitUntil(() => YooAssetManager.Instance.IsInitialized);
            await UniTask.WaitUntil(() => TotalConfigManager.Instance.IsLoadOver);
            // List<ResolveToRenderTexture> list = new (){AllScreenResolveToRenderTexture, HalfScreenResolveToRenderTexture};
            // foreach (ResolveToRenderTexture resolve in list)
            // {
            //     var comp = resolve.GetComponent<MediaPlayer>();
            //     if (comp != null)
            //     {
            //         _mediaPlayers.Add(comp);
            //     }
            // }

            if (_isTest)
            {
                foreach (var kv in PlayerMediaMap)
                {
                    string name = kv.Key;
                    MediaReference mediaR = kv.Value;
                    
                    var gameObject = new GameObject(name);
                    gameObject.transform.SetParent(transform);
                    MediaPlayer mediaPlayer = gameObject.AddComponent<MediaPlayer>();
                    ResolveToRenderTexture resolveToRenderTexture = gameObject.AddComponent<ResolveToRenderTexture>();
                    resolveToRenderTexture.MediaPlayer = mediaPlayer;
                    // mediaPlayer.OpenMedia(mediaR, false);
                    mediaToRenderTexturesDic.TryAdd(name, resolveToRenderTexture);
                    
                    mediaPlayer.Events.AddListener(OnMediaPlayerEvent);
                }
            }
            
            IsInitialized = true;
        }
        
        void OnError(MediaPlayer mp, string errorMsg)
        {

            Debug.LogError(errorMsg);
            // 处理错误：清理状态并调用回调
            if (curMediaPlayer == mp)
            {
                HaflScreenRawImage?.gameObject.SetActive(false);
            }
                
            if (curAllScreenMediaPlayer == mp)
            {
                RawImage?.gameObject.SetActive(false);
                Action cb = _callback1;
                _callback1 = null;
                cb?.Invoke();
            }
        }
        
        void OnMediaPlayerEvent(MediaPlayer mp, MediaPlayerEvent.EventType et, ErrorCode errorCode)
        {
            if (et == MediaPlayerEvent.EventType.FinishedPlaying)
            {
                mp.CloseMedia();
            }
            
            // 首先检查是否有错误
            if (errorCode != ErrorCode.None)
            {
                string errorMessage = Helper.GetErrorMessage(errorCode);
                string mediaPlayerName = mp != null ? mp.name : "Unknown";
                // Debug.LogError($"[VideoManager] MediaPlayer [{mediaPlayerName}] 发生错误: {errorCode} - {errorMessage}", mp);
                OnError(mp, $"[VideoManager] MediaPlayer [{mediaPlayerName}] 发生错误: {errorCode} - {errorMessage}");
                return; // 错误情况下直接返回，不继续处理其他事件
            }
            
            // 处理 Error 事件类型（即使 errorCode 为 None，也可能触发 Error 事件）
            if (et == MediaPlayerEvent.EventType.Error)
            {
                string mediaPlayerName = mp != null ? mp.name : "Unknown";
                // Debug.LogError($"[VideoManager] MediaPlayer [{mediaPlayerName}] 触发 Error 事件", mp);
                OnError(mp, $"[VideoManager] MediaPlayer [{mediaPlayerName}] 触发 Error 事件");
                return;
            }
            
            // Debug.LogError($"MediaPlayer{mp} OnMediaPlayerEvent:" + et);
            if (curMediaPlayer == mp)
            {
                switch (et)
                {
                    case MediaPlayerEvent.EventType.Started:
                        HaflScreenRawImage.gameObject.SetActive(true);
                        break;
                    case MediaPlayerEvent.EventType.FinishedPlaying:
                        HaflScreenRawImage.gameObject.SetActive(false);
                        break;
                    // default:
                    // throw new ArgumentOutOfRangeException(nameof(et), et, null);
                }
            }
            
            if (curAllScreenMediaPlayer == mp)
            {
                switch (et)
                {
                    case MediaPlayerEvent.EventType.Started:
                        RawImage.gameObject.SetActive(true);
                        break;
                    case MediaPlayerEvent.EventType.FinishedPlaying:
                        RawImage.gameObject.SetActive(false);
                        Action cb = _callback1;
                        _callback1 = null;
                        cb?.Invoke();
                        break;
                    // default:
                    // throw new ArgumentOutOfRangeException(nameof(et), et, null);
                }
            }
        }
        protected override void OnRemove()
        {
        }

        /// <summary>
        /// 流程向资源播放
        /// </summary>
        /// <param name="type">枚举各个节点</param>
        /// <param name="onFinished"></param>
        public async UniTask PlayAsync(VideoType type, Action onFinished = null)
        {
            if (!MediaMap.TryGetValue(type, out var reference) || reference == null)
            {
                Debug.LogError($"流程视频列表 及 枚举未加入 {type}");
                onFinished?.Invoke();
                return;
            }
            
            _callbackprocess?.Invoke();
            _callbackprocess = onFinished;
            await PlayAsync(reference, AllScreenResolveToRenderTexture, () =>
            {
                _callbackprocess?.Invoke();
                _callbackprocess = null;
            });
        }
        
        /// <summary>
        /// 播放半屏礼物
        /// </summary>
        /// <param name="videoName"></param>
        /// <param name="onFinished"></param>
        public async UniTask PlayHalfScreenAsync(string videoName, Action onFinished = null)
        {
            if (GameConst.IsOptimized)
                return;
            
            if (!PlayerMediaMap.TryGetValue(videoName, out var reference) || reference == null)
            {
                onFinished?.Invoke();
                return;
            }

            if (_isTest)
            {
                if (!mediaToRenderTexturesDic.TryGetValue(videoName, out ResolveToRenderTexture resolveToRenderTexture) || resolveToRenderTexture == null)
                {
                    onFinished?.Invoke();
                    return;
                }
                
                EnsureExternalTexture(resolveToRenderTexture, 1080,720);
                HaflScreenRawImage.texture = resolveToRenderTexture.ExternalTexture;

                MediaPlayer player = resolveToRenderTexture.MediaPlayer;
                curMediaPlayer = player;
                player.Stop();
                player.Control.Seek(0.01);
                player.OpenMedia(reference);
                HaflScreenRawImage.gameObject.SetActive(true);
                UnityEngine.Debug.Log($"count {isPlayingList.Count}");
            }
            else
            {
                if (HalfScreenResolveToRenderTexture == null || reference == null)
                {
                    onFinished?.Invoke();
                    return;
                }
            
                EnsureExternalTexture(HalfScreenResolveToRenderTexture, 1080,720);
                var player = HalfScreenResolveToRenderTexture.MediaPlayer;
                if (player == null) return;

                HaflScreenRawImage.texture = HalfScreenResolveToRenderTexture.ExternalTexture;
                HaflScreenRawImage.gameObject.SetActive(true);
                player.OpenMedia(reference, true);
                await UniTask.WaitUntil(() => player.Control != null && player.Control.IsFinished());
                player.CloseMedia();
                HaflScreenRawImage.gameObject.SetActive(false);
            }

            onFinished?.Invoke();
        }
        
        /// <summary>
        /// 混合着背景音乐放 TopRank 入场
        /// </summary>
        /// <param name="videoName"></param>
        /// <param name="onFinished"></param>
        public void PlayPlayerDetailAsync(string videoName, Action onFinished = null)
        {
            if (GameConst.IsOptimized)
                return;
            
            if (!PlayerMediaMap.TryGetValue(videoName, out var reference) || reference == null)
            {
                Debug.LogError($"{videoName} 没有这个视频， 直接回调~~~");
                onFinished?.Invoke();
                return;
            }
            
            _callback1?.Invoke();
            _callback1 = onFinished;
            if(curAllScreenMediaPlayer != null)
            {
                curAllScreenMediaPlayer.Stop();
            }
            
            if (_isTest)
            {
                if (!mediaToRenderTexturesDic.TryGetValue(videoName, out ResolveToRenderTexture resolveToRenderTexture) || resolveToRenderTexture == null)
                {
                    _callback1?.Invoke();
                    return;
                }
                
                EnsureExternalTexture(resolveToRenderTexture, 1080,720);
                RawImage.texture = resolveToRenderTexture.ExternalTexture;

                var player = resolveToRenderTexture.MediaPlayer;
                if (player == null) return;

                curAllScreenMediaPlayer = player;
                switch (videoName)
                {
                    case "join":
                        RawImage.rectTransform.anchoredPosition = new Vector2(0, -600); // 对应全屏高度 720（1920 * 0.375）
                        RawImage.rectTransform.sizeDelta = new Vector2(0, -1200);
                        RawImage.material = MatHalfScreen;
                        break;
                    default:
                        RawImage.rectTransform.anchoredPosition = Vector2.zero;
                        RawImage.rectTransform.sizeDelta = Vector2.zero;
                        RawImage.material = MatAllScreen;
                        break;
                }

                player.Stop();
                player.Control.Seek(0.01);
                player.OpenMedia(reference);
                RawImage.gameObject.SetActive(true);
                // await UniTask.WaitUntil(() => player.Control != null && player.Control.IsFinished());
                
            }
            else
            {
                // await PlayAsync(reference, AllScreenResolveToRenderTexture, () =>
                // {
                //     _callback1?.Invoke();
                //     _callback1 = null;
                // }, videoName);
            }
        }

        public async UniTask PlayAsync(MediaReference reference, ResolveToRenderTexture resolver, Action onFinished = null, string videoName = "")
        {
            if (resolver == null || reference == null) return;
            EnsureExternalTexture(resolver);
            var player = resolver.MediaPlayer;
            if (player == null) return;

            RawImage.texture = resolver.ExternalTexture;
            switch (videoName)
            {
                case "join":
                    RawImage.rectTransform.anchoredPosition = new Vector2(0, -600); // 对应全屏高度 720（1920 * 0.375）
                    RawImage.rectTransform.sizeDelta = new Vector2(0, -1200);
                    RawImage.material = MatHalfScreen;
                    break;
                default:
                    RawImage.rectTransform.anchoredPosition = Vector2.zero;
                    RawImage.rectTransform.sizeDelta = Vector2.zero;
                    RawImage.material = MatAllScreen;
                    break;
            }
            RawImage.gameObject.SetActive(true);
            player.OpenMedia(reference, true);
            await UniTask.WaitUntil(() => player.Control != null && player.Control.IsFinished());
            player.CloseMedia();
            RawImage.gameObject.SetActive(false);

            onFinished?.Invoke();
        }

        public void SetMediaVolume(float volume)
        {
            if (AllScreenResolveToRenderTexture != null) AllScreenResolveToRenderTexture.MediaPlayer.AudioVolume = volume;
            if (HalfScreenResolveToRenderTexture != null) HalfScreenResolveToRenderTexture.MediaPlayer.AudioVolume = volume;
            foreach (ResolveToRenderTexture oneResolveToRT in mediaToRenderTexturesDic.Values)
            {
                oneResolveToRT.MediaPlayer.AudioVolume = volume;
            }
        }
        
        private void EnsureExternalTexture(ResolveToRenderTexture resolver, int width=1080, int height = 1920)
        {
            if (resolver.ExternalTexture == null)
            {
                var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
                rt.useMipMap = false;
                rt.filterMode = FilterMode.Bilinear;
                rt.wrapMode = TextureWrapMode.Clamp;
                resolver.ExternalTexture = rt;
            }
        }
    }

}