//====================================================
//Author:HDS
//Time  :2026/02/03 15:02:25
//Desc  :
//====================================================

using System.Collections.Generic;
using Sirenix.Utilities;
using UnityEngine;

namespace XN
{
    public class CarAppearanceTintCtrl : MonoBehaviour
    {
        public HashSet<Animation> Anis = new();

        private const string TINT_ANI_NAME = "Car_TakeSeatItem";

        public CarAppearanceTintCtrl Init()
        {
            Anis.AddRange(GetComponentsInChildren<Animation>());
            return this;
        }

        public void Reset()
        {
            if (Anis is not { Count: > 0 }) return;
            Anis.ForEach(ani => { ani.gameObject.GetComponent<SpineTintController>().Reset(); });
        }

        public void Play()
        {
            if (Anis is not { Count: > 0 }) return;
            Anis.ForEach(ani => { ani.Play(TINT_ANI_NAME); });
        }
    }
}