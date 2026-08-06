using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ByteDance.CloudSync.Editor
{
    /// <summary>
    /// Editor面板调试 Cloud Screen 1~4 用的 View
    /// </summary>
    internal class VisualScreenView
    {
        private readonly IInputEventSender _eventSender;
        private readonly EditorWindow _hostWindow;
        private readonly SeatIndex _index;
        private readonly Dictionary<KeyCode, KeyState> _keyState = new();
        private Vector2 _lastMousePoint;
        private bool _callMockJoin;

        public VisualScreenView(EditorWindow hostWindow, SeatIndex index)
        {
            _hostWindow = hostWindow;
            _index = index;
            _eventSender = new MockInputEventSender(index);
        }

        public void Draw(Rect rect)
        {
            if (!Application.isPlaying || !CloudSyncSdk.Initialized)
                return;

            var device = VirtualDeviceSystem.Find(_index);
            var screen = device?.Screen;
            if (screen == null || screen.Enable == false)
            {
                TryMockJoin();
                return;
            }

            var texture = screen.RenderTexture;
            if (!texture)
                return;

            // draw image
            DrawScreen(rect, texture);

            // mouse
            if (Event.current.isMouse || Event.current.isScrollWheel)
            {
                var imageRect = GetAspectRect(texture, rect);
                // Editor 窗口事件鼠标坐标，左上角坐标 ( 0, 0 ) 原点在左上角，值范围 0~Screen宽高
                var pos = Event.current.mousePosition - imageRect.position;
                var viewRect = new Rect(0, 0, imageRect.width, imageRect.height);

                ProcessMouse(pos, viewRect);
            }
            // keyboard
            else if (Event.current.isKey && _hostWindow.hasFocus)
            {
                ProcessKeys();
            }
        }

        /// <param name="pos">Editor 窗口事件鼠标坐标，左上角坐标 ( 0, 0 ) 原点在左上角，值范围 0~Screen宽高</param>
        /// <param name="viewRect">窗口矩形</param>
        private void ProcessMouse(Vector2 pos, Rect viewRect)
        {
            var screenSize = new Vector2Int((int)viewRect.size.x, (int)viewRect.size.y);
            if (!viewRect.Contains(pos))
                return;

            // from: Editor 窗口事件鼠标坐标，左上角坐标 ( 0, 0 ) 原点在左上角，值范围 0~Screen宽高
            // to: InputEventSender 鼠标坐标。左上角坐标 ( 0, Screen.Height ) 原点在左下角，值范围 0~Screen宽高
            var mousePoint = new Vector2(pos.x, screenSize.y - pos.y);
            if (_lastMousePoint != mousePoint)
            {
                // 模拟云游戏数据，只在 Move 时传坐标，其它传 0，0
                _eventSender.SendMouseEvent(default, MouseAction.MOVE, mousePoint, screenSize, 0);
                _lastMousePoint = mousePoint;
            }

            var button = (MouseButtonId)Event.current.button;
            switch (Event.current.type)
            {
                case EventType.MouseDown:
                    _eventSender.SendMouseEvent(button, MouseAction.DOWN, Vector2.zero, screenSize, 0);
                    break;
                case EventType.MouseUp:
                    _eventSender.SendMouseEvent(button, MouseAction.UP, Vector2.zero, screenSize, 0);
                    break;
                case EventType.ScrollWheel:
                    var scrollDelta = Event.current.delta;
                    _eventSender.SendMouseEvent(default, MouseAction.WHEEL, Vector2.zero, screenSize, scrollDelta.y);
                    break;
            }
        }

        private void ProcessKeys()
        {
            var keyCode = Event.current.keyCode;
            if (!CloudPCKeycode.SupportedKeys.Contains(keyCode))
                return;

            var state = _keyState.GetValueOrDefault(keyCode, KeyState.None);
            if (state != KeyState.Down && Event.current.type == EventType.KeyDown)
            {
                _eventSender.SendKeyboardEvent(KeyboardAction.DOWN, keyCode);
                _keyState[keyCode] = KeyState.Down;
            }
            else if (state != KeyState.Up && Event.current.type == EventType.KeyUp)
            {
                _eventSender.SendKeyboardEvent(KeyboardAction.UP, keyCode);
                _keyState[keyCode] = KeyState.Up;
            }
        }

        private void DrawScreen(Rect rect, Texture texture)
        {
            var imageRect = GetAspectRect(texture, rect);
            var absolutOffsetY = _hostWindow.rootVisualElement.worldBound.y + rect.y;
            var matrix = GUI.matrix;
            matrix *= Matrix4x4.Scale(new Vector3(1, -1, 1));
            matrix *= Matrix4x4.Translate(new Vector3(0, -rect.height - absolutOffsetY * 2, 0));

            GUI.matrix = matrix;
            GUI.color= Color.white;
            rect.position = Vector2.zero;
            GUI.DrawTexture(imageRect, texture);
            GUI.matrix = Matrix4x4.identity;
        }

        private Rect GetAspectRect(Texture image, Rect position)
        {
            var imageAspect = image.width / (float)image.height;
            var num1 = position.width / position.height;
            if (num1 > imageAspect)
            {
                var num2 = imageAspect / num1;
                return new Rect(position.xMin + (float) (position.width * (1.0 - num2) * 0.5), position.yMin, num2 * position.width, position.height);
            }
            var num3 = num1 / imageAspect;
            return new Rect(position.xMin, position.yMin + (float) (position.height * (1.0 - num3) * 0.5), position.width, num3 * position.height);
        }

        private async void TryMockJoin()
        {
            if (!Application.isPlaying || !CloudSyncSdk.Initialized)
                return;
            // Screen面板 仅用于 Simple 模式
            if (MockCloudSync.Instance.MockType != MockType.Simple)
                return;
            // Host 不需要 MockJoin
            if (_index == 0)
                return;
            if (_callMockJoin)
                return;

            _callMockJoin = true;
            var seat = CloudSyncSdk.GetInstance().SeatManager.GetSeat(_index);
            if (seat.State == SeatState.InUse)
                return;
            await MockCloudSync.Instance.MockJoin(_index);
            _callMockJoin = false;
        }

        private enum KeyState
        {
            None, Down, Up
        }

        public void Dispose()
        {
            if (!Application.isPlaying || !CloudSyncSdk.Initialized)
                return;

            if (MockCloudSync.Instance.MockType == MockType.Full)
                return;

            if (_index == 0)
                return;

            var seat = CloudSyncSdk.GetInstance().SeatManager.GetSeat(_index);
            if (seat.State == SeatState.InUse)
                MockCloudSync.Instance.MockExit(_index);
        }
    }
}