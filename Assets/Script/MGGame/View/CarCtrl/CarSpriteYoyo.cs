using DG.Tweening;
using UnityEngine;
#if UNITY_EDITOR
using Sirenix.OdinInspector;
#endif

namespace XN
{
    public class CarSpriteYoyo : MonoBehaviour
    {
        private Tweener tweener;

        public float Duation = 0.1f;

        public float OffsetY = 0.02f;

#if UNITY_EDITOR
        [Button]
        private void Reset()
        {
            tweener?.Kill();
            tweener = transform.DOLocalMoveY(transform.localPosition.y + OffsetY, Duation).SetLoops(-1, LoopType.Yoyo).Play();
        }
#endif

        private void Awake()
        {
            tweener = transform.DOLocalMoveY(transform.localPosition.y + OffsetY, Duation).SetLoops(-1, LoopType.Yoyo);
        }

        private void OnEnable()
        {
            tweener?.Play();
        }

        private void OnDisable()
        {
            tweener?.Pause();
        }
    }
}