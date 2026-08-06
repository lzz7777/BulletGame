using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XN
{
public class ViewItemShowItem : UIItemBase
{
    public Image UIFrameImage;
    public Text UINameText;
    public Text UICarNameText;
    public TextMeshProUGUI UIItemNameTextMeshProUGUI;
    public Image UIItemIconImage;
    public TextMeshProUGUI UIItemNumTextMeshProUGUI;

	#region CustomFields

	public ViewHeadItem ViewHeadItem;
	public ViewItemShowNode ParentNode;
	public CanvasGroup CanvasGroup;
	
	public float Time;
	public int InputId;
	public string PlayerId;
	public int ItemNum;
	public bool IsDisable;

	private void Update() => this.OnUpdateSystem();

	#endregion
}
}
