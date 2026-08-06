using Cysharp.Threading.Tasks;

namespace XN
{
public static class ViewCarInfoPlayerItemSystem
{
	#region CircleLife

	public static async UniTask OnRefresh(this ViewCarInfoPlayerItem self, ViewCarInfoPlayerItemData data)
	{
		var carInfoComp = EntityManager.Instance.GetEntityById(data.CarId).GetComponent<CarInfoComponent>();
		
		if (data.DriveType == 0)
		{
			self.UITmpTextMeshProUGUI.text = "主驾";
			self.UIspImage.sprite = await YooAssetManager.Instance.LoadSpriteAsync("bg_wanfa301");
		}
		else
		{
			self.UITmpTextMeshProUGUI.text = "副驾";
			self.UIspImage.sprite = await YooAssetManager.Instance.LoadSpriteAsync("bg_wanfa302");
		}

		float size = carInfoComp.Group == 0 && data.DriveType == 0 ? 52 : 44;
		await self.ViewHeadItem.OnRefresh(new ViewHeadItemData()
		{
			PlayerId = data.PlayerId,
			SizeData = new(size, size),
		});
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
