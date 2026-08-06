using System;
using UnityEngine;

namespace ByteDance.CloudSync
{
    public class MonitoredDisposable : IDisposable
    {
        public Action OnDispose;

        public bool Disposed { get; private set; }

        public bool CanCallbackThrow { get; set; }

        public MonitoredDisposable(Action actionOnDispose = null)
        {
            OnDispose = actionOnDispose;
        }

        public void Dispose()
        {
            Disposed = true;
            InvokeCallback(OnDispose);
            OnDispose = null;
        }

        private void InvokeCallback(Action action)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (CanCallbackThrow)
                    throw;
            }
        }
    }
}