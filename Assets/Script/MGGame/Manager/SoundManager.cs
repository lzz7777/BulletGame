// ******************************************************************
// @file       SoundManager.cs
// @brief      全局音效管理器
// @author     
// @Modified   2025/11/
// @Copyright  Copyright (c) 2025
// ******************************************************************

using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;
using XN;

namespace XN
{
    public class SoundManager : MonoSingleton<SoundManager>
{
    [SerializeField] [LabelText("漂移音效集合")] private AudioClip[] _driftSound;
    [LabelText("音效混响")] [SerializeField] private AudioMixer mixer;

    [LabelText("游戏状态对应播放哪个音乐")] public Dictionary<MGGameState, AudioClip> bgMusicMaps = new();

    private AudioSource audioSourceMusic;
    private AudioSource audioSourceEffect;
    private string currentMusic;

    [SerializeField, LabelText("最小百分比")] private float minPercent = 0f;
    [SerializeField, LabelText("最大百分比")] private float maxPercent = 1f;
    [SerializeField, LabelText("最小dB阈值[静音]")] private float minDb = -80f;
    [SerializeField, Range(0f, 1f),LabelText("播放视频Music音效")] private float whenVideoMusic = 0.2f;
    public bool IsInitialized { get; private set; }
    protected override async void OnInit()
    {
        await UniTask.WaitUntil(() => YooAssetManager.Instance.IsInitialized);
        await UniTask.WaitUntil(() => TotalConfigManager.Instance.IsLoadOver);
        await UniTask.WaitUntil(() => VideoManager.Instance.IsInitialized);
        
        if (FindObjectOfType<AudioListener>() == null)
            Debug.LogError("场景中未找到 AudioListener，音频将无法播放");

        InitMusic();

        float aValRaw = SaveData.GetFloat(SaveData.Key.AudioVolume,1);
        float mValRaw = SaveData.GetFloat(SaveData.Key.MusicVolume,1);
        float aVal = aValRaw > 1f ? Mathf.Clamp01(aValRaw / 100f) : Mathf.Clamp01(aValRaw);
        float mVal = mValRaw > 1f ? Mathf.Clamp01(mValRaw / 100f) : Mathf.Clamp01(mValRaw);
        AudioVolume = aVal;
        MusicVolume = mVal;
        
        IsInitialized = true;
    }
    public AudioMixerGroup TryGetGroup(string groupName)
    {
        if (mixer == null)
        {
            Debug.LogError("AudioMixer 未设置，无法路由音频");
            return null;
        }
        var groups = mixer.FindMatchingGroups(groupName);
        if (groups == null || groups.Length == 0)
        {
            Debug.LogWarning($"未找到名为 '{groupName}' 的Mixer分组，请检查AudioMixer配置");
            return null;
        }
        return groups[0];
    }

    private float PercentToLinear(float percent)
    {
        var p = Mathf.Clamp(percent, minPercent, maxPercent);
        return Mathf.Clamp01(p);
    }

    private float LinearToPercent(float linear)
    {
        return Mathf.Clamp(linear, minPercent, maxPercent);
    }

    private float LinearToDb(float linear)
    {
        if (linear <= 0f) return minDb;
        return Mathf.Clamp(20f * Mathf.Log10(linear), minDb, 0f);
    }

    private static float DbToLinear(float db)
    {
        return Mathf.Clamp01(Mathf.Pow(10f, db / 20f));
    }

    private float NormalizePercent(float v)
    {
        return v > 1f ? Mathf.Clamp01(v / 100f) : Mathf.Clamp01(v);
    }

    public float AudioVolume
    {
        get
        {
            if (audioSourceEffect != null) return LinearToPercent(audioSourceEffect.volume);
            if (mixer != null && mixer.GetFloat("AudioVolume", out var volumeDb))
                return LinearToPercent(DbToLinear(volumeDb));
            return 1f;
        }
        set
        {
            var linear = PercentToLinear(value);
            SaveData.SetFloat(SaveData.Key.AudioVolume, value);
            if (audioSourceEffect != null) audioSourceEffect.volume = linear;
            if (mixer != null) mixer.SetFloat("AudioVolume", LinearToDb(linear));
            VideoManager.Instance.SetMediaVolume(linear);
        }
    }

    public float MusicVolume
    {
        get
        {
            if (audioSourceMusic != null) return LinearToPercent(audioSourceMusic.volume);
            if (mixer != null && mixer.GetFloat("MusicVolume", out var volumeDb))
                return LinearToPercent(DbToLinear(volumeDb));
            return 1f;
        }
        set
        {
            var linear = PercentToLinear(value);
            SaveData.SetFloat(SaveData.Key.MusicVolume, value);
            if (audioSourceMusic != null) audioSourceMusic.volume = linear;
            if (mixer != null) mixer.SetFloat("MusicVolume", LinearToDb(linear));
        }
    }


    private async void InitMusic()
    {
        if (audioSourceMusic == null)
        {
            audioSourceMusic = GetComponentsInChildren<AudioSource>(true).FirstOrDefault(x => x.name == "Music");
            if (audioSourceMusic == null)
            {
                var go = new GameObject("Music");
                go.transform.SetParent(transform);
                audioSourceMusic = go.AddComponent<AudioSource>();
            }
            audioSourceMusic.playOnAwake = false;
            audioSourceMusic.loop = true;
            audioSourceMusic.spatialBlend = 0f;
            audioSourceMusic.outputAudioMixerGroup = TryGetGroup("Music");
        }

        if (audioSourceEffect == null)
        {
            audioSourceEffect = GetComponentsInChildren<AudioSource>(true).FirstOrDefault(x => x.name == "Audio");
            if (audioSourceEffect == null)
            {
                var go = new GameObject("Audio");
                go.transform.SetParent(transform);
                audioSourceEffect = go.AddComponent<AudioSource>();
            }
            audioSourceEffect.playOnAwake = false;
            audioSourceEffect.loop = false;
            audioSourceEffect.spatialBlend = 0f;
            audioSourceEffect.outputAudioMixerGroup = TryGetGroup("Audio");
        }
    }


    public void PlayMusic(MGGameState state)
    {
        if (bgMusicMaps.TryGetValue(state, out var ac)) PlayMusic(ac);
    }

    /// <summary>
    /// 长音乐，如背景音乐
    /// </summary>
    /// <param name="ac"></param>
    public void PlayMusic(AudioClip ac)
    {
        InitMusic();

        if (ac == null)
        {
            Debug.LogWarning("PlayMusic 传入的 AudioClip 为 null");
            return;
        }

        if (currentMusic == ac.name)
        {
            if (!audioSourceMusic.isPlaying)
            {
                audioSourceMusic.Play();
            }
            return;
        }

        audioSourceMusic.clip = ac;
        audioSourceMusic.Play();

        currentMusic = ac.name;
    }

    public void PauseMusic()
    {
        audioSourceMusic?.Pause();
    }
    public void ContinueMusic()
    {
        if (audioSourceMusic != null && !audioSourceMusic.isPlaying)
        {
            audioSourceMusic?.Play();
        }
    }
    
    
    /// <summary>
    /// 短音效 多段Shot可并行
    /// </summary>
    /// <param name="ac"></param>
    public void PlaySound(AudioClip ac)
    {
        if (ac == null)
        {
            Debug.LogWarning("PlaySound 传入的 AudioClip 为 null");
            return;
        }

        InitMusic();

        if (audioSourceEffect == null)
        {
            Debug.LogError("Audio 音源未初始化，无法播放短音效");
            return;
        }

        audioSourceEffect.PlayOneShot(ac);
    }

#if UNITY_EDITOR
    [Button("测试音效")]
    public void PlaySound()
    {
        List<AudioClip> acs = new List<AudioClip>();
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AudioClip", new [] { "Assets/Bundle/Sounds/sound" });
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("Game") || path.Contains("音效"))
            {
                acs.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path));
            }
        }
        
        PlaySound(acs[Random.Range(0, acs.Count)]);
    }
#endif

    public void SetMusicMute(bool mute)
    {
        InitMusic();
        var percent = NormalizePercent(SaveData.GetFloat(SaveData.Key.MusicVolume, 1));
        if (mixer != null) mixer.SetFloat("MusicVolume", mute ? minDb : LinearToDb(PercentToLinear(percent)));
        if (audioSourceMusic != null) audioSourceMusic.volume = mute ? 0f : PercentToLinear(percent);
    }

    /// <summary>
    /// 阶段性出现过需求：  放着背景音乐，当放视频的时候，混淆降低部分音效
    /// </summary>
    public void SetMusicVideoStart()
    {
        InitMusic();
        var percent = NormalizePercent(SaveData.GetFloat(SaveData.Key.MusicVolume, 1));
        percent *= SaveData.GetInt(SaveData.Key.MuteMusic) == 1 ? 0 : 1;
        if (audioSourceMusic != null) audioSourceMusic.volume = PercentToLinear(percent* whenVideoMusic);
    }
    public void SetMusicVideoClose()
    {
        InitMusic();
        var percent = NormalizePercent(SaveData.GetFloat(SaveData.Key.MusicVolume, 1));
        percent *= SaveData.GetInt(SaveData.Key.MuteMusic) == 1 ? 0 : 1;
        if (audioSourceMusic != null) audioSourceMusic.volume = PercentToLinear(percent);
    }
    
    public bool IsMusicMuted()
    {
        if (mixer != null && mixer.GetFloat("MusicVolume", out var db)) return db <= minDb + 0.001f;
        if (audioSourceMusic != null) return audioSourceMusic.volume <= 0f;
        return false;
    }

    public void ToggleMusicMute()
    {
        SetMusicMute(!IsMusicMuted());
    }

    public void SetAudioMute(bool mute)
    {
        InitMusic();
        var percent = NormalizePercent(SaveData.GetFloat(SaveData.Key.AudioVolume, 1));
        if (mixer != null) mixer.SetFloat("AudioVolume", mute ? minDb : LinearToDb(PercentToLinear(percent)));
        float volume = mute ? 0f : PercentToLinear(percent);
        if (audioSourceEffect != null) audioSourceEffect.volume = volume;
        VideoManager.Instance.SetMediaVolume(volume);
    }

    public bool IsAudioMuted()
    {
        if (mixer != null && mixer.GetFloat("AudioVolume", out var db)) return db <= minDb + 0.001f;
        if (audioSourceEffect != null) return audioSourceEffect.volume <= 0f;
        return false;
    }

    public void ToggleAudioMute()
    {
        SetAudioMute(!IsAudioMuted());
    }

    protected override void OnRemove()
    {
    }
}
}
