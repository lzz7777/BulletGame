using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XN
{
    public static class EffectHelper
    {
        public static async UniTask<EffectCtrl> GetEffect(string tag, Transform parentRoot,
            CancellationToken cancleToken = default)
        {
            try
            {
                // cancleToken.ThrowIfCancellationRequested();
                //
                // await UniTask.Delay(1, cancellationToken: cancleToken);

                var obj = await ObjectPoolManager.Instance.GetFromPool(tag, parentRoot, PrefabType.Effect);

                if (!obj)
                {
                    return null;
                }

                if (!obj.TryGetComponent<EffectCtrl>(out var effectCtrl))
                {
                    effectCtrl = obj.AddComponent<EffectCtrl>();
                    effectCtrl.InitData();
                }

                return effectCtrl;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning(e.Message);
                return null;
            }
        }

        /// <summary>
        /// 是否一次性特效
        /// </summary>
        /// <param name="effectId"></param>
        /// <param name="effectSkin"></param>
        /// <returns></returns>
        public static bool JudgeDisposableEffect(int effectId, int effectSkin)
        {
            var effConf = TotalConfigManager.ConfigManager.EffectInfoConfigCategory.Get(effectId, effectSkin);
            return effConf?.IsCarEffectDpShow == 1;
        }
    }
}