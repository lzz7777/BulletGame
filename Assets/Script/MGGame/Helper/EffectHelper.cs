using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XN
{
    public static class EffectHelper
    {
        /// <summary>
        /// 获取特效
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="parentRoot"> null 不设置父节点 </param>
        /// <returns></returns>
        public static EffectCtrl GetEffect(string tag, Transform parentRoot = null)
        {
            var obj = ObjectPoolManager.Instance.GetFromPoolSync(tag, parentRoot,
                PrefabType.Effect);

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

        /// <summary>
        /// 获取特效
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="parentRoot"> null 不设置父节点 </param>
        /// <returns></returns>
        public static async UniTask<EffectCtrl> GetEffectAsync(string tag, Transform parentRoot = null)
        {
            var obj = await ObjectPoolManager.Instance.GetFromPool(tag, parentRoot,
                PrefabType.Effect);

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