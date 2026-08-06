// Copyright (c) Bytedance. All rights reserved.
// Author: DONEY Dong
// Date: 2025/04/10
// Description:

using UnityEngine;

namespace ByteDance.CloudSync
{
    public class CloudSyncUtil
    {
        public static bool IsInActiveScene(GameObject current)
        {
            if (current == null)
                return false;
            var scene = current.scene;
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            return scene.IsValid() && activeScene.IsValid() && current.scene == activeScene;
        }

        public static string GetTransformPath(Transform current) {
            if (current == null)
                return "";
            if (current.parent == null)
                return current.name;
            return GetTransformPath(current.parent) + "/" + current.name;
        }
    }
}