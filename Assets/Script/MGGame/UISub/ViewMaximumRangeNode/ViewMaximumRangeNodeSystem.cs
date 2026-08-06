using System;
using UnityEngine;

namespace XN
{
    public static class ViewMaximumRangeNodeSystem
    {
        #region CircleLife

        public static void OnOpenSystem(this ViewMaximumRangeNode self, UIWindowData uIWindowData = null)
        {
            self.gameObject.SetActive(false);

            (self.transform as RectTransform).anchoredPosition = uIWindowData.Pos;
            
            EventsManager.AddListener(GameEnum.ViewMaximumRangeNodeShowEvent, self.Show);
        }

        public static void OnCloseSystem(this ViewMaximumRangeNode self)
        {
            self.gameObject.SetActive(false);

            EventsManager.RemoveListener(GameEnum.ViewMaximumRangeNodeShowEvent, self.Show);
        }

        public static void OnUpdateSystem(this ViewMaximumRangeNode self)
        {
            int distance = (int)(RoomHelper.GetFightRoomConfig().MaximumRange - EntityManager.Instance
                .GetEntityById(RoomHelper.GetCars()[0]).GetComponent<CarInfoComponent>().Mileage);
            distance = Math.Max(0, distance);
            self.UIMaximumRangeTMP_UGUI.text = $"{distance}米";
        }

        #endregion

        #region UIEvents

        #endregion

        #region GlobalEvents

        #endregion

        #region Logics

        private static void Show(this ViewMaximumRangeNode self)
        {
            self.gameObject.SetActive(true);
        }

        #endregion
    }
}