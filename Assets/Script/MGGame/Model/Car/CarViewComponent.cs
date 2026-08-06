using System.Collections.Generic;
using System.Threading;
using DG.Tweening;
using UnityEngine;

namespace XN
{
    public class EffectViewData
    {
        public int EffectSkin { get; set; }
        public EffectCtrl EffectCtrl { get; set; }
    }

    public class CarViewComponent : IComponent
    {
        public GameObject Car { get; set; }

        public ViewCarInfoItem ViewCarInfoItem { get; set; }

        public CarCtrl CarCtrl { get; set; }

        /// <summary>
        /// 灯带特效
        /// </summary>
        public EffectCtrl TrackLightEffect { get; set; }

        /// <summary>
        /// 当前载具id
        /// </summary>
        public int CurDeviceId { get; set; }

        /// <summary>
        /// 特效组
        /// </summary>
        public Dictionary<int, EffectViewData> EffectGroup { get; set; } = new();

        public Sequence CarHitSequence { get; set; }

        /// <summary>
        /// 玩家称号标签
        /// </summary>
        public ViewCarTitleItem ViewCarTitleItem { get; set; }
        
        public override void OnCreate()
        {
            this.OnCreateSystem();
        }

        public override void OnDestroy()
        {
            this.OnDestroySystem();
        }
    }
}