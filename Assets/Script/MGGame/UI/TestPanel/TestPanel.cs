using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XN
{
	public class TestPanel : UIPanelBase
	{
		public Image UITokenImage;
		public Button UITokenButton;
		public Text UITextLogText;
		public TextMeshProUGUI UITMPTextLogTextMeshProUGUI;

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

		public void Awake()
		{
			UITokenButton.onClick.AddListener(this.TestBtnClick);
			// UITokenButton.onClick.RemoveListener(this.testBtnClick);
		}

		#endregion
	}
}