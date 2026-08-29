using UnityEngine;

namespace XN
{
    public static class DisposableEffectSystem
    {
        public static void OnDestroySystem(this DisposableEffectComponent self)
        {
            self.EffectCtrl?.Stop();
            ObjectPoolManager.Instance.ReturnToPool(self.EffectCtrl?.gameObject, true);
        }

        [UpdateSystem]
        public static void Update(this DisposableEffectComponent self, float deltaTime)
        {
            var carViewComp = EntityManager.Instance.GetEntityById(self.CarId)?.GetComponent<CarViewComponent>();
            if (carViewComp == null)
                return;
            
            // 缓存查询到的特效配置
            if (self.CachedEffConf == null)
            {
                self.CachedEffConf = TotalConfigManager.ConfigManager.EffectInfoConfigCategory.Get(self.EffectId, self.EffectSkin);
                self.CachedEffectPointStr = self.CachedEffConf.EffectPoint.ToString();
            }

            if (!carViewComp.CarCtrl.effectPoints.TryGetValue(self.CachedEffectPointStr, out Transform target))
                return;
            
            // 缓存 GameObject 的 Transform，避免频繁调用 GetComponent 造成的开销
            if (self.CachedTransform == null && self.EffectCtrl != null)
            {
                self.CachedTransform = self.EffectCtrl.transform;
            }

            if (self.CachedTransform != null)
            {
                self.CachedTransform.position = target.position + self.Offset;
            }
        }
    }
}