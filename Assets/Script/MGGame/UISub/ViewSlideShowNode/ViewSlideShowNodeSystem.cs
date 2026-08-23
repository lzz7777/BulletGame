using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace XN
{
	public static class ViewSlideShowNodeSystem
	{
		#region CircleLife

		public static void OnOpenSystem(this ViewSlideShowNode self, UIWindowData uIWindowData = null)
		{
			(self.transform as RectTransform).anchoredPosition = uIWindowData.Pos;
			
			if (self.Icons.Count == 0)
			{
				self.Icons = new() { self.UIIcon1Image, self.UIIcon2Image };
			}
			
			self.Time = 0;
			self.IconIndex = 0;
			
			self.MaxNum = YooAssetManager.Instance.GetGroupTagFileNum("ImageFightSlideShow");

			self.InitCacheAsync().Forget();
		}

		private static async UniTaskVoid InitCacheAsync(this ViewSlideShowNode self)
		{
			if (self.MaxNum <= 0) return;

			if (self.CachedSprites == null || self.CachedSprites.Length != self.MaxNum + 1)
			{
				self.CachedSprites = new Sprite[self.MaxNum + 1];
				for (int i = 1; i <= self.MaxNum; i++)
				{
					self.CachedSprites[i] = await YooAssetManager.Instance.LoadSpriteAsync($"gundong_{i}");
				}
			}

			self.OnRefresh();
		}

		public static void OnCloseSystem(this ViewSlideShowNode self)
		{
		}

		public static void OnUpdateSystem(this ViewSlideShowNode self)
		{
			self.Time += Time.deltaTime;

			if (self.Time > 5)
			{
				self.Time = 0;

				float doTime = 0.5f;
				self.Icons[0].gameObject.transform.DOLocalMoveY(0, doTime);
				self.Icons[1].gameObject.transform.DOLocalMoveY(-125, doTime).OnComplete(() =>
				{
					self.Icons[1].gameObject.transform.localPosition = new Vector2(0, 125);
					(self.Icons[0], self.Icons[1]) = (self.Icons[1], self.Icons[0]);
					
					self.OnRefresh();
				});
			}
		}

		#endregion

		#region UIEvents

		#endregion

		#region GlobalEvents

		#endregion

		#region Logics

		public static void OnRefresh(this ViewSlideShowNode self)
		{
			if (self.CachedSprites == null || self.CachedSprites.Length <= self.MaxNum)
			{
				return;
			}

			self.IconIndex = self.IconIndex + 1 > self.MaxNum ? 1 : self.IconIndex + 1;
			int nextIconIndex = self.IconIndex + 1 > self.MaxNum ? 1 : self.IconIndex + 1;
			
			self.Icons[0].sprite = self.CachedSprites[nextIconIndex];
			self.Icons[1].sprite = self.CachedSprites[self.IconIndex];
		}
		
		#endregion
	}
}