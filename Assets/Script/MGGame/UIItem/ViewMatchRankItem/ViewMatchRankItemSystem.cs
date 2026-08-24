using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XN
{
    public static class ViewMatchRankItemSystem
    {
        #region CircleLife

        public static async UniTask OnAwakeSystem(this ViewMatchRankItem self)
        {
            self.Objs = await ObjectPoolManager.Instance.GetFromPool<ViewHeadItem>(10, self.UILayoutRectTrans
                .transform);
        }

        public static void OnRefresh(this ViewMatchRankItem self, ViewMatchRankItemData data)
        {
            var carInfoComp = EntityManager.Instance.GetEntityById(data.CarId).GetComponent<CarInfoComponent>();

            self.UITmpRankIndexTMP_UGUI.text = data.Rank.ToString();
            self.UITmpNameText.text = carInfoComp.Name;

            for (int i = 0; i < data.PlayerIds.Count; i++)
            {
                var playerId = data.PlayerIds[i];

                var obj = self.Objs[i];
                obj.transform.localScale = Vector3.one;
                var viewHeadItem = obj.GetComponent<ViewHeadItem>();
                viewHeadItem.OnRefresh(new ViewHeadItemData()
                    { PlayerId = playerId, SizeData = new(76 * 0.5f, 76 * 0.5f) });
            }

            for (int i = data.PlayerIds.Count; i < 10; i++)
            {
                var obj = self.Objs[i];
                obj.transform.localScale = Vector3.zero;
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