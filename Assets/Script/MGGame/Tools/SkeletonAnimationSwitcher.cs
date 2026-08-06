using Spine.Unity;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace XN.Tools
{
    public class SkeletonAnimationSwitcher : MonoBehaviour
    {
        private SkeletonAnimation skeletonAnimation;
        // 初始动画名；如果为空，将使用 animationName 作为默认
        // [SerializeField] private string initialAnimation;
        // [SerializeField] private bool initialLoop = true;
        // 运行时可选择/切换的动画名（Odin 下拉枚举，改名即播放）
        [ValueDropdown(nameof(AnimOptions)), OnValueChanged(nameof(OnAnimationNameChanged))]
        [SerializeField] private string animationName;
        [SerializeField] private bool defaultLoop = true;

        private void Awake()
        {
            if (skeletonAnimation == null) skeletonAnimation = GetComponent<SkeletonAnimation>();
            if (skeletonAnimation == null) return;
            skeletonAnimation.Initialize(true);
            
            // 选择初始化播放的动画：优先 initialAnimation，否则使用 animationName
            // var initAnim = string.IsNullOrEmpty(initialAnimation) ? animationName : initialAnimation;
            // if (!string.IsNullOrEmpty(initAnim))
            // {
            //     var animator = skeletonAnimation.GetComponent<Animator>();
            //     if (animator != null) animator.enabled = false;
            //
            //     var state = skeletonAnimation.AnimationState;
            //     state.ClearTracks();
            //     state.SetAnimation(0, initAnim, string.IsNullOrEmpty(initialAnimation) ? defaultLoop : initialLoop);
            //     state.Update(0f);
            //     state.Apply(skeletonAnimation.Skeleton);
            //     skeletonAnimation.LateUpdate();
            // }
        }

        // Inspector 改名即播放，且取消前面混合/队列的影响
        private void OnAnimationNameChanged()
        {
            if (!string.IsNullOrEmpty(animationName))
            {
                PlayReset(animationName, defaultLoop);
            }
        }

        public void Play(string animationName, bool loop = true)
        {
            if (skeletonAnimation == null) return;
            if (!skeletonAnimation.valid) skeletonAnimation.Initialize(true);

            var animator = skeletonAnimation.GetComponent<Animator>();
            if (animator != null) animator.enabled = false;

            var state = skeletonAnimation.AnimationState;
            state.ClearTracks();
            state.SetAnimation(0, animationName, loop);
            state.Update(0f);
            state.Apply(skeletonAnimation.Skeleton);
            skeletonAnimation.LateUpdate();
        }

        public void Queue(string animationName, bool loop = true, float delay = 0f)
        {
            if (skeletonAnimation == null) return;
            // if (!skeletonAnimation.IsValid) skeletonAnimation.Initialize(true);

            var animator = skeletonAnimation.GetComponent<Animator>();
            if (animator != null) animator.enabled = false;

            var state = skeletonAnimation.AnimationState;
            state.AddAnimation(0, animationName, loop, delay);
            state.Update(0f);
            state.Apply(skeletonAnimation.Skeleton);
            skeletonAnimation.LateUpdate();
        }

        // 取消前置影响后强制当帧播放：清轨、临时无混合、复位为 SetupPose，避免残留变形
        public void PlayReset(string name, bool loop)
        {
            if (skeletonAnimation == null || string.IsNullOrEmpty(name)) return;
            if (!skeletonAnimation.valid) skeletonAnimation.Initialize(true);

            var animator = skeletonAnimation.GetComponent<Animator>();
            if (animator != null) animator.enabled = false;

            var state = skeletonAnimation.AnimationState;
            var skeleton = skeletonAnimation.Skeleton;

            float oldDefaultMix = state.Data.DefaultMix;
            state.Data.DefaultMix = 0f;

            state.ClearTracks();
            if (skeleton.Skin == null) skeleton.SetSkin(skeleton.Data.DefaultSkin);
            skeleton.SetToSetupPose();

            var entry = state.SetAnimation(0, name, loop);
            state.Update(0f);
            state.Apply(skeleton);
            skeletonAnimation.LateUpdate();

            state.Data.DefaultMix = oldDefaultMix;
        }

        public string GetCurrentAnimationName(int trackIndex = 0)
        {
            if (skeletonAnimation == null) return null;
            var entry = skeletonAnimation.AnimationState.GetCurrent(trackIndex);
            return entry?.Animation?.Name;
        }

        // Odin 下拉枚举数据源：返回当前资源的所有动画名
        private IEnumerable<string> AnimOptions()
        {
            if (skeletonAnimation == null) skeletonAnimation = GetComponent<SkeletonAnimation>();
            if (skeletonAnimation == null) return Enumerable.Empty<string>();
            if (!skeletonAnimation.valid) skeletonAnimation.Initialize(true);
            var anims = skeletonAnimation.Skeleton.Data.Animations;
            return anims.Select(a => a.Name);
        }

        private void OnValidate()
        {
            if (skeletonAnimation == null) skeletonAnimation = GetComponent<SkeletonAnimation>();
        }
    }
}