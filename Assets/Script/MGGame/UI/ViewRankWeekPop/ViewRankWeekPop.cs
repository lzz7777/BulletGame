	using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XN
{
public class ViewRankWeekPop : UIPanelBase
{
    public Button UIMaskButton;
    public Image UITitleImage;
    public TextMeshProUGUI UITitle1TextMeshProUGUI;
    public TextMeshProUGUI UITitle2TextMeshProUGUI;
    public RectTransform UIPanel1RectTransform;
    public Image UIContent1Image;
    public TextMeshProUGUI UIDescTextMeshProUGUI;
    public RectTransform UIPanel2RectTransform;
    public Image UIContent2Image;
    public ToggleGroup UIToggleGroupToggleGroup;
    public Toggle UIToggleCarToggle;
    public TextMeshProUGUI UINameCarTextMeshProUGUI;
    public Toggle UIToggleAirToggle;
    public TextMeshProUGUI UINameAirTextMeshProUGUI;
    public TextMeshProUGUI UICloseHintTextMeshProUGUI;
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
	
	private void Awake()
	{
		UIBtnCloseButton.onClick.AddListener(this.UIBtnCloseButtonOnClick);
		UIMaskButton.onClick.AddListener(this.UIMaskButtonOnClick);
		this.Init();
	}
	
	#endregion
}
}
