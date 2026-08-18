using System;
using UnityEngine.U2D;

namespace XN.AOT
{
    /// <summary>
    /// 专门用于解决 HybridCLR 热更层调用 Unity 原生事件（如 atlasRequested）
    /// 时可能出现的 MethodNotFind 桥接失败问题。
    /// 将事件的注册转移到 AOT 层执行。
    /// </summary>
    public static class AtlasEventWrapper
    {
        public static void AddListener(Action<string, Action<SpriteAtlas>> listener)
        {
            SpriteAtlasManager.atlasRequested += listener;
        }

        public static void RemoveListener(Action<string, Action<SpriteAtlas>> listener)
        {
            SpriteAtlasManager.atlasRequested -= listener;
        }

        // 提供一个普通方法，让 AOT 可以轻松调用，防止整个类被剥离
        public static void Preserve()
        {
        }
    }
}