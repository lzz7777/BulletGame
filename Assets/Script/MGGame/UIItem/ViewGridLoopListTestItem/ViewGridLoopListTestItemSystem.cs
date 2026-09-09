using System.Threading;
using Cysharp.Threading.Tasks;

namespace XN
{
    public static class ViewGridLoopListTestItemSystem
    {
        #region CircleLife

        public static void OnRefresh(this ViewGridLoopListTestItem self, ViewGridLoopListTestItemData data)
        {
            // 这里是单个格子的刷新逻辑
            if (self.NumText != null)
            {
                self.NumText.text = $"格子: {data.Index}";
            }

            // 如果有Icon逻辑可以继续在这里写
            if (self.IconImage != null)
            {
                // 每次刷新前，如果上次异步加载还没完成，直接取消掉
                if (self.Cts != null)
                {
                    self.Cts.Cancel();
                    self.Cts.Dispose();
                }
                self.Cts = new CancellationTokenSource();
                
                YooAssetManager.Instance.LoadSpriteAsync($"mrt_txk_{data.Index % 8 + 1}", self.IconImage, false, self.Cts.Token).Forget();
            }
        }

        public static void OnShowLoadingState(this ViewGridLoopListTestItem self)
        {
            self.NumText.text = "";

            self.IconImage.sprite = null;
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