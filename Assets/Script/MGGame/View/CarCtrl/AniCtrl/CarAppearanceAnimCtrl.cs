//====================================================
//Author:HDS
//Time  :2026/02/03 15:02:18
//Desc  :
//====================================================

using System.Collections.Generic;
using Sirenix.Utilities;
using Spine.Unity;
using UnityEngine;

namespace XN
{
    public class CarAppearanceAnimCtrl : MonoBehaviour
    {
        public SkeletonAnimation SkeletonAni;
        public HashSet<WheelRotation> Wheels = new();

        public CarAppearanceAnimCtrl Init()
        {
            SkeletonAni = GetComponent<SkeletonAnimation>();
            Wheels.AddRange(GetComponentsInChildren<WheelRotation>());
            return this;
        }

        public void SetAnimation(string animationName)
        {
            if (SkeletonAni != null)
            {
                SkeletonAni.AnimationName = animationName;
            }
            else
            {
                if (Wheels is not { Count: > 0 }) return;
                foreach (var wheelRotation in Wheels)
                {
                    wheelRotation.Rotating = !string.Equals(animationName, "Standby");
                }
            }
        }
    }
}