using DG.Tweening;
using UnityEngine;

namespace XN
{
    public static class CameraHelper
    {
        // 记录上一帧震屏的帧号，防止同帧多次调用造成严重性能开销
        private static int _lastShakeFrame = -1;

        /// <summary>
        /// 摄像头抖动
        /// </summary>
        public static void DoCameraShake(float duration = 0.2f, float strength = 0.1f, int vibrato = 10, float randomness = 70, ShakeRandomnessMode mode = ShakeRandomnessMode.Full, Ease ease = Ease.Linear, bool fadeOut = true)
        {
            // 压测优化：同一帧内如果触发了成百上千次震屏，只执行第一次，后续直接抛弃，防止创建海量 Tween 对象
            if (Time.frameCount == _lastShakeFrame) return;
            _lastShakeFrame = Time.frameCount;

            var camera = Main.MainCamera ? Main.MainCamera : Camera.main;
            if (camera == null) return;

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