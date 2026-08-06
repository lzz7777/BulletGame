using DG.Tweening;
using UnityEngine;

namespace XN
{
    public static class CameraHelper
    {
        /// <summary>
        /// 摄像头抖动
        /// </summary>
        public static void DoCameraShake(float duration = 0.2f, float strength = 0.1f, int vibrato = 10, float randomness = 70, ShakeRandomnessMode mode = ShakeRandomnessMode.Full, Ease ease = Ease.Linear, bool fadeOut = true)
        {
            var camera = Main.MainCamera ? Main.MainCamera : Camera.main;
            camera.DOKill(true);
            camera.DOShakePosition(duration, strength, vibrato, randomness, fadeOut, mode).SetEase(ease).SetAutoKill(true).OnComplete(() =>
            {
                if (camera == null) return;

                var transform = camera.transform;
                transform.position = new Vector3(0, 0, transform.position.z);
            });
        }
    }
}