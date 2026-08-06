using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace ByteDance.CloudSync
{
    public interface ISafeActionsUpdatable
    {
        void Update();
    }

    public class SafeActionsProxy : ISafeActionsUpdatable
    {
        private readonly ConcurrentQueue<Action> _actions = new();
        private readonly List<Action> _buffer = new();

        public void RunOnUnity(Action action)
        {
            _actions.Enqueue(action);
        }

        public void Update()
        {
            if (_actions.Count == 0)
                return;

            _buffer.Clear();
            while (_actions.TryDequeue(out var action))
            {
                _buffer.Add(action);
            }

            foreach (var action in _buffer)
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            _buffer.Clear();
        }
    }
}