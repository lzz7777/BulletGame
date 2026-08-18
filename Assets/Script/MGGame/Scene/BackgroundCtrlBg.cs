using System.Collections.Generic;
using cfg;
using DG.Tweening;
using UnityEngine;

namespace XN
{
    public class BackgroundCtrlBg : BackgroundCtrlBase
    {
        private List<Transform> _goList = new();
        public SceneLayerType sceneLayerType;

        private LayerInfo _layerInfo;

        private void Awake()
        {
            foreach (Transform child in transform)
            {
                _goList.Add(child);
            }
        }

        public override void Init()
        {
            UpdateSceneData();

            if (_layerInfo == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            var curSpriteRand = _goList[0].GetComponent<SpriteRenderer>();
            curSpriteRand.color = Color.white;
            string str = SceneHelper.GetLayerInfoRandomResName(_layerInfo);
            YooAssetManager.Instance.LoadSpriteAsync("Scene",str, curSpriteRand);
            
            var nextSpriteRand = _goList[1].GetComponent<SpriteRenderer>();
            nextSpriteRand.color = new Color(1, 1, 1, 0);
        }

        /// <summary>
        /// 更新场景
        /// </summary>
        public override void UpdateScene()
        {
            UpdateSceneData();

            float fadeDuration = 0.5f;
            
            //上个背景透明的1->0
            var curSpriteRand = _goList[0].GetComponent<SpriteRenderer>();
            curSpriteRand.DOFade(0, fadeDuration).SetEase(Ease.OutQuad);

            //下个背景透明的0->1
            var nextSpriteRand = _goList[1].GetComponent<SpriteRenderer>();
            string str = SceneHelper.GetLayerInfoRandomResName(_layerInfo);
            YooAssetManager.Instance.LoadSpriteAsync("Scene",str, nextSpriteRand);
            nextSpriteRand.DOFade(1, fadeDuration).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                (_goList[0], _goList[1]) = (_goList[1], _goList[0]);
            });
        }

        /// <summary>
        /// 更新场景数据
        /// </summary>
        public void UpdateSceneData()
        {
            _layerInfo = SceneHelper.GetLayerInfo(sceneLayerType);
        }
    }
}