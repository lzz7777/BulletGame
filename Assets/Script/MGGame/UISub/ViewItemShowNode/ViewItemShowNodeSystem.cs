using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XN
{
	public static class ViewItemShowNodeSystem
	{
		#region CircleLife

		public static void OnOpenSystem(this ViewItemShowNode self, UIWindowData uIWindowData = null)
		{
			(self.transform as RectTransform).anchoredPosition = uIWindowData.Pos;
		}

		public static void OnCloseSystem(this ViewItemShowNode self)
		{
		}
		
		public static async UniTask OnRefresh(this ViewItemShowNode self, ViewItemShowNodeData data)
		{
			//InputIndex.InputNumber数量为1可叠加，不为1建个新的
			var inputConf = TotalConfigManager.ConfigManager.InputIndexConfigCategory.Get(data.InputId);
			if (inputConf == null || inputConf.InputNumber == 0)
			{
				return;
			}

			if (inputConf.InputNumber == 1)
			{
				if (self.ItemDic.TryGetValue((data.PlayerId, data.InputId), out var tempViewItem))
				{
					tempViewItem.OnRefresh(new ViewItemShowItemData()
					{
						PlayerId = data.PlayerId,
						InputId = data.InputId,
					});
				}
				else
				{
					var item = await self.CreateItem(data);

					if (item != null)
					{
						self.ItemDic[(data.PlayerId, data.InputId)] = item;
					}
				}
				
				return;
			}
			
			//InputIndex.InputNumber > 1,建个新的
			self.CreateItem(data);
		}

		#endregion

		#region UIEvents

		#endregion

		#region GlobalEvents

		#endregion

		#region Logics

		private static async UniTask<ViewItemShowItem> CreateItem(this ViewItemShowNode self, ViewItemShowNodeData data)
		{
			var obj = await ObjectPoolManager.Instance.GetFromPool<ViewItemShowItem>(self.transform);
			if (!obj)
				return null;
			
			var viewItem = obj.GetComponent<ViewItemShowItem>();
			viewItem.ParentNode = self;
			viewItem.OnInit(new ViewItemShowItemData()
			{
				PlayerId = data.PlayerId,
				InputId = data.InputId,
			});

			return viewItem;
		}

		public static bool JudgeShowVideo(this ViewItemShowNode self, ViewItemShowNodeData viewItemShowNodeData)
		{
			return !self.ItemDic.ContainsKey((viewItemShowNodeData.PlayerId, viewItemShowNodeData.InputId));
		}

		public static void RemoveData(this ViewItemShowNode self, string playerId, int inputId)
		{
			self.ItemDic.Remove((playerId, inputId));
		}

		#endregion
	}
}