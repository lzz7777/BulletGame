using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using UnityEngine;
using UnityEngine.Scripting;

namespace XN
{
    public static class AOTGenericWarmup
    {
        [Preserve]
        public static void Init()
        {
            TouchUniTask<GameObject>();
            TouchToCoroutine<Sprite>();
        }

        [Preserve]
        private static void TouchUniTask<T>()
        {
            var builder = AsyncUniTaskMethodBuilder<T>.Create();
            UniTask<T> task = default;
            _ = builder;
            _ = task;
        }

        [Preserve]
        private static void TouchToCoroutine<T>()
        {
            IEnumerator e = UniTaskExtensions.ToCoroutine<T>(default, null, null);
            _ = e;
        }
    }
}
