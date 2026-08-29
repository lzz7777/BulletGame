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
        
        // --- 缓存字段，避免每帧产生 GC 与高昂的方法调用开销 ---
        public cfg.Fight.EffectInfoConfig CachedEffConf { get; set; }
        public string CachedEffectPointStr { get; set; }
        public Transform CachedTransform { get; set; }
        // --------------------------------------------------

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
            
            // 清理缓存
            CachedEffConf = null;
            CachedEffectPointStr = null;
            CachedTransform = null;
        }
    }
}