using Spine.Unity;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using Cysharp.Threading.Tasks;

namespace XN
{
public class ViewWorldRankMain : UIPanelBase
{
    public Button UIRankFamousTopButton;
    public TextMeshProUGUI UITitleTextMeshProUGUI;
    public HorizontalLayoutGroup UIFamousTop3NodeHorizontalLayoutGroup;
    public Toggle UIWorldCurRoomToggle;
    public Toggle UIWorldWeekToggle;
    public Toggle UIWorldFansToggle;
    public Toggle UIWorldMonthToggle;
    public Toggle UIMaximumRangeToggle;
    public TextMeshProUGUI UIToggleDetailTitle1TextMeshProUGUI;
    public TextMeshProUGUI UIToggleDetailTitle2TextMeshProUGUI;
    public TextMeshProUGUI UIToggleDetailTitle3TextMeshProUGUI;
    public TextMeshProUGUI UIToggleDetailTitle4TextMeshProUGUI;
    public TextMeshProUGUI UIToggleDetailTitle5TextMeshProUGUI;
    public Button UIToggleDetailFansButton;
    public VerticalLayoutGroup UIScrollContentVerticalLayoutGroup;
    public TextMeshProUGUI UIRankDateTextMeshProUGUI;
    public Button UIBtnBackMainButton;
    public Button UIButtonOneMoreAgainButton;
    public Button UIButtonSelectModeButton;
    public TextMeshProUGUI UIWorldRankTitleTextMeshProUGUI;

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

	/// <summary>
	/// World2Rank(世界排行)
	/// Room2Rank(游戏结算排行)
	/// </summary>
	public string OpenType = "";
	public List<GameObject> Top3Items = new();
	public List<GameObject> RankListItems = new();
	public cfg.RankType currToggle = cfg.RankType.None;
	private void Awake()
	{
		UIBtnBackMainButton.onClick.AddListener(this.UIBtnBackMainButtonOnClick);
		UIButtonOneMoreAgainButton.onClick.AddListener(this.UIButtonOneMoreAgainButtonOnClick);
		UIButtonSelectModeButton.onClick.AddListener(this.UIButtonSelectModeButtonOnClick);
		UIRankFamousTopButton.onClick.AddListener(this.UIRankFamousTopButtonOnClick);
		UIToggleDetailFansButton.onClick.AddListener(this.UIToggleDetailFansButtonOnClick);
		// 排行榜toggle内容
		UIWorldCurRoomToggle.onValueChanged.AddListener(isOn => this.UIWorldRankToggleOnChanged(cfg.RankType.None, isOn));
		UIWorldWeekToggle.onValueChanged.AddListener(isOn => this.UIWorldRankToggleOnChanged(cfg.RankType.WeekRank, isOn));
		UIWorldFansToggle.onValueChanged.AddListener(isOn => this.UIWorldRankToggleOnChanged(cfg.RankType.FansRank, isOn));
		UIWorldMonthToggle.onValueChanged.AddListener(isOn => this.UIWorldRankToggleOnChanged(cfg.RankType.MonthRank, isOn));
		UIMaximumRangeToggle.onValueChanged.AddListener(isOn => this.UIWorldRankToggleOnChanged(cfg.RankType.KillRank, isOn));

	}
	#endregion
}
}
