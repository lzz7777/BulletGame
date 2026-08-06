using System;
using ByteDance.LiveOpenSdk.Runtime;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;

namespace XN
{
public static class TopSettingSystem
{
	#region CircleLife
    public static void OnOpenSystem(this TopSetting self, UIWindowData uIWindowData)
    {
        self.UIButtonScalerButton.gameObject.SetActive(!LiveOpenSdk.CloudGameApi.IsCloudGame());
        
        self.UIBgScalerButton.gameObject.SetActive(false);
        self.UIBgMixerButton.gameObject.SetActive(false);
        self.UIBgAnimButton.gameObject.SetActive(false);

        self.UISliderMusicSlider.value = SaveData.GetFloat(SaveData.Key.MusicVolume);
        self.RefreshButtonMute(SaveData.Key.MuteMusic);
        self.UISliderEffectSlider.value = SaveData.GetFloat(SaveData.Key.AudioVolume);
        self.RefreshButtonMute(SaveData.Key.MuteAudio);

        // TODO 开关百强入场动画获取例子
        Debug.Log($"SettingTop100Anim: {SaveData.GetInt(SaveData.Key.SettingTop100Anim)}");
        self.UIToggleAnimToggle.isOn = SaveData.GetInt(SaveData.Key.SettingTop100Anim, 0) == 1;
    }
    
    public static void OnCloseSystem(this TopSetting self)
    {
        
    }
	#endregion

    #region UIEvents
    
    public static void UIButtonScalerButtonOnClick(this TopSetting self)
    {
        bool active = self.UIBgScalerButton.gameObject.activeSelf;
        self.UIBgScalerButton.gameObject.SetActive(!active);
        self.UIPanelScalerImage.transform.position = new Vector3()
        {
            x = self.UIPanelScalerImage.transform.position.x,
            y = self.UIButtonScalerButton.transform.position.y,// - self.UIPanelScalerImage.sprite.rect.height/2f + 86/2f,   // 按钮位置Y - 高度一半 + 按钮高度一半
            z = self.UIPanelScalerImage.transform.position.z
        };
    }
    
    public static void UIButtonMixerButtonOnClick(this TopSetting self)
    {
        bool active = self.UIBgMixerButton.gameObject.activeSelf;
        self.UIBgMixerButton.gameObject.SetActive(!active);
        
        self.UISliderMusicSlider.value = SaveData.GetFloat(SaveData.Key.MusicVolume);
        self.UISliderEffectSlider.value = SaveData.GetFloat(SaveData.Key.AudioVolume);

        self.UIPanelMixerImage.transform.position = new Vector3()
        {
            x = self.UIPanelMixerImage.transform.position.x,
            y = self.UIButtonMixerButton.transform.position.y,
            z = self.UIPanelMixerImage.transform.position.z
        };
    }
    
    public static void UIButtonAnimButtonOnClick(this TopSetting self)
    {
        bool active = self.UIBgAnimButton.gameObject.activeSelf;
        self.UIBgAnimButton.gameObject.SetActive(!active);
        
        self.UIToggleAnimToggle.transform.position = new Vector3()
        {
            x = self.UIToggleAnimToggle.transform.position.x,
            y = self.UIButtonAnimButton.transform.position.y,
            z = self.UIToggleAnimToggle.transform.position.z
        };
    }

    
    public static void UIButtonMuteOnClick(this TopSetting self, SaveData.Key mute)
    {
        int nextState = SaveData.GetInt(mute) == 0 ? 1 : 0;
        Debug.Log($"{mute} ---- {SaveData.GetInt(mute)} --> {nextState} ");
        SaveData.SetInt(mute, nextState);
        self.RefreshButtonMute(mute);
    }

    public static void RefreshButtonMute(this TopSetting self, SaveData.Key muteKey)
    {
        bool isMute = SaveData.GetInt(muteKey) == 1;
        switch (muteKey)
        {
            case SaveData.Key.MuteMusic:
                string iconMusic = isMute ? "icon_yinyue02" : "icon_yinyue";
                YooAssetManager.Instance.LoadSpriteAsync(iconMusic,self.UIMusicButton.image,true).ToCoroutine();
                SoundManager.Instance.SetMusicMute(isMute);
                break;
            case SaveData.Key.MuteAudio:
                string iconAudio = isMute ? "icon_yinxiao02" : "icon_yinxiao";
                YooAssetManager.Instance.LoadSpriteAsync(iconAudio,self.UIEffectButton.image,true).ToCoroutine();
                SoundManager.Instance.SetAudioMute(isMute);
                break;
        }

    }
    
    #endregion
    
    #region GlobalEvents
    
    #endregion
    
    #region Logics
    
    public static void TopSettingRefresh(this TopSetting self, string key)
    {
        switch (key)
        {
            case "ViewMain":
                self.UIButtonScalerButton.gameObject.SetActive(true);
                self.UIButtonScalerButton.transform.parent.transform.localScale = Vector3.one * 1f;
                break;
            case "ViewBattleMain":
                self.UIButtonScalerButton.gameObject.SetActive(false);
                self.UIButtonScalerButton.transform.parent.transform.localScale = Vector3.one * 0.8f;
                break;
            default:
                Debug.LogWarning($"{key} is not a valid ??? TODO");
                break;
        }
    }
    #endregion
}
}
