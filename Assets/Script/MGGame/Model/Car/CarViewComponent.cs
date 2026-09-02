using System.Collections.Generic;
using System.Threading;
using DG.Tweening;
using UnityEngine;

namespace XN
{
    public class EffectViewData
    {
        public int EffectSkin { get; set; }
        public long EffectEntityId { get; set; }
        // public EffectCtrl EffectCtrl { get; set; }
    }

    public class CarViewComponent : ComponentBase
    {
        [SerializeField]
        public GameObject Car { get; set; }

        [SerializeField]
        public ViewCarInfoItem ViewCarInfoItem { get; set; }

        [SerializeField]
        public CarCtrl CarCtrl { get; set; }

        /// <summary>
        /// 灯带特效
        /// </summary>
        [SerializeField]
        public EffectCtrl TrackLightEffect { get; set; }

        /// <summary>
        /// 当前载具id
        /// </summary>
        [SerializeField]
        public int CurDeviceId { get; set; }

        /// <summary>
        /// 特效组
        /// </summary>
        [SerializeField]
        public Dictionary<int, EffectViewData> EffectGroup { get; set; } = new();

        [SerializeField]
        public Sequence CarHitSequence { get; set; }

        /// <summary>
        /// 玩家称号标签
        /// </summary>
        [SerializeField]
        public ViewCarTitleItem ViewCarTitleItem { get; set; }
        
        public override void OnCreate()
        {
            this.OnCreateSystem();
        }

        public override void OnDestroy()
        {
            this.OnDestroySystem();
            
            Car = default;
            ViewCarInfoItem = default;
            CarCtrl = default;
            TrackLightEffect = default;
            CurDeviceId = default;
            EffectGroup.Clear();
            CarHitSequence = default;
            ViewCarTitleItem = default;
        }
    }
}