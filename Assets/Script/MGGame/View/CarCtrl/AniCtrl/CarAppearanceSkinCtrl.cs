//====================================================
//Author:HDS
//Time  :2026/02/03 15:02:20
//Desc  :
//====================================================

using Spine.Unity;
using UnityEngine;

namespace XN
{
    public class CarAppearanceSkinCtrl : MonoBehaviour
    {
        public SkeletonAnimation SkeletonAni;

        public CarAppearanceSkinCtrl Init()
        {
            SkeletonAni = GetComponent<SkeletonAnimation>();
            return this;
        }

        public void SetSkin(string skinName)
        {
            if (SkeletonAni == null) return;
            if (SkeletonAni.skeleton == null) return;
            SkeletonAni.skeleton.SetSkin(skinName);
        }
    }
}