using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace XN
{
	public static class ViewTickerSystem
	{
		#region CircleLife

		public static void OnOpenSystem(this ViewTicker self, UIWindowData uIWindowData)
		{
			self.Time = self.ExistTime;
		}

		public static void OnCloseSystem(this ViewTicker self)
		{
			self.Datas.Clear();
			ObjectPoolManager.Instance.ReturnToPool(self.Items);
			self.Items.Clear();
		}

		public static void OnUpdateSystem(this ViewTicker self)
		{
			self.Time += Time.deltaTime;

			if (self.Time > self.ExistTime)
			{
				if (self.Datas.Count != 0)
				{
					self.Time = 0;
					self.ShowItem(self.Datas.Dequeue());
					return;
				}
				
				self.Close();
			}
		}

		#endregion

		#region UIEvents

		#endregion

		#region GlobalEvents

		#endregion

		#region Logics

		public static void AddData(this ViewTicker self, ViewTickerItemData viewTickerItemData)
		{
			self.Datas.Enqueue(viewTickerItemData);
		}

		private static async UniTask ShowItem(this ViewTicker self, ViewTickerItemData viewTickerItemData)
		{
			var obj = await ObjectPoolManager.Instance.GetFromPool<ViewTickerItem>(self.UIFrameImage.transform);
			self.Items.Add(obj);
			var viewTickerItem = obj.GetComponent<ViewTickerItem>();
			viewTickerItem.OnRefresh(viewTickerItemData);
			
			obj.transform.localPosition = new Vector3(obj.transform.localPosition.x + 700, obj.transform.localPosition.y, obj.transform.localPosition.z);
			obj.transform.DOLocalMoveX(0, self.MoveTime);
			await UniTask.Delay((int)(self.ExistTime * 1000));
			ObjectPoolManager.Instance.ReturnToPool(obj);
			self.Items.Remove(obj);
		}

		#endregion
	}
}