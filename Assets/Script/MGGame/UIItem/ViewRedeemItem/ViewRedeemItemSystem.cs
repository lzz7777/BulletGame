using cfg;
using cfg.Item;
using Cysharp.Threading.Tasks;

namespace XN
{
public static class ViewRedeemItemSystem
{
	#region CircleLife

	public static void OnRefresh(this ViewRedeemItem self, ViewRedeemItemData data)
	{
		StoreConfig oneConfig = data.config;
		YooAssetManager.Instance.LoadSpriteAsync(oneConfig.CostItemPic, self.UIIconImage, true).ToCoroutine();
		self.UINameTextMeshProUGUI.SetText($"{oneConfig.CostName}({oneConfig.CostDay})");
		self.UICmdTextMeshProUGUI.SetText($"兑换{oneConfig.GoodsId}");
		self.UICostTextMeshProUGUI.SetText(oneConfig.Cost.Number.ToString());
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
