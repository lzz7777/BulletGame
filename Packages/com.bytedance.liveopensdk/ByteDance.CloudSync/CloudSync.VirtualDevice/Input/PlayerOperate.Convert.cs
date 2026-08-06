// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using UnityEngine;

namespace ByteDance.CloudSync
{
    public static class PlayerOperateExtension
    {
        public static string ToCnString(this MouseButtonId buttonId)
        {
            return buttonId switch
            {
                MouseButtonId.LEFT => "左键",
                MouseButtonId.RIGHT => "右键",
                MouseButtonId.MIDDLE => "中键",
                _ => string.Empty
            };
        }
    }

    public partial class CloudTouchData
    {
        internal RemoteTouch ToRemoteTouch(Vector2Int resolution, int touchIdOffset)
        {
            ToActionAndPhase(out var touchAction, out var phase);
            if (phase == RemoteTouchPhase.None)
                return new RemoteTouch();

            var touchId = pointerId + touchIdOffset;
            var px = (int)(x * resolution.x);
            var py = (int)((1 - y) * resolution.y);
            var pos = new Vector2Int(px, py);
            var viewPosition = new Vector2((float)x, (float)(1 - y));

            var touch = new RemoteTouch
            {
                touchId = touchId,
                phase = phase,
                action = touchAction,
                position = pos,
                viewPosition = viewPosition,
            };
            return touch;
        }

        private void ToActionAndPhase(out TouchAction touchAction, out RemoteTouchPhase phase)
        {
            touchAction = TouchAction.CANCEL;
            phase = RemoteTouchPhase.None;
            var dataAction = action;
            if (dataAction > 4)
            {
                int pointer = dataAction / 256;
                if (pointerId != pointer)
                    return;
                dataAction %= 256;
            }

            touchAction = (TouchAction)dataAction;
            phase = ToRemoteTouchPhase(touchAction);
        }

        private static RemoteTouchPhase ToRemoteTouchPhase(TouchAction touchAction)
        {
            return touchAction switch
            {
                TouchAction.DOWN => RemoteTouchPhase.Began,
                TouchAction.UP => RemoteTouchPhase.Ended,
                TouchAction.MOVE => RemoteTouchPhase.Moved,
                TouchAction.CANCEL => RemoteTouchPhase.Canceled,
                TouchAction.OUTSIDE => RemoteTouchPhase.Stationary,
                TouchAction.MULTI_DOWN => RemoteTouchPhase.Began,
                TouchAction.MULTI_UP => RemoteTouchPhase.Ended,
                _ => RemoteTouchPhase.None
            };
        }

        internal static CloudTouchData Create(Touch touch, Vector2 screenSize)
        {
            // `UnityEngine.Touch` position 左上角坐标 ( 0, Screen.Height ) 原点在左下角，值范围 0~Screen宽高
            // `CloudTouchData` x,y 左上角坐标 ( 0, 0 ) 原点在左上角，值范围 0~1.0
            var x = (double)touch.position.x / screenSize.x;
            var y = 1 - (double)touch.position.y / screenSize.y;
            return new CloudTouchData
            {
                x = x,
                y = y,
                action = ToDataAction(touch.phase, touch.fingerId),
                pointerId = touch.fingerId
            };
        }

        private static int ToDataAction(TouchPhase phase, int pointerId)
        {
            var cloudAction = phase switch
            {
                TouchPhase.Began => TouchAction.DOWN,
                TouchPhase.Moved => TouchAction.MOVE,
                TouchPhase.Ended => TouchAction.UP,
                TouchPhase.Canceled => TouchAction.CANCEL,
                TouchPhase.Stationary => TouchAction.MOVE,
                _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, null)
            };

            var action = ToDataAction(cloudAction, pointerId);
            return action;
        }

        private static int ToDataAction(TouchAction cloudAction, int pointerId)
        {
            var action = (int)cloudAction;
            if (pointerId <= 0)
                return action;
            action = cloudAction switch
            {
                // index << 8 | ACTION_POINT_DOWN
                TouchAction.DOWN => (int)TouchAction.MULTI_DOWN + 256 * pointerId,
                TouchAction.UP => (int)TouchAction.MULTI_UP + 256 * pointerId,
                _ => action
            };

            return action;
        }
    }
}