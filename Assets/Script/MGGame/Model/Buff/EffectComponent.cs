using Unity.Mathematics;
using UnityEngine;

namespace XN
{
    public class EffectComponent : ComponentBase
    {
        [SerializeField]
        public int EffectId { get; set; }
        [SerializeField]
        public int EffectSkin { get; set; }
        [SerializeField]
        public EffectCtrl EffectCtrl { get; set; }
        [SerializeField]
        public Vector3 Offset { get; set; }

        [SerializeField]
        public Transform Transform { get; set; }
        [SerializeField]
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
            Transform = default;
            Target = default;
        }
    }
}