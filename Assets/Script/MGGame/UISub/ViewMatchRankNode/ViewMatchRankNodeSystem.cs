using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XN
{
    public static class ViewMatchRankNodeSystem
    {
        #region CircleLife

        public static void OnOpenSystem(this ViewMatchRankNode self, UIWindowData uIWindowData = null)
        {
            self.gameObject.SetActive(false);

            (self.transform as RectTransform).anchoredPosition = uIWindowData.Pos;
            
            EventsManager.AddListener(GameEnum.ViewMatchRankNodeRefreshEvent, self.OnRefresh);
        }

        public static void OnCloseSystem(this ViewMatchRankNode self)
        {
            self.Datas.Clear();
            ObjectPoolManager.Instance.ReturnToPool(self.Objs);
            self.Objs.Clear();

            EventsManager.RemoveListener(GameEnum.ViewMatchRankNodeRefreshEvent, self.OnRefresh);
        }

        public static void OnRefresh(this ViewMatchRankNode self) => self.OnRefreshAsync();

        public static async UniTask OnRefreshAsync(this ViewMatchRankNode self)
        {
            self.TempDatas.Clear();
            foreach (var carId in RoomHelper.GetCars())
            {
                var carInfoComp = EntityManager.Instance.GetEntityById(carId).GetComponent<CarInfoComponent>();
                if (carInfoComp.PlayerIds.Count > 0)
                {
                    self.TempDatas.Add(new ViewMatchRankNodeData()
                    {
                        CarId = carId,
                        PlayerIds = carInfoComp.PlayerIds.GetRange(0, Math.Min(carInfoComp.PlayerIds.Count, 10)),
                    });
                }
            }

            await UniTask.Delay(300);
            self.OnRefreshPrefabs();
        }

        #endregion

        #region UIEvents

        #endregion

        #region GlobalEvents

        #endregion

        #region Logics

        private static async UniTask OnRefreshPrefabs(this ViewMatchRankNode self)
        {
            bool isRefresh = false;
            if (self.TempDatas.Count != self.Datas.Count)
            {
                isRefresh = true;
            }
            else
            {
                for (int i = 0; i < self.TempDatas.Count; i++)
                {
                    var tempData = self.TempDatas[i];
                    var data = self.Datas[i];
                    if (tempData.CarId != data.CarId)
                    {
                        isRefresh = true;
                        break;
                    }

                    if (!tempData.PlayerIds.SequenceEqual(data.PlayerIds))
                    {
                        isRefresh = true;
                        break;
                    }
                }
            }

            if (!isRefresh)
            {
                return;
            }

            self.gameObject.SetActive(true);

            self.Datas = self.TempDatas.ToList();

            ObjectPoolManager.Instance.ReturnToPool(self.Objs);
            self.Objs.Clear();

            int maxNum = 0;

            for (int i = 0; i < self.Datas.Count; i++)
            {
                var data = self.Datas[i];
                maxNum = Math.Max(maxNum, data.PlayerIds.Count);

                var obj = await ObjectPoolManager.Instance.GetFromPool<ViewMatchRankItem>(
                    self.UILayoutVerticalLayoutGroup.transform);
                self.Objs.Add(obj);
                var viewMatchRankItem = obj.GetComponent<ViewMatchRankItem>();
                await viewMatchRankItem.OnRefresh(new ViewMatchRankItemData()
                    { CarId = data.CarId, PlayerIds = data.PlayerIds, Rank = i + 1 });
            }

            float x = 180 + 40 * maxNum;
            float y = self.TempDatas.Count * 48 + 50;
            (self.transform as RectTransform).sizeDelta = new Vector2(x, y);
        }

        #endregion
    }
}