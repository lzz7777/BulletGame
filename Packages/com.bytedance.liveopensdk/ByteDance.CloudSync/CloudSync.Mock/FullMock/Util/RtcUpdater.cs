using Unity.WebRTC;
using UnityEngine;

namespace ByteDance.CloudSync.Mock
{
    internal static class RtcUpdater
    {
        private static Runner _runner;
        
        public static void EnsureUpdate()
        {
            if (_runner)
                return;
            var go = new GameObject("[WebRtcUpdater]");
            go.hideFlags |= HideFlags.HideAndDontSave;
            _runner = go.AddComponent<Runner>();
        }

        public static void StopUpdate()
        {
            if (_runner)
            {
                Object.Destroy(_runner.gameObject);
                _runner = null;
            }
        }
    }

    class Runner : MonoBehaviour
    {
        private void Start()
        {
            StartCoroutine(WebRTC.Update());
        }
    }
}