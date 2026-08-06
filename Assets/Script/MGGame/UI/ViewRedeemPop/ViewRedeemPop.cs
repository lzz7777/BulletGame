using Spine.Unity;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;

namespace XN
{
public class ViewRedeemPop : UIPanelBase
{
    public Button UIMaskButton;
    public RectTransform UIPanel1RectTransform;
    public GridLayoutGroup UIContentGridLayoutGroup;
    public RectTransform UIPanel2RectTransform;
    public Image UIContent2Image;
    public ToggleGroup UIToggleGroupToggleGroup;
    public Toggle UIToggle1Toggle;
    public TextMeshProUGUI UIToggle1NameTextMeshProUGUI;
    public Toggle UIToggle2Toggle;
    public TextMeshProUGUI UIToggle2NameTextMeshProUGUI;
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
	public List<GameObject> RedeemItemList = new ();

	private void Awake()
	{
		UIBtnCloseButton.onClick.AddListener(this.Close);
		UIMaskButton.onClick.AddListener(this.Close);
	}
	
	#endregion
}
}
