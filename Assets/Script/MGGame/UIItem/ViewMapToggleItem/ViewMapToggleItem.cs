using System;
using TMPro;
using UnityEngine.UI;

namespace XN
{
public class ViewMapToggleItem : UIItemBase
{
    public Image UIBgImage;
    public Image UIBackgroundImage;
    public TextMeshProUGUI UITextTextMeshProUGUI;

	#region CustomFields
	public Toggle toggle;
	public int toggleIndex;
	public Action<int> OnClick;

	private void Awake()
	{
		toggle.onValueChanged.AddListener(this.UIOneMapToggleOnChanged);
	}
	#endregion
}
}
