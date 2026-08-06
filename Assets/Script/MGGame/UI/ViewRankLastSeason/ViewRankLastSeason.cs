using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XN
{
public class ViewRankLastSeason : UIPanelBase
{
    public Button UIBgButton;
    public Image UITitleBgImage;
    public Image UITitleImage;
    public ToggleGroup UIToggleGroupToggleGroup;
    public Toggle UIToggleLastWeekToggle;
    public TextMeshProUGUI UIBtnName1TextMeshProUGUI;
    public Toggle UIToggleLastMounthToggle;
    public TextMeshProUGUI UIBtnName2TextMeshProUGUI;
    public Toggle UIToggleLastMilesToggle;
    public TextMeshProUGUI UIBtnName3TextMeshProUGUI;
    public RectTransform UINameGroupRectTransform;
    public TextMeshProUGUI UIDataIndexTextMeshProUGUI;
    public TextMeshProUGUI UIDataNameTextMeshProUGUI;
    public TextMeshProUGUI UIDataScoreTextMeshProUGUI;
    public TextMeshProUGUI UIDataSkinTextMeshProUGUI;
    public TextMeshProUGUI UIDataFansTextMeshProUGUI;
    public VerticalLayoutGroup UIScrollContentVerticalLayoutGroup;
    public TextMeshProUGUI UIRankSeasonTipsTextMeshProUGUI;
    public TextMeshProUGUI UISkinSeasonTipsTextMeshProUGUI;

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
	
	public cfg.RankType currToggle = cfg.RankType.PreviousWeekRank;
	public List<GameObject> RankListItems = new();

	private void Awake()
	{
		UIBgButton.onClick.AddListener(this.UIBgButtonOnClick);
		UIToggleLastWeekToggle.onValueChanged.AddListener(isOn => this.UIRankToggleOnChanged(cfg.RankType.PreviousWeekRank, isOn));
		UIToggleLastMounthToggle.onValueChanged.AddListener(isOn => this.UIRankToggleOnChanged(cfg.RankType.PreviousMonthRank, isOn));
		UIToggleLastMilesToggle.onValueChanged.AddListener(isOn => this.UIRankToggleOnChanged(cfg.RankType.PreviousMilestone, isOn));
		// UIWorldMonthToggle.onValueChanged.AddListener(isOn => this.UIWorldRankToggleOnChanged(cfg.RankType.MonthRank, isOn));

	}
	
	#endregion
}
}
