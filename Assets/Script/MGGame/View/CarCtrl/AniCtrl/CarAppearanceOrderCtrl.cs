//====================================================
//Author:HDS
//Time  :2026/02/03 15:02:57
//Desc  :
//====================================================

using System.Collections.Generic;
using UnityEngine;

namespace XN
{
    public class CarAppearanceOrderCtrl : MonoBehaviour
    {
        public Dictionary<Renderer, int> orderDic = new();

        public CarAppearanceOrderCtrl Init()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers is { Length: > 0 })
            {
                foreach (var renderer in renderers)
                {
                    // Material tempMat = new Material(renderer.material);
                    // renderer.material = tempMat;
                    orderDic.Add(renderer, renderer.sortingOrder);
                }
            }

            return this;
        }

        public void RefreshLayerOrder(int order)
        {
            if (orderDic is not { Count: > 0 }) return;

            foreach (var (r, initOrder) in orderDic)
            {
                r.sortingOrder = order + initOrder;
            }
        }
    }
}