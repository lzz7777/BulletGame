using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Object = UnityEngine.Object;

namespace Douyin.LiveOpenSDK.Samples
{
    [RequireComponent(typeof(Camera)) /*,ExecuteInEditMode*/]
    public class MixCameraTexture : MonoBehaviour
    {

        public RenderTexture baseRT;
        public RenderTexture targetRT;
        public RenderTexture uiRT;

        public ComputeShader combine;
        public ComputeShader flip;

        private ComputeShader _combineInstances;
        private ComputeShader _flipInstances;

        // public Material blendMaterial;  // 应用了Blending Shader的材质

        private RenderTexture _temp;
        private int _width, _height;
        private GraphicsFormat _graphicsFormat;

        private void Awake()
        {
            if (uiRT != null)
            {
                uiRT.enableRandomWrite = true;
            }
            if (targetRT != null)
            {
                targetRT.enableRandomWrite = true;
            }
        }

        private void OnEnable()
        {
            _width = baseRT.width;
            _height = baseRT.height;
            _graphicsFormat = baseRT.graphicsFormat;

            _temp = new RenderTexture(_width, _height, 0, _graphicsFormat)
            {
                enableRandomWrite = true
            };

            _combineInstances = Object.Instantiate(combine);
            _combineInstances.SetTexture(0, "backTex", baseRT);
            _combineInstances.SetTexture(0, "uiTex", uiRT);
            _combineInstances.SetTexture(0, "Result", _temp);


            _flipInstances = Object.Instantiate(flip);
            _flipInstances.SetTexture(0, "_originalTex", _temp);
            _flipInstances.SetTexture(0, "Result", targetRT);
            _flipInstances.SetFloat("_width", _width);
            _flipInstances.SetFloat("_height", _height);
            _flipInstances.SetBool("_flipY", true);

        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            //合并
            _combineInstances.Dispatch(0, _width, _height, 1);
            //上下翻转
            _flipInstances.Dispatch(0, _width, _height, 1);
            Graphics.Blit(source, destination);

        }

    }
}