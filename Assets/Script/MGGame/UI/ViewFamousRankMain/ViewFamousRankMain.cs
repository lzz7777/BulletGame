using Spine.Unity;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XN
{
public class ViewFamousRankMain : UIPanelBase
{
    public Button UIBgButton;
    public ScrollRect UIScrollViewScrollRect;
    public GridLayoutGroup UISVContentGridLayoutGroup;
    public Image UIHeadIconImage;
    public Image UIHeadFrameImage;
    public TextMeshProUGUI UIOneTitleTextMeshProUGUI;
    public TextMeshProUGUI UINameTextMeshProUGUI;
    public Text UINameText;
    public Image UIRulePanelImage;
    public TextMeshProUGUI UIRuleTitleTextMeshProUGUI;
    public TextMeshProUGUI UITop3TitleTextMeshProUGUI;
    public VerticalLayoutGroup UITop3ContentVerticalLayoutGroup;
    public TextMeshProUGUI UITop5TitleTextMeshProUGUI;
    public VerticalLayoutGroup UITop5ContentVerticalLayoutGroup;
    public TextMeshProUGUI UITop10TitleTextMeshProUGUI;
    public VerticalLayoutGroup UITop10ContentVerticalLayoutGroup;
    public TextMeshProUGUI UIStarRuleTipsTextMeshProUGUI;
    public Toggle UITgFamousToggle;
    public TextMeshProUGUI UIToggleFamousTextMeshProUGUI;
    public Toggle UITgRuleToggle;
    public TextMeshProUGUI UIToggleStarRuleTextMeshProUGUI;
    public TextMeshProUGUI UICloseTipsTextMeshProUGUI;

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
	public ItemStarList ItemStarList;
	public List<GameObject> RankListItems = new();
	public List<GameObject> RuleTop3List = new();
	public List<GameObject> RuleTop5List = new();
	public List<GameObject> RuleTop10List = new();

	private void Awake()
	{
		UITgFamousToggle.onValueChanged.AddListener(this.UITgFamousToggleOnValueChanged);
		UITgRuleToggle.onValueChanged.AddListener(this.UITgRuleToggleOnValueChanged);
		UIBgButton.onClick.AddListener(this.UIBgButtonOnClick);

	}
	#endregion
}
}
