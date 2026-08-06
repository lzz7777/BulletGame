using UnityEngine;

namespace ByteDance.CloudSync.TextureProvider
{
    [DllMonoBehaviour]
    public class ScreenTextureProviderCollection : MonoBehaviour, IScreenTextureCollection
    {
        [SerializeField]
        private ScreenTextureProvider textureProvider3;

        [SerializeField]
        private ScreenTextureProvider textureProvider2;

        [SerializeField]
        private ScreenTextureProvider textureProvider1;

        [SerializeField]
        private ScreenTextureProvider textureProviderBase;

        private IScreenTextureCollectionObserver _observer;

        public void Set(ScreenTextureType type, ScreenTextureProvider provider)
        {
            switch (type)
            {
                case ScreenTextureType.Base:
                    textureProviderBase = provider;
                    break;
                case ScreenTextureType.Tex1:
                    textureProvider1 = provider;
                    break;
                case ScreenTextureType.Tex2:
                    textureProvider2 = provider;
                    break;
                case ScreenTextureType.Tex3:
                    textureProvider3 = provider;
                    break;
            }
            _observer?.OnCollectionChanged();
        }

        public Texture Get(ScreenTextureType type, IVirtualScreen screen)
        {
            switch (type)
            {
                case ScreenTextureType.Base:
                    return textureProviderBase?.GetTexture(screen);
                case ScreenTextureType.Tex1:
                    return textureProvider1?.GetTexture(screen);
                case ScreenTextureType.Tex2:
                    return textureProvider2?.GetTexture(screen);
                case ScreenTextureType.Tex3:
                    return textureProvider3?.GetTexture(screen);
            }

            return null;
        }

        // Editor-only function that Unity calls when the script is loaded or a value changes in the Inspector.
        private void OnValidate()
        {
            _observer?.OnCollectionChanged();
        }

        public void SetObserver(IScreenTextureCollectionObserver observer)
        {
            _observer = observer;
        }
    }
}