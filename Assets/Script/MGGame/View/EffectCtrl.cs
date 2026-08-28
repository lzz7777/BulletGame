using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace XN
{
    public class EffectCtrl : MonoBehaviour
    {
        public Dictionary<SkeletonAnimation, int> SkeletonAnimOrderDic = new();
        public Dictionary<ParticleSystem, int> ParticleOrderDic = new();
        
        public void InitData()
        {
            foreach (SkeletonAnimation skelAnim in transform.GetComponentsInChildren<SkeletonAnimation>())
            {
                var mr = skelAnim.GetComponent<MeshRenderer>();
                SkeletonAnimOrderDic[skelAnim] = mr.sortingOrder;
                
                // Material newMat = new Material(mr.material);
                // mr.material = newMat;
            }
            
            foreach (ParticleSystem partSystem in transform.GetComponentsInChildren<ParticleSystem>())
            {
                var psr = partSystem.GetComponent<ParticleSystemRenderer>();
                ParticleOrderDic[partSystem] = psr.sortingOrder;
            }
        }

        public void RefreshLayerOrder(int order)
        {
            foreach (var (skel, skelOrder) in SkeletonAnimOrderDic)
            {
                skel.GetComponent<MeshRenderer>().sortingOrder = order + skelOrder;
            }

            foreach (var (part, partOrder) in ParticleOrderDic)
            {
                part.GetComponent<ParticleSystemRenderer>().sortingOrder = order + partOrder;
            }
        }

        public void Play(int effectId, int effectSkin)
        {
            var effConf = TotalConfigManager.ConfigManager.EffectInfoConfigCategory.Get(effectId, effectSkin);
            
            float scaleSize = effConf.Size / 10000.0f;
            transform.localScale = Vector3.one * scaleSize;
            
            foreach (var skelAnim in this.SkeletonAnimOrderDic.Keys)
            {
                int animCount = skelAnim.skeleton.Data.Animations.Count;
                if (animCount > 1)
                {
                    // 使用局部变量保存委托实例，并利用闭包特性在回调中实现一次性解绑
                    Spine.AnimationState.TrackEntryDelegate onComplete = null;
                    onComplete = (trackEntry) =>
                    {
                        Debug.Log(
                            $"OnAnimationComplete: effectId: {effectId} effectSkin: {effectSkin} Name:{trackEntry.Animation.Name}");
                        
                        if (trackEntry.Animation.Name == "Birth")
                        {
                            skelAnim.AnimationState.SetAnimation(0, "Loop", true);
                            // 触发后解绑自己，防止后续重复调用或内存泄漏
                            skelAnim.AnimationState.Complete -= onComplete;
                        }
                    };

                    //约定有两个动画,Birth, Loop
                    skelAnim.AnimationState.Complete += onComplete;
                }
                else
                {
                    skelAnim.Skeleton.SetSkin(effConf.SpineSkin);
                }

                skelAnim.AnimationState.SetAnimation(0, "Birth", false);
                
                // 强制刷新动画和网格，防止生成时一帧的闪烁或处于SetupPose
                skelAnim.skeleton.SetToSetupPose();
                skelAnim.Update(0);
                skelAnim.LateUpdate();
            }

            foreach (var part in this.ParticleOrderDic.Keys)
            {
                part.Play();
            }
        }
    }
}