// Copyright (c) Bytedance. All rights reserved.
// Author: DONEY Dong
// Date: 2025/04/08
// Description:

using UnityEditor;
using UnityEngine;

namespace Douyin.LiveOpenSDK.Editor
{
    public static class SdkEditorGUI
    {
        public static GUIIcons Icons => _icons ??= new GUIIcons();
        private static GUIIcons _icons;

        public class GUIIcons
        {
            public GUIIcons()
            {
                CheckInitRes();
            }

            public bool HasInitRes { get; private set; }
            public Texture2D Refresh { get; private set; }
            public Texture2D SaveActive { get; private set; }
            public Texture2D WarnIcon { get; private set; }

            public void CheckInitRes()
            {
                if (HasInitRes && Refresh != null)
                    return;
                Refresh = FindTexturePro("Refresh");
                SaveActive = FindTexture("SaveActive");
                WarnIcon = FindTexture("console.warnicon");
                HasInitRes = true;
            }

            internal static Texture2D FindTexturePro(string name, string darkPrefix = "d_")
            {
                var isPro = EditorGUIUtility.isProSkin;
                var path = (isPro ? darkPrefix : "") + name;
                var tex2d = EditorGUIUtility.FindTexture(path);
                return tex2d;
            }

            internal static Texture2D FindTexture(string name) => FindTexturePro(name, "");
        }
    }
}