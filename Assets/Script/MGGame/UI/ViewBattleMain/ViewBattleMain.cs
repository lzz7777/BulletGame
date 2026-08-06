using Spine.Unity;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using Cysharp.Threading.Tasks;
using UnityEngine.Serialization;

namespace XN
{
public class ViewBattleMain : UIPanelBase
{
    public VerticalLayoutGroup UIGMVerticalLayoutGroup;
    public TMP_InputField UIGMPlayerIdTMP_InputField;
    public TMP_InputField UIGMCmdTMP_InputField;
    public Button UIGMSendButton;
    public Button UIButtonOverButton;
    public Button UIRankButton;
    public Button UIDtaDSkinsButton;
    public Button UIDataFamousButton;
    public Button UIDataMilesButton;
    public Button UIDataWeekSkinsButton;
    public TextMeshProUGUI UIScoreTextMeshProUGUI;
    public TextMeshProUGUI UIFansTextMeshProUGUI;
    public TextMeshProUGUI UITimeTextMeshProUGUI;
    public Button UIRankListButton;
    public Button UISkinListButton;
    public RectTransform UITakeCrownNodeRectTransform;
    public Text UITakeCrownText;
    public Button UIStartButton;
    public VerticalLayoutGroup UIVideoFansVerticalLayoutGroup;
    public Image UIFansIconImage;
    public TextMeshProUGUI UIVideoFansNameTextMeshProUGUI;
    public Text UIVideoFansNameText;
    public RawImage UIVideoRawImageRawImage;
    public RectTransform UIEntranceAnimRectTransform;
    public Image UIVideoScoreImage;
    public TextMeshProUGUI UIVideoWeekRankTextMeshProUGUI;
    public TextMeshProUGUI UIVideoScoreNameTextMeshProUGUI;
    public Text UIVideoScoreNameText;
    public TextMeshProUGUI UIVideoScoreTextMeshProUGUI;
    public RectTransform UIShowEntranceNodeRectTransform;
    public TextMeshProUGUI UIPlayerNameTextMeshProUGUI;
    public Text UIPlayerJoinCarNameText;
    public Text UICarNameText;
    public RawImage UIVideoHalfRawImage;
    public Image UIEndMaskImage;

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

	public Button GMLogoButton;
	public ViewHeadItem viewHeadItem;
	public TextMeshProUGUI UIHelpJionTextMeshProUGUI;
	public RectTransform UISubPageNodeRectTransform;
	
	public Vector3 lastRankNodePos;
	public Vector3 originalScoreLicensePos;

	// 百强+入场变量
	public  List<Func<UniTask>> Funcs = new List<Func<UniTask>>();
	public bool isVideoStart = false;
	public bool isFuncsRuning = false;
	public Queue<string> EntranceShowData = new();
	
    private void Awake()
    {
        VideoManager.Instance.RawImage = UIVideoRawImageRawImage;
        VideoManager.Instance.HaflScreenRawImage = UIVideoHalfRawImage;

        UIStartButton.onClick.AddListener(this.UIStartButtonOnClick);
        UIRankButton.onClick.AddListener(this.UIRankButtonOnClick);
        UIButtonOverButton.onClick.AddListener(this.UIButtonOverOnClick);
        UIGMSendButton.onClick.AddListener(this.UIGMSendButtonOnClick);
        GMLogoButton.onClick.AddListener(this.GMLogoButtonOnClick);
        
        // 主界面4排行
        UIDtaDSkinsButton.onClick.AddListener(this.UIDtaDSkinsButtonOnClick);
        UIDataFamousButton.onClick.AddListener(this.UIDataFamousButtonOnClick);
        UIDataMilesButton.onClick.AddListener(this.UIDataMilesButtonOnClick);
        UIDataWeekSkinsButton.onClick.AddListener(this.UIDataWeekSkinsButtonOnClick);
        UIRankListButton.onClick.AddListener(this.UIRankListButtonOnClick);
        
        UISkinListButton.onClick.AddListener(this.UISkinListButtonOnClick);
    }

    private void Update()
    {
        this.OnUpdateSystem();
    }

    #endregion
}
}
