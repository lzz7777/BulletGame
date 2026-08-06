using System;
using System.Collections.Concurrent;

namespace ByteDance.CloudSync
{
    internal class SafeMatchAPIListener : IMatchAPIListener, ISafeActionsUpdatable
    {
        private SafeActionsProxy _proxy;
        private readonly IMatchAPIListener _inner;
        private readonly ConcurrentQueue<Action> _actions = new();

        public SafeMatchAPIListener(IMatchAPIListener inner)
        {
            _proxy = new SafeActionsProxy();
            _inner = inner;
        }

        public void Update()
        {
            _proxy.Update();
        }

        public void OnPodCustomMessage(ApiPodMessageData msgData)
        {
            _proxy.RunOnUnity(() => _inner.OnPodCustomMessage(msgData));
        }

        public void OnCommandMessage(ApiMatchCommandMessage msg)
        {
            _proxy.RunOnUnity(() => _inner.OnCommandMessage(msg));
        }
    }
}