// Copyright (c) Bytedance. All rights reserved.
// Description:

using System.Collections.Generic;
using UnityEngine;

namespace ByteDance.CloudSync.Mock
{
    public partial class MockPlay
    {
        private bool _isTouchBegan;
        private bool _isTouchStaying;
        private Vector3 _lastTouchPoint = Vector3.zero;
        private readonly Dictionary<KeyCode, KeyState> _keyState = new();

        private enum KeyState
        {
            None,
            Down,
            Up
        }

        /// <summary>
        /// 上报鼠标等输入事件
        /// </summary>
        private void SyncInputEvents()
        {
            var screenRect = new Rect(0, 0, Screen.width, Screen.height);
            // 鼠标坐标。左上角坐标 ( 0, Screen.Height ) 原点在左下角，值范围 0~Screen宽高
            var point = Input.mousePosition;
            if (screenRect.Contains(point))
            {
                SendMouseEvents(point);
                SendMouseSimulateTouchEvents(point);
            }

            SendTouchEvents();
            SendKeyboardEvents();
        }

        /// <summary>
        /// 发送鼠标事件
        /// </summary>
        /// <param name="point">鼠标坐标。左上角坐标 ( 0, Screen.Height ) 原点在左下角，值范围 0~Screen宽高</param>
        private void SendMouseEvents(Vector3 point)
        {
            if (simulateTouchByMouse)
            {
                return;
            }

            var screenSize = new Vector2Int(Screen.width, Screen.height);
            // from: 鼠标坐标。左上角坐标 ( 0, Screen.Height ) 原点在左下角，值范围 0~Screen宽高</param>
            // to: InputEventSender 鼠标坐标。左上角坐标 ( 0, Screen.Height ) 原点在左下角，值范围 0~Screen宽高
            var sender = _rtcStream.InputEventSender;
            sender.SendMouseEvent(default, MouseAction.MOVE, point, screenSize, 0);
            for (var buttonIndex = (int)MouseButtonId.LEFT; buttonIndex <= (int)MouseButtonId.MIDDLE; buttonIndex++)
            {
                var button = (MouseButtonId)buttonIndex;
                if (Input.GetMouseButtonDown(buttonIndex))
                {
                    sender.SendMouseEvent(button, MouseAction.DOWN, point, screenSize, 0);
                }
                else if (Input.GetMouseButtonUp(buttonIndex))
                {
                    sender.SendMouseEvent(button, MouseAction.UP, point, screenSize, 0);
                }
            }

            var scrollDelta = Input.mouseScrollDelta;
            if (scrollDelta.y != 0)
            {
                sender.SendMouseEvent(default, MouseAction.WHEEL, point, screenSize, scrollDelta.y);
            }
        }

        /// <summary>
        /// 发送鼠标模拟Touch事件
        /// </summary>
        /// <param name="mousePoint">鼠标坐标。左上角坐标 ( 0, Screen.Height ) 原点在左下角，值范围 0~Screen宽高</param>
        private void SendMouseSimulateTouchEvents(Vector3 mousePoint)
        {
            if (!simulateTouchByMouse)
                return;

            var lastPoint = _lastTouchPoint;
            _lastTouchPoint = mousePoint;
            var touch = CreateTouch(0, mousePoint);

            var frameDown = Input.GetMouseButtonDown(0);
            var frameUp = Input.GetMouseButtonUp(0);
            if (frameDown)
            {
                _isTouchBegan = true;
                touch.phase = TouchPhase.Began;
                SendTouch(touch);
            }
            else if (frameUp)
            {
                _isTouchBegan = false;
                touch.phase = TouchPhase.Ended;
                SendTouch(touch);
            }

            if (!_isTouchBegan || frameDown || frameUp)
            {
                // i.e., not moving, nor staying
                _isTouchStaying = false;
                return;
            }

            var isStay = simulateTouchStationary && VectorApproximately(mousePoint, lastPoint);
            if (!isStay)
            {
                _isTouchStaying = false;
                touch.phase = TouchPhase.Moved;
                SendTouch(touch);
                return;
            }

            if (_isTouchStaying)
                return;
            _isTouchStaying = true;
            touch.phase = TouchPhase.Moved;
            SendTouch(touch);
        }

        /// <summary>
        /// 创建一个`UnityEngine.Touch`
        /// </summary>
        /// <param name="fingerId"></param>
        /// <param name="mousePoint">鼠标坐标。左上角坐标 ( 0, Screen.Height ) 原点在左下角，值范围 0~Screen宽高</param>
        private static Touch CreateTouch(int fingerId, Vector3 mousePoint)
        {
            // `UnityEngine.Touch` position 左上角坐标 ( 0, Screen.Height ) 原点在左下角，值范围 0~Screen宽高
            return new Touch
            {
                fingerId = fingerId,
                position = mousePoint,
                rawPosition = mousePoint,
                deltaPosition = default,
                phase = TouchPhase.Canceled,
            };
        }

        private bool VectorApproximately(Vector3 point, Vector3 point2)
        {
            // 由Vector3实现
            return point == point2;
        }

        private void SendTouch(Touch touch) => SendTouches(new[] { touch });

        private void SendTouches(Touch[] touches)
        {
            var sender = _rtcStream.InputEventSender;
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            sender.SendTouchesEvent(touches, screenSize);
        }

        /// <summary>
        /// 发送Touch事件
        /// </summary>
        private void SendTouchEvents()
        {
            if (Input.touchCount <= 0)
                return;
            SendTouches(Input.touches);
        }

        /// <summary>
        /// 发送键盘事件
        /// </summary>
        private void SendKeyboardEvents()
        {
            foreach (var supportedKey in CloudPCKeycode.SupportedKeys)
            {
                var state = _keyState.GetValueOrDefault(supportedKey, KeyState.None);
                var sender = _rtcStream.InputEventSender;
                if (state != KeyState.Down && Input.GetKeyDown(supportedKey))
                {
                    sender.SendKeyboardEvent(KeyboardAction.DOWN, supportedKey);
                    _keyState[supportedKey] = KeyState.Down;
                }
                else if (state != KeyState.Up && Input.GetKeyUp(supportedKey))
                {
                    sender.SendKeyboardEvent(KeyboardAction.UP, supportedKey);
                    _keyState[supportedKey] = KeyState.Up;
                }
            }
        }
    }
}