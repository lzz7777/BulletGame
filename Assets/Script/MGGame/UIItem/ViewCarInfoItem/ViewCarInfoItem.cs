using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XN
{
public class ViewCarInfoItem : UIItemBase
{
    public Image UIPlayerNodeImage;
    public Text UIPlayerNameText;
    public Text UIPlayerMileageText;
    public Image UIPlayerLongNodeImage;
    public Text UIPlayerLongNameText;
    public Text UIPlayerLongMileageText;
    public Image UINobodyNodeImage;
    public Text UINobodyNameText;
    public TextMeshProUGUI UINobodyMileageTextMeshProUGUI;
    public Image UINobodyLongNodeImage;
    public Text UINobodyLongNameText;
    public TextMeshProUGUI UINobodyLongMileageTextMeshProUGUI;
    public HorizontalLayoutGroup UIMemberNodeHorizontalLayoutGroup;
    public RectTransform UICaptainNodeRectTransform;
    public Text UICaptainNameText;
    public RectTransform UIShieldNodeRectTransform;
    public Text UIShieldText;
    public Image UIItemProgressBgImage;
    public Image UIItemProgressImage;

	#region CustomFields  

	public Animation ViewCarAnimation;
	public ViewHeadItem HeadItem;
	public Animation ShieldAnimation;
	
	public ViewCarInfoItemNameplateType NameplateType { set; get; }
	public long TargetEntity { set; get; }
	public List<string> TempMemberIds { set; get; } = new();
	public List<string> MemberIds { set; get; } = new();
	public List<GameObject> MemberPrefabs { set; get; } = new();
	public List<float> CaptainNodePosXList { set; get; } = new(){112, 160, 78, 124};

	public Entity CarUnit => EntityManager.Instance.GetEntityById(TargetEntity);
	public CarViewComponent CarViewComponent => CarUnit?.GetComponent<CarViewComponent>();
	public CarInfoComponent CarInfoComponent => CarUnit?.GetComponent<CarInfoComponent>();
	
	private void Update()
	{
		this.OnUpdateSystem();
	}

	#endregion
}
}
