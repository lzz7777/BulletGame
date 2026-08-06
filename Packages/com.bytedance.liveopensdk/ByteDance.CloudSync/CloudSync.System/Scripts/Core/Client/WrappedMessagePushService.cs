// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using System.Threading.Tasks;
using ByteDance.LiveOpenSdk;
using ByteDance.LiveOpenSdk.Push;
using UnityEngine;

namespace ByteDance.CloudSync
{
    /// <summary>
    /// 封装包裹的 IMessagePushService，开发者没有正确释放事件也没有关系。
    /// </summary>
    internal class WrappedMessagePushService : IMessagePushService, IDisposable
    {
        private IMessagePushService _inner;

        public ConnectionState ConnectionState
        {
            get
            {
                if (_inner == null)
                    return ConnectionState.Disconnected;
                return _inner.ConnectionState;
            }
        }

        public event Action<ConnectionState> OnConnectionStateChanged;

        public event OnPushMessageCallback OnMessage;

        public WrappedMessagePushService()
        {
        }

        public WrappedMessagePushService(ILiveOpenSdk openSdk)
        {
            _inner = openSdk.GetService<IMessagePushService>();
            _inner.OnMessage += OnMessageImpl;
            _inner.OnConnectionStateChanged += OnConnectionStateChangedImpl;
        }

        private void OnConnectionStateChangedImpl(ConnectionState state)
        {
            InvokeEvent(nameof(OnConnectionStateChanged),() => OnConnectionStateChanged?.Invoke(state));
        }

        private void OnMessageImpl(IPushMessage message)
        {
            //这个是点赞/评论/礼物消息回调，可能会太频繁。因此先不包上前后log。
            OnMessage?.Invoke(message);
        }

        public Task StartPushTaskAsync(string msgType, MultiPushType pushType = MultiPushType.SinglePush)
        {
            if (_inner == null)
                return Task.CompletedTask;

            return _inner.StartPushTaskAsync(msgType, pushType);
        }

        public Task StopPushTaskAsync(string msgType)
        {
            if (_inner == null)
                return Task.CompletedTask;

            return _inner.StopPushTaskAsync(msgType);
        }

        public void Dispose()
        {
            if (_inner == null) return;
            _inner.OnMessage -= OnMessageImpl;
            _inner.OnConnectionStateChanged -= OnConnectionStateChangedImpl;
            _inner = null;
        }

        private static void InvokeEvent(string name, Action action) => CloudSyncSdk.InvokeEvent(name, action);
    }
}