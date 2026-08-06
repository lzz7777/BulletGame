using System;
using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;

namespace XN
{
    public class CarCtrl : MonoBehaviour
    {
        public Dictionary<string, Transform> effectPoints = new();

        public CarAppearanceTintCtrl tintCtrl;
        public CarAppearanceSkinCtrl skinCtrl;
        public CarAppearanceOrderCtrl orderCtrl;
        public CarAppearanceAnimCtrl animCtrl;
        
        public void InitData(string resName)
        {
            var parentTrans = transform.Find(resName);
            var praentGo = parentTrans.gameObject;
            tintCtrl = praentGo.AddComponent<CarAppearanceTintCtrl>().Init();
            skinCtrl = praentGo.AddComponent<CarAppearanceSkinCtrl>().Init();
            orderCtrl = transform.gameObject.AddComponent<CarAppearanceOrderCtrl>().Init();
            animCtrl = praentGo.AddComponent<CarAppearanceAnimCtrl>().Init();

            foreach (Transform tf in transform)
            {
                effectPoints[tf.name] = tf;
            }
        }
        
        public void Reset()
        {
            tintCtrl.Reset();
        }
    }
}