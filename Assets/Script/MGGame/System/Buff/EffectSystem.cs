namespace XN
{
    public static class EffectSystem
    {
        public static void OnCreateSystem(this EffectComponent self)
        {
            if (self.Target)
            {
                self.Transform = self.EffectCtrl.transform;
                self.Transform.position = self.Target.position + self.Offset;
            }
            
            self.EffectCtrl.Play(self.EffectId, self.EffectSkin);
        }

        public static void OnDestroySystem(this EffectComponent self)
        {
            self.EffectCtrl?.Stop();
            ObjectPoolManager.Instance.ReturnToPool(self.EffectCtrl?.gameObject, true);
        }

        [UpdateSystem]
        public static void Update(this EffectComponent self, float deltaTime)
        {
            if (!self.Target)
                return;
            
            self.Transform.position = self.Target.position + self.Offset;
        }
    }
}