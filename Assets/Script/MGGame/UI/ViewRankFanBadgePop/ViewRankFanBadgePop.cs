using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XN
{
public class ViewRankFanBadgePop : UIPanelBase
{
    public Button UIBgButton;
    public Image UIIconImage;
    public TextMeshProUGUI UIDescTextMeshProUGUI;
    public Button UILeftButton;
    public Button UIRightButton;

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

	public int currIndex;
	private void Awake()
	{
		UIBgButton.onClick.AddListener(this.UIBgButtonOnClick);
		UILeftButton.onClick.AddListener(this.UILeftButtonOnClick);
		UIRightButton.onClick.AddListener(this.UIRightButtonOnClick);
	}
	#endregion
}
}
