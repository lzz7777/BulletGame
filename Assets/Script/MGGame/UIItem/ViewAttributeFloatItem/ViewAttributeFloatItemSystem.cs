using cfg;
using UnityEngine;

namespace XN
{
	public static class ViewAttributeFloatItemSystem
	{
		#region CircleLife

		public static void OnRefresh(this ViewAttributeFloatItem self, ViewAttributeFloatItemData data)
		{
			int value = data.ChangeValue;

			switch (data.ChangeType)
			{
				case ChangeType.MileageAddPct:
				case ChangeType.MileageAddValue:
					//加速
					self.UIParamJiaSuText.gameObject.transform.localScale = Vector3.one;
					self.UIParamJiTuiText.gameObject.transform.localScale = Vector3.zero;
					self.JiaSuAnimation.Play();
					
					self.UIParamJiaSuText.text = value.ToString();
					break;
				case ChangeType.MileageDelPct:
				case ChangeType.MileageDelValue:
					//击退
					self.UIParamJiaSuText.gameObject.transform.localScale = Vector3.zero;
					self.UIParamJiTuiText.gameObject.transform.localScale = Vector3.one;
					self.JiTuiAnimation.Play();
						
					self.UIParamJiTuiText.text = value.ToString();
					break;
			}
		}

		#endregion

		#region UIEvents

		#endregion

		#region GlobalEvents

		#endregion

		#region Logics

		#endregion
	}
}