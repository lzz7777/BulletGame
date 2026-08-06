using UnityEngine;

namespace XN
{
    public static class DisposableEffectSystem
    {
        public static void OnDestroySystem(this DisposableEffectComponent self)
        {
            ObjectPoolManager.Instance.ReturnToPool(self.EffectCtrl?.gameObject);
        }

        [UpdateSystem]
        public static void Update(this DisposableEffectComponent self, float deltaTime)
        {
            var carViewComp = EntityManager.Instance.GetEntityById(self.CarId)?.GetComponent<CarViewComponent>();
            if (carViewComp == null)
                return;
            
            var effConf = TotalConfigManager.ConfigManager.EffectInfoConfigCategory.Get(self.EffectId, self.EffectSkin);
            if (!carViewComp.CarCtrl.effectPoints.TryGetValue(effConf.EffectPoint.ToString(), out Transform target))
                return;
            
            self.EffectCtrl.gameObject.transform.position = target.position + self.Offset;
        }
    }
}