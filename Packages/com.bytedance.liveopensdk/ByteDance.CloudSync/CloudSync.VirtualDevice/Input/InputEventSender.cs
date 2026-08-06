using System;
using System.Linq;
using UnityEngine;

namespace ByteDance.CloudSync
{
    /// <summary>
    /// 输入事件发送器
    /// </summary>
    public interface IInputEventSender
    {
        /// <param name="button"></param>
        /// <param name="actionType"></param>
        /// <param name="point">鼠标坐标。左上角坐标 ( 0, Screen.Height ) 原点在左下角，值范围 0~Screen宽高</param>
        /// <param name="screenSize"></param>
        /// <param name="wheel"></param>
        void SendMouseEvent(MouseButtonId button, MouseAction actionType, Vector2 point, Vector2Int screenSize, double wheel);

        void SendTouchesEvent(Touch[] touches, Vector2Int screenSize);

        void SendKeyboardEvent(KeyboardAction actionType, KeyCode keyCode);
    }

    /// <summary>
    /// Mock输入事件发送器。
    /// Mock发送来自端上玩法窗口的Input输入事件。
    /// 发送给对应设备 <see cref="IVirtualDevice"/> 的输入对象 <see cref="IVirtualDevice.Input"/> ，由它处理输入事件。
    /// 参考例如 <see cref="IVirtualDeviceInput.ProcessMouseEvent"/> 处理鼠标事件。
    /// </summary>
    public class MockInputEventSender : IInputEventSender
    {
        private bool _moved;
        private Vector2 _lastPoint;
        private Vector2Int _lastScreenSize;

        public MockInputEventSender(SeatIndex index = SeatIndex.Invalid)
        {
            Index = index;
        }

        public SeatIndex Index { get; set; }

        private IVirtualDevice GetDevice()
        {
            return VirtualDeviceSystem.Find(Index);
        }

        /// <summary>
        /// 更新是否dirty标记，需要输入当前x,y,w,h数值
        /// </summary>
        private void UpdateIsMoved(Vector2 point, Vector2Int screen, out bool isMoved)
        {
            if (_moved)
            {
                isMoved = true;
                return;
            }

            isMoved = !Mathf.Approximately(point.x, _lastPoint.x)
                      || !Mathf.Approximately(point.y, _lastPoint.y)
                      || screen != _lastScreenSize;
            _lastPoint = point;
            _lastScreenSize = screen;
            _moved = isMoved;
        }

        private void ClearMovedFlag()
        {
            _moved = false;
        }

        /// <inheritdoc cref="IInputEventSender.SendMouseEvent"/>
        // "point" 鼠标坐标。左上角坐标 ( 0, Screen.Height ) 原点在左下角，值范围 0~Screen宽高</param>
        public void SendMouseEvent(MouseButtonId button, MouseAction actionType, Vector2 point, Vector2Int screenSize, double wheel)
        {
            var device = GetDevice();
            if (device == null)
                return;

            try
            {
                UpdateMove(actionType, point, screenSize);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            // from: "point" 鼠标坐标。左上角坐标 ( 0, Screen.Height ) 原点在左下角，值范围 0~Screen宽高</param>
            // to: 云端鼠标事件 x, y: double类型。 x,y 左上角坐标 ( 0, 0 ) 原点在左上角，值范围 0~1.0
            var x = point.x / screenSize.x;
            var y = 1.0 - point.y / screenSize.y;
            ClearMovedFlag();
            device.Input.ProcessMouseEvent(new CloudMouseData
            {
                x = x,
                y = y,
                action = actionType,
                button = button,
                wheel = wheel,
            });
        }

        public void SendTouchesEvent(Touch[] touches, Vector2Int screenSize)
        {
            var device = GetDevice();
            if (device == null)
                return;

            var list = touches.Select(s => CloudTouchData.Create(s, screenSize)).ToArray();
            device.Input.ProcessTouchEvent(list);
        }

        private void UpdateMove(MouseAction actionType, Vector2 point, Vector2Int screenSize)
        {
            if (actionType == MouseAction.MOVE)
                UpdateMove(point, screenSize);
        }

        private void UpdateMove(Vector2 point, Vector2Int screenSize)
        {
            UpdateIsMoved(point, screenSize, out var isMoved);

            var cancel = !isMoved;
            var x = point.x / screenSize.x;
            var y = point.y / screenSize.y;
            cancel = cancel || x < 0 || x > 1;
            cancel = cancel || y < 0 || y > 1;
            if (cancel)
                throw new OperationCanceledException();
        }

        public void SendKeyboardEvent(KeyboardAction actionType, KeyCode keyCode)
        {
            var device = GetDevice();
            if (device == null)
                return;
            device.Input.ProcessKeyboardEvent(actionType, keyCode);
        }
    }
}