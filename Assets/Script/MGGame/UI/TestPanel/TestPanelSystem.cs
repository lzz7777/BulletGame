using Unity.VisualScripting;
using UnityEngine;

namespace XN
{
	public static class TestPanelSystem
	{
		#region CircleLife

		public static void OnOpenSystem(this TestPanel self, UIWindowData uIWindowData)
		{

		}

		public static void OnCloseSystem(this TestPanel self)
		{

		}

		#endregion

		#region UIEvents

		public static void TestBtnClick(this TestPanel self)
		{
			var token = string.Empty;
			token = CommandLine.GetArg(CommandKey.DyToken);
			self.UITextLogText.text = "Click text token :\n" + token;
			self.UITMPTextLogTextMeshProUGUI.text = "Click tmptxt token :\n" + token;

			// 剪切板
			GUIUtility.systemCopyBuffer = token;
			if (string.IsNullOrEmpty(token))
			{
				GUIUtility.systemCopyBuffer = "No token";
			}
			// post


		}

		#endregion

		#region GlobalEvents

		#endregion

		#region Logics

		#endregion
	}
}