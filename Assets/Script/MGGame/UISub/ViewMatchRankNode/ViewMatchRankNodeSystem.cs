using System;
using System.Linq;
using UnityEngine;

namespace XN
{
    public static class ViewMatchRankNodeSystem
    {
        #region CircleLife

        public static void OnOpenSystem(this ViewMatchRankNode self, UIWindowData uIWindowData = null)
        {
            self.SetActiveScale(false);

            (self.transform as RectTransform).anchoredPosition = uIWindowData.Pos;

            EventsManager.AddListener(GameEnum.ViewMatchRankNodeRefreshEvent, self.OnSetDirty);
        }

        public static void OnCloseSystem(this ViewMatchRankNode self)
        {
            self.Datas.Clear();
            ObjectPoolManager.Instance.ReturnToPool(self.Objs);
            self.Objs.Clear();

            EventsManager.RemoveListener(GameEnum.ViewMatchRankNodeRefreshEvent, self.OnSetDirty);
        }

        public static void OnUpdateSystem(this ViewMatchRankNode self)
        {
            self.RefreshDt += Time.deltaTime;
            if (self.RefreshDt < 0.5f)
                return;
            
            self.RefreshDt = 0;
            
            if (!self.IsDirty)
                return;

            self.IsDirty = false;

            self.OnRefresh();
        }

        #endregion

        #region UIEvents

        #endregion

        #region GlobalEvents

        #endregion

        #region Logics

        public static void OnSetDirty(this ViewMatchRankNode self) => self.IsDirty = true;

        public static void OnRefresh(this ViewMatchRankNode self)
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

            self.OnRefreshPrefabs();
        }

        private static void OnRefreshPrefabs(this ViewMatchRankNode self)
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

            self.SetActiveScale(true);
            
            self.Datas = self.TempDatas;

            ObjectPoolManager.Instance.ReturnToPool(self.Objs);
            self.Objs.Clear();

            int maxNum = 0;

            self.ScrollViewLoopList.ClearData();
            
            for (int i = 0; i < self.Datas.Count; i++)
            {
                var data = self.Datas[i];
                maxNum = Math.Max(maxNum, data.PlayerIds.Count);

                self.ScrollViewLoopList.AddData(out ViewMatchRankItemData itemData);
                itemData.CarId = data.CarId;
                itemData.PlayerIds = data.PlayerIds;
                itemData.Rank = i + 1;
            }

            self.ScrollViewLoopList.RefreshItem(false);
            
            float x = 180 + 40 * maxNum;
            var tf = self.transform as RectTransform;
            tf.sizeDelta = new Vector2(x, tf.sizeDelta.y);
        }

        #endregion
    }
}