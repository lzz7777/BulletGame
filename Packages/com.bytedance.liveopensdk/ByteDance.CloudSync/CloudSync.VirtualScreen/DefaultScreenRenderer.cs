using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace ByteDance.CloudSync
{
    /// <summary>
    /// RenderTexture 混合器
    /// </summary>
    public class DefaultScreenRenderer : IScreenRenderer, IScreenTextureCollectionObserver
    {
        private static readonly int IdBaseTex = Shader.PropertyToID("_BaseTex");
        private static readonly int IdTex1 = Shader.PropertyToID("_Tex1");
        private static readonly int IdTex2 = Shader.PropertyToID("_Tex2");
        private static readonly int IdTex3 = Shader.PropertyToID("_Tex3");
        private static readonly int IdTexCount1 = Shader.PropertyToID("_TexCount1");
        private static readonly int IdTexCount2 = Shader.PropertyToID("_TexCount2");
        private static readonly int IdTexCount3 = Shader.PropertyToID("_TexCount3");
        private static readonly int IdGammaCorrect = Shader.PropertyToID("_GammaCorrect");

        private Material _composureMat;

        private RenderTexture _targetRT;

        private TextureAndHandle _textureAndHandles;

        private Texture _baseTexture;
        private Texture _texture1;
        private Texture _texture2;
        private Texture _texture3;

        private bool _ready;

        private IVirtualScreen _screen;

        private readonly IScreenTextureCollection _collection;

        public DefaultScreenRenderer(IScreenTextureCollection textureCollection)
        {
            if (textureCollection == null)
                CGLogger.LogError("ScreenTextureCollection is null!");
            _collection = textureCollection;
            _collection.SetObserver(this);
        }

        public void Init(IVirtualScreen screen)
        {
            CGLogger.Log("RenderTexture OnInitialize");
            _screen = screen;

            var helper = Resources.Load<RenderTextureHelper>("RenderTextureHelper");
            _composureMat = new Material(helper.composureShader);
            CGLogger.Log("RenderTexture OnInitialize Finished");
        }


        public void OnEnable()
        {
            if (_ready)
                return;
            CGLogger.Log("RenderTexture OnEnable");

            var width = _screen.Resolution.x;
            var height = _screen.Resolution.y;

            _textureAndHandles = CreateShareRenderTexture(width, height);
            _textureAndHandles.texture.name = $"ShareRenderTexture:{_screen.Index}";

            UpdateTextures();

            if (Application.platform != RuntimePlatform.Android)
            {
                _targetRT = new RenderTexture(width, height, 0, _textureAndHandles.format);
                _composureMat.EnableKeyword("FlipY");
            }
            _ready = true;
            CGLogger.Log("RenderTexture OnEnable Finished");
        }

        private void UpdateTextures()
        {
            _baseTexture = _collection.Get(ScreenTextureType.Base, _screen);
            _texture1 = _collection.Get(ScreenTextureType.Tex1, _screen);
            _texture2 = _collection.Get(ScreenTextureType.Tex2, _screen);
            _texture3 = _collection.Get(ScreenTextureType.Tex3, _screen);

            _composureMat.SetTexture(IdBaseTex, _baseTexture);
            _composureMat.SetTexture(IdTex1, _texture1);
            _composureMat.SetTexture(IdTex2, _texture2);
            _composureMat.SetTexture(IdTex3, _texture3);
            _composureMat.SetInteger(IdTexCount1, _texture1 != null ? 1 : 0);
            _composureMat.SetInteger(IdTexCount2, _texture2 != null ? 1 : 0);
            _composureMat.SetInteger(IdTexCount3, _texture3 != null ? 1 : 0);
        }

        public bool IsReady => _ready;

        public void SetMode(VideoStreamMode mode)
        {
        }

        public TextureAndHandle Frame => _textureAndHandles;

        public TextureAndHandle Render()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                Graphics.Blit(null, _textureAndHandles.texture as RenderTexture, _composureMat);
            }
            else
            {
                Graphics.Blit(null, _targetRT, _composureMat);

                //将内容拷贝到共享纹理上
                Graphics.CopyTexture(_targetRT, _textureAndHandles.texture);
            }
            return _textureAndHandles;
        }

        private TextureAndHandle CreateShareRenderTexture(int width, int height)
        {
            IShareRenderTexture shareRenderTexture;
            if (Application.platform == RuntimePlatform.Android)
            {
                shareRenderTexture = new AndroidShareRenderTexture();
            }
            else
            {
                shareRenderTexture = new PCShareRenderTexture();
            }
            return shareRenderTexture.Create(width, height);
        }

        public void OnCollectionChanged()
        {
            UpdateTextures();
        }
    }
}