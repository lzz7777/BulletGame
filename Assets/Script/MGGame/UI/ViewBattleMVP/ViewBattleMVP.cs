using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XN
{
public class ViewBattleMVP : UIPanelBase
{
    public Button UIBgButton;
    public RectTransform UIIMVPtem1RectTransform;
    public RectTransform UIIMVPtem2RectTransform;
    public RectTransform UIIMVPtem3RectTransform;
    public TextMeshProUGUI UIChampionTextTextMeshProUGUI;
    public TextMeshProUGUI UIChampionNameTextMeshProUGUI;
    public TextMeshProUGUI UITeam1TextMeshProUGUI;
    public TextMeshProUGUI UITeam2TextMeshProUGUI;
    public TextMeshProUGUI UITeam3TextMeshProUGUI;
    public VerticalLayoutGroup UIPlayerContentVerticalLayoutGroup;

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

	public GameObject Mvp1Item;
	public List<GameObject> Mvp2Items;
	public List<GameObject> PlayerItems;
	private void Awake()
	{
		UIBgButton.onClick.AddListener(this.UIBgButtonOnClick);
	}
	

	#endregion

}
}
