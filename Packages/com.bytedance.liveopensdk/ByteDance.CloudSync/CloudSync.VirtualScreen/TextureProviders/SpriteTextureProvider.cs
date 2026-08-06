using UnityEngine;

namespace ByteDance.CloudSync.TextureProvider
{
    [DllMonoBehaviour]
    public class SpriteTextureProvider : ScreenTextureProvider
    {
        public Sprite sprite;
        private Texture2D _texture;

        public override Texture GetTexture(IVirtualScreen screen)
        {
            if (sprite == null)
                return null;

            return sprite.texture;
        }
    }
}