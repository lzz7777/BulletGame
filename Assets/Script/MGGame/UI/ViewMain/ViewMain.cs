using Spine.Unity;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using Cysharp.Threading.Tasks;

namespace XN
{
public class ViewMain : UIPanelBase
{
    public Image UITitleImage;
    public ToggleGroup UINamesToggleGroup;
    public Toggle UITextNameToggle;
    public Toggle UIZodiacNameToggle;
    public Toggle UIFreeNameToggle;
    public Button UIButtonRankButton;
    public Button UIButtonMapButton;
    public ToggleGroup UIModelsToggleGroup;
    public Button UIStartButton;
    public Button UIMapNodeButton;
    public ToggleGroup UIToggleGroupToggleGroup;
    public Button UIReportBtnButton;

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

	public Image UIMapImage;
	public List<GameObject> ToggleDescItems;
	public cfg.FightRoomType currRoomType;
	public int currRoomId;
	public List<GameObject> MapToggleItems;
	private void Awake()
	{
		this.LoadTitleLogo();
		UIButtonRankButton.onClick.AddListener(this.UIButtonRankButtonOnClick);

		UIStartButton.onClick.AddListener(this.UIStartButtonOnClick);
		UIButtonMapButton.onClick.AddListener(this.UIButtonMapButtonOnClick);
		UIMapNodeButton.onClick.AddListener(this.UIMapNodeButtonOnClick);
		UIReportBtnButton.onClick.AddListener(this.UIReportBtnButtonOnClick);
		
		UITextNameToggle.onValueChanged.AddListener(isOn => this.UIToggleRoomOnValueChanged(isOn, cfg.FightRoomType.TextRoom));
		UIZodiacNameToggle.onValueChanged.AddListener(isOn => this.UIToggleRoomOnValueChanged(isOn, cfg.FightRoomType.ZodiacRoom));
		UIFreeNameToggle.onValueChanged.AddListener(isOn => this.UIToggleRoomOnValueChanged(isOn, cfg.FightRoomType.FreeRoom));
			
		for (int i = 0; i < ToggleDescItems.Count; i++)
		{
			int index = i;
			ToggleDescItems[i].GetComponent<Toggle>()?.onValueChanged.AddListener(isOn => this.UIToggleOneRoomTimeOnValueChange(isOn, index));
		}
	}

	#endregion
}
}
