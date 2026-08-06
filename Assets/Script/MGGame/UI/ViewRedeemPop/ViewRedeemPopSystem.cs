using cfg.Item;
using Cysharp.Threading.Tasks;

namespace XN
{
public static class ViewRedeemPopSystem
{
	#region CircleLife
    public static void OnOpenSystem(this ViewRedeemPop self, UIWindowData uIWindowData)
    {
	    self.InitRedeemUI().ToCoroutine();
    }
    
    public static void OnCloseSystem(this ViewRedeemPop self)
    {
        
    }
	#endregion

    #region UIEvents
    #endregion
    
    #region GlobalEvents
    #endregion
    
    #region Logics

    public static async UniTask InitRedeemUI(this ViewRedeemPop self)
    {
	    if (self.RedeemItemList.Count > 0) return;

	    StoreConfigCategory storeConfigCategory = TotalConfigManager.ConfigManager.StoreConfigCategory;
	    foreach (StoreConfig d in storeConfigCategory.DataList)
	    {
		    if (!string.IsNullOrEmpty(d.CostItemPic))
		    {
			    var obj = await ObjectPoolManager.Instance.GetFromPool<ViewRedeemItem>(self.UIContentGridLayoutGroup.transform);
			    self.RedeemItemList.Add(obj);
			    var itemData = new ViewRedeemItemData()
			    {
				    config = d
			    };
			    obj.GetComponent<ViewRedeemItem>().OnRefresh(itemData);
		    }
	    }
    }
    
    #endregion
}
}
