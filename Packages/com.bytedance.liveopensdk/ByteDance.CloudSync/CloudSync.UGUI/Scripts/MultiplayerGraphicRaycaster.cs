using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.Users;
using UnityEngine.UI;

namespace ByteDance.CloudSync.UGUI
{
    [DllMonoBehaviour]
    public class MultiplayerGraphicRaycaster : GraphicRaycaster
    {
        public SeatIndex Index => _index;
        public bool IsCloudUser => _index != SeatIndex.Invalid;

        private SeatIndex _index = SeatIndex.Invalid;
        private InputUser _inputUser;
        private HashSet<int> _pairedDevices;
        private readonly Dictionary<int, GameObject> _pointerEnterObjects = new();

        public virtual void InitInput(InputUser user, SeatIndex index)
        {
            _index = index;
            _inputUser = user;
            _pairedDevices = new HashSet<int>(_inputUser.pairedDevices.Select(d => d.deviceId));
            CGLogger.Log($"Raycaster InitInput {UserInfo}, {PairedInfo}");
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (eventCamera == null)
            {
                var objName = transform.parent != null ? $"{transform.parent.name}\\{gameObject.name}" : gameObject.name;
                CGLogger.LogError($"Raycaster camera invalid! Maybe you need to set canvas RenderMode.ScreenSpaceCamera && Render Camera. gameObject: \"{objName}\"");
                return;
            }

            // 防止主屏响应离屏的触摸事件，和RemoteInput中的事件输入UnityInputSystem.QueueStateEvent里displayIndex对应上就可以
            VirtualScreenSystem.SetTargetDisplay(eventCamera);
            CGLogger.Log($"Raycaster OnEnable seat: {(int)_index} user id: {_inputUser.id} {eventCamera.gameObject.name} #{Time.frameCount}f");
        }

        public override int sortOrderPriority => IsCloudUser ? int.MaxValue : base.sortOrderPriority;

        public virtual bool IsPointerOverGameObject()
        {
            return _pointerEnterObjects.TryGetValue(0, out var g) && g != null;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            CGLogger.Log($"Raycaster OnDisable seat: {(int)_index} user id: {_inputUser.id} {eventCamera.gameObject.name} #{Time.frameCount}f");
        }

        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            if (!IsCloudUser)
            {
                base.Raycast(eventData, resultAppendList);
                return;
            }

            if (eventData is not ExtendedPointerEventData data)
                return;

            if (_pairedDevices == null)
                return;

            var device = data.device;
            if (device is not ICustomInputDevice customDevice)
                return;
            if (customDevice.SeatIndex != _index)
                return;
            if (!_pairedDevices.Contains(device.deviceId))
                return;

            resultAppendList.Clear();
            base.Raycast(eventData, resultAppendList);

            if (resultAppendList.Count > 0)
            {
                var enterObject = resultAppendList[0].gameObject;
                _pointerEnterObjects[data.touchId] = enterObject;
                // Debug.Log($"Raycaster {UserInfo} {DeviceInfo(device)}, touch {data.touchId} {data.position:F1}, enterObject: {enterObject} #{Time.frameCount}f");
            }
            else
            {
                _pointerEnterObjects[data.touchId] = null;
                // Debug.Log($"Raycaster {UserInfo} {DeviceInfo(device)}, touch {data.touchId} {data.position:F1}, enterObject: null #{Time.frameCount}f");
            }
        }

        private string UserInfo => $"seat: {_index} input: {_inputUser.id}";
        private string DeviceInfo(InputDevice device) => $"device: {device.deviceId} {device.name}";
        private string PairedInfo => $"pairedDevices {{{string.Join(",", _pairedDevices)}}} ({_pairedDevices.Count})";
    }
}
