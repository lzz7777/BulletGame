//====================================================
//Author:lixin
//Time  :2025/11/20 17:17
//Desc  :
//====================================================
using UnityEngine.EventSystems;
using UnityEngine;

namespace XN
{

    public class ClickBlocker : MonoBehaviour,
        IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
    {
        public void OnPointerClick(PointerEventData eventData) {}
        public void OnPointerDown(PointerEventData eventData) {}
        public void OnPointerUp(PointerEventData eventData) {}
    }
}