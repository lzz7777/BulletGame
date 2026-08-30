using Unity.Mathematics;
using UnityEngine;

namespace XN
{
    public class EffectComponent : IComponent
    {
        public int EffectId { get; set; }
        public int EffectSkin { get; set; }
        public EffectCtrl EffectCtrl { get; set; }
        public Vector3 Offset { get; set; }

        public Transform Transform { get; set; }
        public Transform Target { get; set; }
        
        public override void OnCreate()
        {
            this.OnCreateSystem();
        }

        public override void OnDestroy()
        {
            this.OnDestroySystem();
            
            EffectId = default;
            EffectSkin = default;
            EffectCtrl = default;
            Offset = default;
            Transform = null;
        }
    }
}