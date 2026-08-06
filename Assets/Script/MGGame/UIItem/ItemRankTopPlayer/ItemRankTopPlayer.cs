using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XN
{
public class ItemRankTopPlayer : UIItemBase
{
    public Image UIHeadIconImage;
    public Image UIHeadFgImage;
    public Image UIHeadFrameImage;
    public TextMeshProUGUI UINameTextMeshProUGUI;
    public Text UINameTxetText;

	#region CustomFields
	public ItemStarList StarGroupComp;
	public Image UIHeadBgImage;
	public Button UIHeadBgBtn;

	public Action<string> BtnOnClick;
	private void Awake()
	{
		UIHeadBgBtn.onClick.AddListener(this.UIHeadBgBtnOnClick);
	}

	
	#endregion
}
}
