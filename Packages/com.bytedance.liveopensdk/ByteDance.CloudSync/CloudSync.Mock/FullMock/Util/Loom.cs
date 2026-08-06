using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace ByteDance.CloudSync.Mock
{
    public static class Loom
    {
        private static readonly ConcurrentQueue<Action> _actions = new();
        private static readonly List<Action> _tempActions = new();
        
        [RuntimeInitializeOnLoadMethod]
        private static async void Init()
        {
            while (Application.isPlaying)
            {
                await Task.Yield();
                Update();
            }
        }

        private static void Update()
        {
            if (_actions.Count > 0)
            {
                _tempActions.AddRange(_actions);
                _actions.Clear();
                foreach (var action in _tempActions)
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
                _tempActions.Clear();
            }
        }

        public static void Run(Action action)
        {
            _actions.Enqueue(action);
        }
    }
}