using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using UnityEngine.UI;

namespace ByteDance.CloudSync.UGUI
{
    /// <summary>
    /// UGUI 下的远程输入处理。注意根 Canvas 一定要加上 <see cref="MultiplayerGraphicRaycaster"/>
    /// </summary>
    internal class UVirtualDeviceInput : IVirtualDeviceInput
    {
        private RemoteInput _remoteInput;
        private CloudGameInputDebugger _debugger;
        private UCloudView _view;
        private InputUser _inputUser;
        private SeatIndex _deviceIndex;
        private CustomMouse _remoteMouse;
        private CustomKeyboard _keyboard;
        private CustomGamepad _gamepad;
        private CustomTouchscreen _touchscreen;
        private readonly List<InputDevice> _devices = new();
        private readonly List<MultiplayerGraphicRaycaster> _casters = new();

        public void Init(IVirtualDevice device, UCloudView view)
        {
            _view = view;
            var graphicRaycasters = view.GameObject.GetComponentsInChildren<GraphicRaycaster>();
            foreach (var raycaster in graphicRaycasters)
            {
                if (raycaster is not MultiplayerGraphicRaycaster caster)
                {
                    var obj = raycaster.gameObject;
                    Object.DestroyImmediate(raycaster);
                    caster = obj.AddComponent<MultiplayerGraphicRaycaster>();
                }
                _casters.Add(caster);
            }

            InitInputUser(device, view);
        }

        public bool IsPointerOverGameObject()
        {
            return _casters.Any(it => it.IsPointerOverGameObject());
        }

        public void Close()
        {
            CGLogger.Log($"Close input user, seat: {_deviceIndex} {(_inputUser.valid ? $"user id: {_inputUser.id} {_inputUser.index}" : "")}");
            foreach (var device in _devices.ToList())
            {
                RemoveDevice(device);
            }

            if (_inputUser.valid)
                _inputUser.UnpairDevicesAndRemoveUser();
        }

        private void InitInputUser(IVirtualDevice device, UCloudView canvas)
        {
            var index = device.Index;
            _deviceIndex = index;
            if (!Application.isPlaying)
                return;

            InputUser user = InputUser.CreateUserWithoutPairedDevices();
            _inputUser = user;

            CGLogger.Log($"Init input user, seat: {index}, user id: {user.id}, {user.index}, runInBackground: {Application.runInBackground}");
            var intIndex = index.ToInt();
            _remoteMouse = AddDevice<CustomMouse>($"{nameof(CustomMouse)}{intIndex}");
            CGLogger.Log($"AddDevice, seat: {index}, {GetDeviceInfo(_remoteMouse)}");
            _keyboard = AddDevice<CustomKeyboard>($"{nameof(CustomKeyboard)}{intIndex}");
            _gamepad = AddDevice<CustomGamepad>($"{nameof(CustomGamepad)}{intIndex}");
            _touchscreen = AddDevice<CustomTouchscreen>($"{nameof(CustomTouchscreen)}{intIndex}");
            _remoteInput = new RemoteInput(device);
            _remoteInput.OnMouse += OnMouseEvent;
            _remoteInput.OnTouches += OnTouchesEvent;

            var raycasters = canvas.GameObject.GetComponentsInChildren<MultiplayerGraphicRaycaster>();
            foreach (var raycaster in raycasters)
            {
                raycaster.InitInput(user, _deviceIndex);
            }
        }

        private T AddDevice<T>(string name) where T : InputDevice, ICustomInputDevice
        {
            var device = InputSystem.GetDevice<T>(name);
            var prevAdded = device != null;
            device ??= InputSystem.AddDevice<T>(name);
            device.SeatIndex = _deviceIndex;
            // CGLogger.Log($"AddDevice, seat: {_deviceIndex}, {GetDeviceInfo(device)}, prevAdded: {prevAdded}");
            _inputUser = InputUser.PerformPairingWithDevice(device, _inputUser);
            _devices.Add(device);
            return device;
        }

        private string GetDeviceInfo(InputDevice device)
        {
            return $"device: {device.name}, canRunInBackground: {device.canRunInBackground}";
        }

        private void RemoveDevice(InputDevice device)
        {
            // ReSharper disable once MergeIntoNegatedPattern
            if (device == null || !device.added)
                return;
            CGLogger.Log($"RemoveDevice, seat: {_deviceIndex}, device: {device.name} {device}");
            InputSystem.RemoveDevice(device);
            _devices.Remove(device);
        }

        private void OnTouchesEvent(RemoteTouchesEvent evt)
        {
            foreach (var t in evt.touches)
            {
                var phase = (UnityEngine.InputSystem.TouchPhase)t.phase;
                if (RemoteInput.IsVerboseLogForInput)
                    Debug.Log($"UDevice touch event {t.touchId}, {phase}, {t.viewPosition:F3}, {t.position:F0}, #{Time.frameCount}f");
                InputSystem.QueueStateEvent(_touchscreen, new TouchState
                {
                    touchId = t.touchId,
                    position = t.position,
                    phase = phase,
                    displayIndex = evt.displayIndex
                });
            }
        }

        private void OnMouseEvent(RemoteMouseEvent e)
        {
            InputSystem.QueueStateEvent(_remoteMouse, new MouseState
            {
                position = e.position,
                delta = e.delta,
                buttons = e.buttons,
                displayIndex = e.displayIndex,
                clickCount = e.clickCount,
                scroll = new Vector2(0, (float)e.wheel),
            });
        }

        public bool Enable { get; set; }

        public IRemoteInput RemoteInput => _remoteInput;

        public void ProcessInput(PlayerOperate operate)
        {
            if (!Enable)
                return;
            if (_debugger)
                _debugger.HandleOperate(operate);
            _remoteInput.ProcessInput(operate);
        }

        public void ProcessMouseEvent(CloudMouseData data)
        {
            if (!Enable)
                return;
            _remoteInput.ProcessMouseEvent(data);
        }

        public void ProcessTouchEvent(IList<CloudTouchData> touchesData)
        {
            if (!Enable)
                return;
            _remoteInput.ProcessTouchEvent(touchesData);
        }

        public void ProcessKeyboardEvent(KeyboardAction action, KeyCode keyCode)
        {
            if (!Enable)
                return;
            _remoteInput.ProcessPCKeyboardEvent((PCKeyboardAction)action, keyCode);
        }

        public void EnableDebugger()
        {
            if (_debugger)
                return;

            var prefab = Resources.Load<GameObject>("Debug/CloudGameInputDebugger");
            var go = Object.Instantiate(prefab, _view.UIRoot);
            _debugger = go.GetComponent<CloudGameInputDebugger>();

            var rectTransform = go.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;

            rectTransform.localScale = Vector3.one;
            rectTransform.localPosition = Vector3.zero;

            go.SetActive(true);
        }
    }
}