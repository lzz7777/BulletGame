using UnityEngine;

namespace ByteDance.CloudSync.TextureProvider
{
    public abstract class ScreenTextureProvider : MonoBehaviour
    {
        public abstract Texture GetTexture(IVirtualScreen screen);
    }
}