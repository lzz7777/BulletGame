using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

// ReSharper disable InconsistentNaming

namespace ByteDance.CloudSync
{
    public interface IRemoteInput
    {
        /// 是否调试log开启，对于Input输入
        bool IsVerboseLogForInput { get; }

        event Action<RemoteMouseEvent> OnMouse;

        event Action<RemoteKeyboardEvent> OnKeyboard;

        event Action<RemoteTouchesEvent> OnTouches;

        /// <summary>
        /// 有输入的帧序号
        /// </summary>
        int InputFrame { get; }

        /// <summary>
        /// 有输入Touch（触摸）的帧序号
        /// </summary>
        int InputTouchFrame { get; }

        /// <summary>
        /// 有输入Mouse（鼠标）帧序号
        /// </summary>
        int InputMouseFrame { get; }

        /// <summary>
        /// 指定的Key是否处于按下状态
        /// </summary>
        /// <param name="keyCode">UnityEngine.KeyCode</param>
        /// <returns></returns>
        bool GetKey(int keyCode);

        /// <summary>
        /// 是否开启键盘事件的`character`字符。 参考 <see cref="RemoteKeyboardEvent.character"/>
        /// </summary>
        bool EnableKeyboardCharacter { get; set; }
    }

    public class RemoteInput : IRemoteInput
    {
        // 是否详细输入日志，调试用
        public static bool IsVerboseInputLog
        {
            get => CloudGameSdk.IsVerboseLogForInput;
            set => CloudGameSdk.IsVerboseLogForInput = value;
        }

        public bool IsVerboseLogForInput => CloudGameSdk.IsVerboseLogForInput;
        public event Action<RemoteMouseEvent> OnMouse;
        public event Action<RemoteKeyboardEvent> OnKeyboard;
        public event Action<RemoteTouchesEvent> OnTouches;

        public int InputFrame { get; private set; }

        public int InputTouchFrame { get; private set; }

        public int InputMouseFrame { get; private set; }

        private readonly HashSet<KeyCode> _mPressedKey = new();
        private EventModifiers _mPressedModifiers;

        private int DisplayIndex => VirtualScreenSystem.TargetDisplayIndex;

        private readonly IVirtualDevice _device;

        private readonly LocalMouseState _mouseState = new();
        private static readonly SdkDebugLogger Debug = new("CloudInput");
        private const float DoubleClickThresh = 0.5f; // 单位：秒，默认0，5秒

        /// <summary>
        /// 本地鼠标状态数据（仅内部）
        /// </summary>
        internal class LocalMouseState
        {
            /// `viewPosition` 左上角坐标 ( 0, 1 ) , 原点 ( 0, 0 ) 在左下角，值范围 0~1.0
            public Vector2 ViewPosition;

            /// `position` 左上角坐标 ( 0, Screen.Height ) , 原点 ( 0, 0 ) 在左下角，值范围 0~Screen宽高
            public Vector2Int Pos;

            public Vector2Int PrevPos;

            public Vector2 delta => Pos - PrevPos;

            public void SetPos(double x, double y)
            {
                PrevPos = Pos;
                Pos = new Vector2Int((int)x, (int)y);
            }

            /// note: 保持按下状态，Down时true，直到Up时变 false。（注意不是仅在Down的那一帧true）
            public bool LeftDown;

            /// <summary>
            /// 按键状态。 see <see cref="RemoteMouseEvent.buttons"/>
            /// </summary>
            public ushort buttons;

            public ushort clickCount;
            public MouseButtonId lastClickButton;
            public float lastClickTime;
        }

        public RemoteInput(IVirtualDevice virtualDevice)
        {
            _device = virtualDevice;
        }

        private const int RemoteTouchIDOffset = 1000;

        private int Frame => Time.frameCount;

        // todo: refactor 改为 internal
        // note: make sure to process in `EarlyUpdate` (i.e., before `Update` in lifecycle)
        public void ProcessInput(PlayerOperate op)
        {
            if (op == null)
                throw new ArgumentNullException();

            InputFrame = Frame;
            switch (op.op_type)
            {
                case OperateType.MOUSE:
                {
                    var data = op.event_data.ToObject<CloudMouseData>();
                    ProcessMouseEvent(data);
                    break;
                }
                case OperateType.MOBILE_KEY:
                {
                    var data = op.event_data.ToObject<CloudMobileKeyData>();
                    ProcessMobileKeyboardEvent(data.action, data.key_code);
                    break;
                }
                case OperateType.PC_KEYBOARD:
                {
                    var data = op.event_data.ToObject<CloudPCKeyboardData>();
                    var key = CloudPCKeycode.ToUnityKeyCode(data.key_code);
                    ProcessPCKeyboardEvent(data.action, key, data.key_code);
                    break;
                }
                case OperateType.TOUCH:
                {
                    var touchesData = op.event_data.ToObject<List<CloudTouchData>>();
                    ProcessTouchEvent(touchesData);
                    break;
                }
            }
        }

        // MARK: - Touch
        public void ProcessTouchEvent(IList<CloudTouchData> touchesData)
        {
            InputTouchFrame = InputFrame;
            var touchesEvent = new RemoteTouchesEvent
            {
                device = _device,
                displayIndex = (byte)DisplayIndex,
                touches = new List<RemoteTouch>(touchesData.Count),
            };

            var resolution = _device.Screen.Resolution;
            foreach (var data in touchesData)
            {
                if (data == null)
                    continue;
                var t = data.ToRemoteTouch(resolution, RemoteTouchIDOffset);
                if (t.phase == RemoteTouchPhase.None)
                    continue;
                touchesEvent.touches.Add(t);

                var viewPos = t.viewPosition;
                var pos = t.position;
                if (CloudGameSdk.IsVerboseLogForInput)
                    Debug.Log($"Input Touch pointer: {data.pointerId}, {t.action} ({data.action}), phase: {t.phase}" +
                              $", data.x,y: {data.x:F4},{data.y:F4}, viewPos: {viewPos:F3}, pos: {pos:F0}, resolution: {resolution:F0}, #{Time.frameCount}f");
            }

            Profiler.BeginSample("ProcessTouchMoveEvent");
            ProcessTouchesEvent(touchesEvent);
            Profiler.EndSample();
        }

        private void ProcessTouchesEvent(RemoteTouchesEvent touches)
        {
            OnTouches?.Invoke(touches);
        }

        // MARK: - Mouse

        private void UpdateMouseState(CloudMouseData data) => UpdateMouseState(_mouseState, data);

        internal void UpdateMouseState(LocalMouseState mouseState, CloudMouseData data)
        {
            var resolution = _device.Screen.Resolution;
            // from: 云端 CloudMouseData `data` x,y 左上角坐标 ( 0, 0 ) 原点在左上角，值范围 0~1.0
            // to: ->本地 LocalMouseState `viewPosition` 左上角坐标 ( 0, 1 ) , 原点在左下角，值范围 0~1.0
            var viewPosition = new Vector2((float)data.x, (float)(1 - data.y));
            var pos_x = viewPosition.x * resolution.x;
            var pos_y = viewPosition.y * resolution.y;
            var action = data.action;

            if (action != MouseAction.MOVE)
            {
                // 由于来自云游戏的非Move的消息，x,y 通常是空的 = 0，所以我们需要使用当前坐标，使得后续处理计算 PrevPos、和 delta 的数据正确变化。
                var pos = mouseState.Pos;
                mouseState.SetPos(pos.x, pos.y);
            }

            switch (action)
            {
                case MouseAction.DOWN:
                case MouseAction.UP:
                    // 按下或抬起时，更新`buttons`的对应位
                    var bitVar = (ushort)(action == MouseAction.DOWN ? 1 : 0);
                    var bitPos = (int)data.button;
                    if (data.button == MouseButtonId.LEFT)
                        mouseState.LeftDown = action == MouseAction.DOWN;
                    if (bitPos >= 0 && bitPos < 8)
                    {
                        var mask = ~(1 << bitPos);
                        mouseState.buttons = (ushort)((mouseState.buttons & mask) | (bitVar << bitPos));
                    }

                    break;
                case MouseAction.MOVE:
                    mouseState.SetPos(pos_x, pos_y);
                    mouseState.ViewPosition = viewPosition;
                    break;
            }

            if (action == MouseAction.DOWN)
                UpdateClickCount(mouseState, action, data.button, Time.unscaledTime);
        }

        private void UpdateClickCount(LocalMouseState mouseState, MouseAction action, MouseButtonId button, float nowTime)
        {
            if (action != MouseAction.DOWN)
                return;

            var lastButon = mouseState.lastClickButton;
            var lastTime = mouseState.lastClickTime;
            if (lastButon == button && nowTime - lastTime <= DoubleClickThresh)
                mouseState.clickCount += 1;
            else
                mouseState.clickCount = 1;
            mouseState.lastClickButton = button;
            mouseState.lastClickTime = nowTime;
        }

        public void ProcessMouseEvent(CloudMouseData data)
        {
            // 云游戏会传入state为MousePositionState.RELATIVE且position为0，0的数据，导致一些鼠标悬浮效果消失
            if (data.state == MousePositionState.RELATIVE)
            {
                return;
            }

            InputMouseFrame = InputFrame;
            UpdateMouseState(data);

            var resolution = _device.Screen.Resolution;
            var pos = _mouseState.Pos;
            var delta = _mouseState.delta;
            var button = (int)data.button;
            var buttons = _mouseState.buttons;
            var clickCount = _mouseState.clickCount;
            if (CloudGameSdk.IsVerboseLogForInput)
            {
                Debug.Log($"Input Mouse {data.action} ({(int)data.action}), button: {button} ({buttons}), x,y: {data.x:F4},{data.y:F4}, pos: {pos:F0}" +
                          $", delta: {delta:F0}, click: {clickCount}, resolution: {resolution:F0}, frame: {Time.frameCount}");
            }

            var wheel = data.wheel;
            // 兼容新旧版本 滚轮
            if (data.action == MouseAction.SCROLL)
                wheel = data.axis_v;

            var mouseEvent = new RemoteMouseEvent
            {
                device = _device,
                action = data.action,
                position = pos,
                viewPosition = _mouseState.ViewPosition,
                delta = delta,
                buttons = buttons,
                buttonId = data.button,
                displayIndex = (ushort)DisplayIndex,
                clickCount = clickCount,
                wheel = wheel
            };
            OnMouse?.Invoke(mouseEvent);
        }

        // MARK: - Keyboard

        private void ProcessMobileKeyboardEvent(MobileKeyAction action, int mobile_key_code)
        {
            var key = CloudMobileKeycode.ToUnityKeyCode(mobile_key_code);
            if (CloudGameSdk.IsVerboseLogForInput)
                Debug.Log($"Input MobileKey {action} ({(int)action}), UnityKey: {key}, key_code: {mobile_key_code}");
            if (key == KeyCode.None)
            {
                return;
            }

            switch (action)
            {
                case MobileKeyAction.DOWN:
                    SetPressedKey(key, true);
                    var modifiers = GetModifiers();
                    var character = EnableKeyboardCharacter ? CloudUnityKeyCode.ToCharacter(key, modifiers) : '\0';
                    OnKeyboard?.Invoke(new RemoteKeyboardEvent
                    {
                        device = _device,
                        key = key,
                        action = KeyboardAction.DOWN,
                        modifiers = modifiers,
                        character = character
                    });
                    break;
                case MobileKeyAction.UP:
                    SetPressedKey(key, false);
                    OnKeyboard?.Invoke(new RemoteKeyboardEvent
                    {
                        device = _device,
                        key = key,
                        action = KeyboardAction.UP,
                    });
                    break;
            }
        }

        // param `op_key_code` 真实环境来自云游戏 PlayerOperate event_data. 若是模拟环境，默认-1.
        public void ProcessPCKeyboardEvent(PCKeyboardAction action, KeyCode key, int op_key_code = -1)
        {
            if (CloudGameSdk.IsVerboseLogForInput)
                Debug.Log($"Input PCKey {action} ({(int)action}), UnityKey: {key}, key_code: {op_key_code}");
            if (key == KeyCode.None)
            {
                return;
            }

            switch (action)
            {
                case PCKeyboardAction.DOWN:
                    SetPressedKey(key, true);
                    var modifiers = GetModifiers();
                    var character = EnableKeyboardCharacter ? CloudUnityKeyCode.ToCharacter(key, modifiers) : '\0';
                    OnKeyboard?.Invoke(new RemoteKeyboardEvent
                    {
                        device = _device,
                        key = key,
                        action = KeyboardAction.DOWN,
                        modifiers = modifiers,
                        character = character
                    });
                    break;
                case PCKeyboardAction.UP:
                    SetPressedKey(key, false);
                    OnKeyboard?.Invoke(new RemoteKeyboardEvent
                    {
                        device = _device,
                        key = key,
                        action = KeyboardAction.UP,
                    });
                    break;
            }
        }

        public bool GetKey(int keyCode) => _mPressedKey.Contains((KeyCode)keyCode);

        public bool EnableKeyboardCharacter { get; set; } = true;

        private bool SetPressedKey(KeyCode key, bool isDown)
        {
            bool ret;
            if (isDown)
                ret = _mPressedKey.Add(key);
            else
                ret = _mPressedKey.Remove(key);
            _mPressedModifiers = UpdateModifiers(_mPressedKey);
            return ret;
        }

        private EventModifiers UpdateModifiers(HashSet<KeyCode> set)
        {
            EventModifiers ret = EventModifiers.None;
            if (set.Contains(KeyCode.LeftShift) || set.Contains(KeyCode.RightShift))
                ret |= EventModifiers.Shift;
            if (set.Contains(KeyCode.LeftControl) || set.Contains(KeyCode.RightControl))
                ret |= EventModifiers.Control;
            if (set.Contains(KeyCode.LeftAlt) || set.Contains(KeyCode.RightAlt))
                ret |= EventModifiers.Alt;
            if (set.Contains(KeyCode.LeftCommand) || set.Contains(KeyCode.RightCommand))
                ret |= EventModifiers.Command;

            if (set.Contains(KeyCode.Numlock) || set.Contains(KeyCode.Numlock))
                ret |= EventModifiers.Numeric;
            if (set.Contains(KeyCode.CapsLock) || set.Contains(KeyCode.CapsLock))
                ret |= EventModifiers.CapsLock;

            return ret;
        }

        private EventModifiers GetModifiers() => _mPressedModifiers;
    }
}
