using Unity.Mathematics;
using UnityEngine;

namespace XN
{
    public class DisposableEffectComponent : IComponent
    {
        public long CarId { get; set; }
        public int EffectId { get; set; }
        public int EffectSkin { get; set; }
        public EffectCtrl EffectCtrl { get; set; }
        public Vector3 Offset { get; set; }
        
        public override void OnCreate()
        {
        }

        public override void OnDestroy()
        {
            this.OnDestroySystem();
            
            CarId = default;
            EffectId = default;
            EffectSkin = default;
            EffectCtrl = default;
            Offset = default;
        }
    }
}