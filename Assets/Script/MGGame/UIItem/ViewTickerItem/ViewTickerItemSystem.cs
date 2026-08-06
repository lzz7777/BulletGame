namespace XN
{
	public static class ViewTickerItemSystem
	{
		#region CircleLife

		public static void OnRefresh(this ViewTickerItem self, ViewTickerItemData data)
		{
			self.UIContentTextMeshProUGUI.text = data.Content;
			self.UIOilImage.gameObject.SetActive(data.IsShowOil);
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