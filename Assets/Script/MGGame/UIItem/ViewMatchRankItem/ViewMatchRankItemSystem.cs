using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XN
{
    public static class ViewMatchRankItemSystem
    {
        #region CircleLife

        public static async UniTask OnRefresh(this ViewMatchRankItem self, ViewMatchRankItemData data)
        {
            var carInfoComp = EntityManager.Instance.GetEntityById(data.CarId).GetComponent<CarInfoComponent>();

            self.UITmpRankIndexTextMeshProUGUI.text = data.Rank.ToString();
            self.UITmpNameText.text = carInfoComp.Name;

            ObjectPoolManager.Instance.ReturnToPool(self.Objs);
            self.Objs.Clear();

            foreach (var playerId in data.PlayerIds)
            {
                var obj = await ObjectPoolManager.Instance.GetFromPool<ViewHeadItem>(self.UILayoutHorizontalLayoutGroup
                    .transform);
                self.Objs.Add(obj);
                var viewHeadItem = obj.GetComponent<ViewHeadItem>();
                await viewHeadItem.OnRefresh(new ViewHeadItemData()
                    { PlayerId = playerId, SizeData = new(76 * 0.5f, 76 * 0.5f) });
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