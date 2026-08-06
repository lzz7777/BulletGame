using Spine.Unity;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XN
{
public class TopSetting : UIPanelBase
{
    public Button UIButtonScalerButton;
    public Button UIButtonMixerButton;
    public Button UIButtonAnimButton;
    public Button UIBgScalerButton;
    public Image UIPanelScalerImage;
    public ToggleGroup UIToggleScalerToggleGroup;
    public Button UIBgMixerButton;
    public Image UIPanelMixerImage;
    public Button UIEffectButton;
    public Slider UISliderEffectSlider;
    public Button UIMusicButton;
    public Slider UISliderMusicSlider;
    public Button UIBgAnimButton;
    public Toggle UIToggleAnimToggle;

	public override void OnOpen(UIWindowData uIWindowData)
	{
		base.OnOpen();
		this.OnOpenSystem(uIWindowData);
	}

	public override void OnClose()
	{
		base.OnClose();
		this.OnCloseSystem();
	}

	#region CustomFields

	public float ratio = 9f / 16f;
    private float lastResizeTime = 0f;
    private const float resizeDebounceTime = 0.2f;
    private bool isResizing = false;
	
	private void Awake()
	{
		EventsManager.AddListener<string>(GameEnum.TopSettingRefreshEvent, this.TopSettingRefresh);
		UIButtonScalerButton.onClick.AddListener(this.UIButtonScalerButtonOnClick);
		UIButtonMixerButton.onClick.AddListener(this.UIButtonMixerButtonOnClick);
		UIButtonAnimButton.onClick.AddListener(this.UIButtonAnimButtonOnClick);
		UIBgScalerButton.onClick.AddListener(this.UIButtonScalerButtonOnClick);
		UIBgMixerButton.onClick.AddListener(this.UIButtonMixerButtonOnClick);
		UIBgAnimButton.onClick.AddListener(this.UIButtonAnimButtonOnClick);
		
		UIEffectButton.onClick.AddListener(delegate { this.UIButtonMuteOnClick(SaveData.Key.MuteAudio); });
		UIMusicButton.onClick.AddListener(delegate { this.UIButtonMuteOnClick(SaveData.Key.MuteMusic); });
		UISliderEffectSlider.onValueChanged.AddListener(value => SoundManager.Instance.AudioVolume = value);
		UISliderMusicSlider.onValueChanged.AddListener(value => SoundManager.Instance.MusicVolume = value);
		UIToggleAnimToggle.onValueChanged.AddListener(isOn => SaveData.SetInt(SaveData.Key.SettingTop100Anim, isOn ? 1 : 0));
	}

	private void OnDestroy()
	{
		EventsManager.RemoveListener<string>(GameEnum.TopSettingRefreshEvent, this.TopSettingRefresh);
	}

	private void Start()
	{
		if (DySdkManager.IsCloudGame())
		{
			InitCloundGameScaler();
		}
		else
		{
			InitSetScreenScaler();
		}
	}

	private void InitSetScreenScaler()
	{
		int screenScaler = PlayerPrefs.GetInt("ScreenScaler", 0);
		screenScaler = screenScaler == 0 ? 1 : screenScaler;
		
		foreach (Transform child in UIToggleScalerToggleGroup.transform)
		{
			Toggle toggle = child.GetComponent<Toggle>();
			toggle.onValueChanged.AddListener((bool isOn) =>
			{
				if (isOn)
				{
					Debug.Log($"{child.name} isOn");
					SetScreenScaler(System.Convert.ToInt32(child.name));
				}
			});
		}
		
		Transform tChild = UIToggleScalerToggleGroup.transform.GetChild(screenScaler - 1);
		Toggle tToggle = tChild.GetComponent<Toggle>();
		tToggle.isOn = true;
		Debug.Log(tToggle);
	}
	
	private void SetScreenScaler(int scaler)
	{
		int width = Screen.width;
		int height = Screen.height;
		switch (scaler)
		{
			case 1:
				width = 540;
				height = 960;
				break;
			case 2:
				width = 720;
				height = 1280;
				break;
			case 3:
				width = 1080;
				height = 1920;
				break;
			default:
				width = 1080;
				height = 1920;
				break;
		}
		
		Resolution current = Screen.currentResolution;
		
		if (width > current.width || height > current.height)
		{
			// 计算缩放比例，保持宽高比
			float widthRatio = (float)current.width / width;
			float heightRatio = (float)current.height / height;
			float scale = Mathf.Min(widthRatio, heightRatio);
            
			// 计算缩放后的分辨率
			int scaledWidth = Mathf.FloorToInt(width * scale);
			int scaledHeight = Mathf.FloorToInt(height * scale);
            
			// 设置窗口化分辨率
			Screen.SetResolution(scaledWidth, scaledHeight, false);
            
			Debug.Log($"高分辨率 {width}x{height} 已缩放为 {scaledWidth}x{scaledHeight} | minscale {scale}");
		}
		else
		{
			Screen.SetResolution(width, height, false);
		}
		
		PlayerPrefs.SetInt("ScreenScaler", scaler);
		PlayerPrefs.Save();
	}

	private void InitCloundGameScaler()
	{
		UnityEngine.Debug.Log($"CommandLine {CommandLine.GetArg(CommandKey.IsCloud)} | {CommandLine.GetArg(CommandKey.Fullscreen)}");
		UnityEngine.Debug.Log($"CommandLine {CommandLine.GetArg(CommandKey.ScreenWidth)} x {CommandLine.GetArg(CommandKey.ScreenHeight)}");

		if (CommandLine.TryGetArg(CommandKey.Fullscreen, out var full) 
		    && CommandLine.TryGetArg(CommandKey.ScreenWidth, out string width)
		    && CommandLine.TryGetArg(CommandKey.ScreenHeight, out string height)
		    )
		{
			bool fullScreen = full == "1";
			Screen.SetResolution(int.Parse(width), int.Parse(height), fullScreen);
		}
	}
	
	private void OnRectTransformDimensionsChange()
	{
        if (DySdkManager.IsCloudGame()) return;
		if (!isResizing)
		{
			isResizing = true;
		}
		lastResizeTime = Time.time;
	}

	private void Update()
	{
		if (isResizing && Time.time - lastResizeTime > resizeDebounceTime)
		{
			if (!Input.GetMouseButton(0))
			{
				isResizing = false;
				UpdateScreen();
			}
		}
	}
	
	public void UpdateScreen()
	{
		if (DySdkManager.IsCloudGame()) return;

		if (Screen.fullScreen) return;
		
		Resolution maxRes = Screen.currentResolution;
		int maxWidth = maxRes.width;
		int maxHeight = maxRes.height;
        
        int minWidth = 540;
        int minHeight = 960;

		int currentW = Screen.width;
		int currentH = Screen.height;

		// 1. 以宽度为基准计算目标高度
		int targetW = currentW;
		int targetH = Mathf.RoundToInt(targetW / ratio);

        // 2. 最小尺寸限制
        if (targetW < minWidth)
        {
            targetW = minWidth;
            targetH = Mathf.RoundToInt(targetW / ratio);
        }
        if (targetH < minHeight)
        {
            targetH = minHeight;
            targetW = Mathf.RoundToInt(targetH * ratio);
        }

		// 3. 如果高度超出屏幕，限制高度并反推宽度
		if (targetH > maxHeight)
		{
			targetH = maxHeight;
			targetW = Mathf.RoundToInt(targetH * ratio);
		}

		// 4. 如果宽度超出屏幕，限制宽度并反推高度
		if (targetW > maxWidth)
		{
			targetW = maxWidth;
			targetH = Mathf.RoundToInt(targetW / ratio);
		}

		// 5. 双向检查：如果修正后的高度又超出了最大高度（因为宽度反推高度可能导致高度溢出），需要再次以高度为基准限制
		if (targetH > maxHeight)
		{
			targetH = maxHeight;
			targetW = Mathf.RoundToInt(targetH * ratio);
		}
		
		// 6. 只有当尺寸偏差超过阈值时才调整，避免频繁抖动
		if (Mathf.Abs(currentW - targetW) > 2 || Mathf.Abs(currentH - targetH) > 2)
		{
			Screen.SetResolution(targetW, targetH, false);
			Debug.Log($"分辨率 maxRes {maxRes} | current:{currentW}x{currentH} | target:{targetW}x{targetH}");
		}
	}
	
	#endregion
}
}
