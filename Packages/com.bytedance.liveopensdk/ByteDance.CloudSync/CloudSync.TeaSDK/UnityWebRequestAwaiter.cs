using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Networking;

namespace ByteDance.CloudSync.TeaSDK
{
    public struct UnityWebRequestAwaiter : INotifyCompletion
    {
        private readonly UnityWebRequestAsyncOperation _asyncOp;
        private Action _continuation;

        public UnityWebRequestAwaiter(UnityWebRequestAsyncOperation asyncOp)
        {
            _asyncOp = asyncOp;
            _continuation = null;
        }

        public bool IsCompleted => _asyncOp.isDone;

        public void GetResult()
        {
        }

        public void OnCompleted(Action continuation)
        {
            _continuation = continuation;
            _asyncOp.completed += OnRequestCompleted;
        }

        private void OnRequestCompleted(AsyncOperation obj)
        {
            _continuation?.Invoke();
        }
    }

    public static class ExtensionMethods
    {
        public static UnityWebRequestAwaiter GetAwaiter(this UnityWebRequestAsyncOperation asyncOp)
        {
            return new UnityWebRequestAwaiter(asyncOp);
        }
    }
}