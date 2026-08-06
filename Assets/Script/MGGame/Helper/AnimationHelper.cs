using UnityEngine;

namespace XN
{
    public static class AnimationHelper
    {
        public static float GetAnimationLength(this Animation animation, string clipName)
        {
            // 获取指定名称的动画剪辑
            AnimationClip clip = animation.GetClip(clipName);
            if (clip == null) {
                Debug.LogError($"Animation clip '{clipName}' not found!");
                return 0f;
            }
    
            // 返回动画长度（单位：秒）
            return clip.length;
        }
    }
}