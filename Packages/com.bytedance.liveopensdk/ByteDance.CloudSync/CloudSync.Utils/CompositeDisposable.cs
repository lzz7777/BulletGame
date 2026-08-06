using System;
using System.Collections.Generic;
using UnityEngine;

namespace ByteDance.CloudSync
{
    public class CompositeDisposable : IDisposable
    {
        public Action<CompositeDisposable> OnDisposing;
        public Action<CompositeDisposable> OnDisposed;
        public bool Disposing { get; private set; }
        public bool Disposed { get; private set; }

        public bool CanDisposeElementThrow { get; set; }
        public bool CanCallbackThrow { get; set; }

        private readonly List<IDisposable> _list = new();

        public void Dispose()
        {
            Disposing = true;
            InvokeCallback(OnDisposing);
            OnDisposing = null;

            DisposeElements(_list.ToArray());
            _list.Clear();

            Disposing = false;
            Disposed = true;
            InvokeCallback(OnDisposed);
            OnDisposed = null;
        }

        private void DisposeElements(IDisposable[] arr)
        {
            foreach (var item in arr)
            {
                DisposeElement(item);
            }
        }

        private void DisposeElement(IDisposable item)
        {
            try
            {
                item.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (CanDisposeElementThrow)
                    throw;
            }
        }

        private void InvokeCallback(Action<CompositeDisposable> action)
        {
            try
            {
                action?.Invoke(this);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (CanCallbackThrow)
                    throw;
            }
        }

        public int Count => _list.Count;

        public void Add(IDisposable value)
        {
            _list.Add(value);
        }

        public void Clear(bool disposeThem)
        {
            if (disposeThem)
                DisposeElements(_list.ToArray());
            _list.Clear();
        }

        public bool Contains(IDisposable value)
        {
            return _list.Contains(value);
        }

        public int IndexOf(IDisposable value)
        {
            return _list.IndexOf(value);
        }

        public bool Remove(IDisposable value, bool disposeIfContain)
        {
            var ret = _list.Contains(value);
            if (ret && disposeIfContain)
                DisposeElement(value);
            return _list.Remove(value);
        }

        public IDisposable this[int index]
        {
            get => _list[index];
        }
    }
}