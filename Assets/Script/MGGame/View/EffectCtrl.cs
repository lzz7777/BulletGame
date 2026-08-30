using System;
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
            
            Stop();
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
                skelAnim.enabled = true;
                
                int animCount = skelAnim.skeleton.Data.Animations.Count;
                if (animCount <= 1)
                {
                    skelAnim.Skeleton.SetSkin(effConf.SpineSkin);
                }

                // 播放 Birth 动画
                skelAnim.AnimationState.SetAnimation(0, "Birth", false);
                
                if (animCount > 1)
                {
                    // 约定有两个动画: Birth, Loop。利用 Spine 原生 AddAnimation 队列功能
                    // 替代手写的 Complete 委托回调，从根本上解决可能产生的闭包内存泄漏
                    skelAnim.AnimationState.AddAnimation(0, "Loop", true, 0f);
                }
                
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

        public void Stop()
        {
            foreach (var skelAnim in this.SkeletonAnimOrderDic.Keys)
            {
                // 1. 清空所有动画轨道，停止播放
                skelAnim.AnimationState?.ClearTracks();
                
                // 2. 恢复到初始姿势
                skelAnim.skeleton?.SetToSetupPose();
                
                // 3. 禁用组件，避免在对象池中持续执行 Update/LateUpdate 消耗 CPU
                skelAnim.enabled = false;
            }
            
            foreach (var part in this.ParticleOrderDic.Keys)
            {
                part.Stop();
                part.Clear(); // 清除已发射的残留粒子
            }
        }
    }
}