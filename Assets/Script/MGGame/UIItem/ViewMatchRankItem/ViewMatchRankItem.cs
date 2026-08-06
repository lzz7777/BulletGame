using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XN
{
public class ViewMatchRankItem : UIItemBase
{
    public TextMeshProUGUI UITmpRankIndexTextMeshProUGUI;
    public Text UITmpNameText;
    public HorizontalLayoutGroup UILayoutHorizontalLayoutGroup;

	#region CustomFields
	
	public List<GameObject> Objs = new();
	
	#endregion
}
}
