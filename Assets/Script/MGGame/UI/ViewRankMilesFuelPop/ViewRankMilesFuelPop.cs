using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XN
{
public class ViewRankMilesFuelPop : UIPanelBase
{
    public Button UIMaskButton;
    public TextMeshProUGUI UIMonthTextMeshProUGUI;
    public TextMeshProUGUI UIDayTextMeshProUGUI;
    public TextMeshProUGUI UITopScoreTextMeshProUGUI;
    public TextMeshProUGUI UITopDescTextMeshProUGUI;
    public RectTransform UIMilesItemRectTransform;
    public Image UIHeadIconImage;
    public VerticalLayoutGroup UIMilesContentVerticalLayoutGroup;
    public TextMeshProUGUI UIEndTimeTextMeshProUGUI;
    public RectTransform UIFuelNodeRectTransform;
    public TextMeshProUGUI UIFuelRuleTextMeshProUGUI;
    public TextMeshProUGUI UIRankMileIndexTextMeshProUGUI;
    public TextMeshProUGUI UIFuelTextMeshProUGUI;
    public RectTransform UITemplateRectTransform;
    public RectTransform UIFuelItemRectTransform;
    public TextMeshProUGUI UIRankitemValue1TextMeshProUGUI;
    public TextMeshProUGUI UIRankitemValue2TextMeshProUGUI;
    public VerticalLayoutGroup UIFuelContentVerticalLayoutGroup;
    public TextMeshProUGUI UIHintTextMeshProUGUI;
    public ToggleGroup UIToggleGroupToggleGroup;
    public Button UIBtnCloseButton;

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
	
	public List<GameObject> UIItemList = new ();
	public List<GameObject> FuelItemList = new ();
	private void Awake()
	{
		UIBtnCloseButton.onClick.AddListener(this.UIBtnCloseButtonOnClick);
		UIMaskButton.onClick.AddListener(this.UIMaskButtonOnClick);
	}
	#endregion
}
}
