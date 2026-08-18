using System.Collections.Generic;
using cfg;
using UnityEngine;

namespace XN
{
    public enum SceneLayerType
    {
        Bg,
        Layer0,
        Layer1,
        Layer2,
        Layer3,
        Layer4,
        Layer5,
        Layer6,
        RoadLine1,
        RoadLine2,
        RoadLine3,
    }

    public class BackgroundCtrl : BackgroundCtrlBase
    {
        private List<Transform> _goList = new();
        public SceneLayerType sceneLayerType;

        private float _speed = 0;
        private float _outposX = 0;
        private float _interval = 0;
        private LayerInfo _layerInfo;

        //用在树木，开始前用清晰的树，跑起来用模糊的树
        private bool _isBlurry;

        private void Awake()
        {
            float scenePos = -GameConst.ScreenWidth * 1.0f / 100 / 2;
            float longest = 0;

            foreach (Transform child in transform)
            {
                _goList.Add(child);

                if (child.GetComponent<SpriteRenderer>().bounds.size.x > longest)
                {
                    longest = child.GetComponent<SpriteRenderer>().bounds.size.x;
                }
            }

            _outposX = scenePos - longest / 2;
        }

        private void Update()
        {
            if (!GameStateCtrl.IsGaming || _speed == 0)
            {
                return;
            }

            foreach (var tf in _goList)
            {
                if (tf.position.x < _outposX)
                {
                    //设置x
                    var lastChildTf = transform.GetChild(transform.childCount - 1);
                    var spriteRand = tf.GetComponent<SpriteRenderer>();
                    var width = lastChildTf.GetComponent<SpriteRenderer>().bounds.size.x / 2 +
                                spriteRand.bounds.size.x / 2;
                    tf.position = new Vector3(lastChildTf.position.x + width + _interval, lastChildTf.position.y,
                        lastChildTf.position.z);

                    string str = SceneHelper.GetLayerInfoRandomResName(_layerInfo);

                    if (_isBlurry)
                    {
                        str += "B";
                    }

                    YooAssetManager.Instance.LoadSpriteAsync("Scene",str, spriteRand);

                    //位置放最后一个
                    tf.SetAsLastSibling();
                }

                tf.position += Vector3.left * (_speed * Time.deltaTime);
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

            foreach (var tf in _goList)
            {
                var spriteRand = tf.GetComponent<SpriteRenderer>();
                string str = SceneHelper.GetLayerInfoRandomResName(_layerInfo);

                if (_isBlurry)
                {
                    str += "A";
                }

                YooAssetManager.Instance.LoadSpriteAsync("Scene",str, spriteRand);
            }
        }

        /// <summary>
        /// 更新场景
        /// </summary>
        public override void UpdateScene()
        {
            UpdateSceneData();
        }

        /// <summary>
        /// 更新场景数据
        /// </summary>
        public void UpdateSceneData()
        {
            _speed = 0;
            _layerInfo = SceneHelper.GetLayerInfo(sceneLayerType);

            if (_layerInfo == null)
            {
                return;
            }

            _speed = _layerInfo.Speed;
            _isBlurry = _layerInfo.IsBlur;
        }
    }
}