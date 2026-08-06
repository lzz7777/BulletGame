using DG.Tweening;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector;
#endif

namespace XN
{
    public class CarSpriteShake : MonoBehaviour
    {
        private Tweener tweener;

        public float duration = 0.2f;
        public float shakeStrength = 0.1f;
        public int vibrato = 10;
        public float randomness = 90f;
        public bool snapping = false;
        public bool fadeOut = true;

#if UNITY_EDITOR
        [Button]
        private void Reset()
        {
            tweener?.Kill();
            tweener = transform.DOShakePosition(duration, shakeStrength, vibrato, randomness, snapping, fadeOut).SetLoops(-1).Play();
        }
#endif

        private void Awake()
        {
            tweener = transform.DOShakePosition(duration, shakeStrength, vibrato, randomness, snapping, fadeOut).SetLoops(-1).Play();
        }

        private void OnEnable()
        {
            tweener?.Pause();
        }

        private void OnDisable()
        {
            tweener?.Play();
        }
    }
}