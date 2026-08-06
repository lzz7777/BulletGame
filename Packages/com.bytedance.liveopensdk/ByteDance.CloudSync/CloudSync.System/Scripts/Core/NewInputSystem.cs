// Copyright (c) Bytedance. All rights reserved.
// Description:

using UnityEngine;
using UnityEngine.EventSystems;

namespace ByteDance.CloudSync
{
    [DllMonoBehaviour]
    public class NewInputSystem : MonoBehaviour
    {
        [SerializeField]
        private EventSystem eventSystem;

        private EventSystem _lastEventSystem;
        private bool _initSdk;
        private bool _enableSdk;
        private bool _enableComponent;

        public void OnInitSdk()
        {
            _initSdk = true;
            DoEnable();
        }

        public void OnEnableSdk()
        {
            _enableSdk = true;
            DoEnable();
        }

        private void OnEnable()
        {
            _enableComponent = true;
            DoEnable();
        }

        public void OnDisableSdk()
        {
            _enableSdk = false;
            DoDisable();
        }

        private void OnDisable()
        {
            _enableComponent = false;
            DoDisable();
        }

        private void DoEnable()
        {
            if (!_initSdk || !_enableSdk || !_enableComponent)
                return;
            if (eventSystem == null)
                eventSystem = transform.Find("EventSystem").GetComponent<EventSystem>();
            Debug.Assert(eventSystem != null, "Assert EventSystem Failed! Maybe prefab is broken. Please reimport \"Packages/LiveOpenSDK\"!");
            if (eventSystem != null)
                Debug.Assert(eventSystem.enabled, "Assert EventSystem Failed! EventSystem component in `CloudGameSystem.prefab` must be enabled!");
            if (EventSystem.current != eventSystem)
            {
                _lastEventSystem = EventSystem.current;
                if (_lastEventSystem != null)
                    _lastEventSystem.gameObject.SetActive(false);
            }

            eventSystem.gameObject.SetActive(true);
        }

        private void DoDisable()
        {
            if (_initSdk && _enableSdk && _enableComponent)
                return;
            if (_lastEventSystem != null)
                _lastEventSystem.gameObject.SetActive(true);
            eventSystem.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_lastEventSystem != null)
                _lastEventSystem.gameObject.SetActive(true);
        }
    }
}