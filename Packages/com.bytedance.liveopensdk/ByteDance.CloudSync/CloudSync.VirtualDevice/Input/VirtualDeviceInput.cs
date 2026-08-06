using System;
using System.Collections.Generic;
using UnityEngine;

namespace ByteDance.CloudSync
{
    /// <summary>
    /// 云设备用户输入
    /// </summary>
    public interface IVirtualDeviceInput
    {
        bool Enable { get; set; }

        IRemoteInput RemoteInput { get; }

        /// <summary>
        /// 处理云游戏发来的用户输入事件（鼠标、键盘、触摸等）。
        /// </summary>
        /// <remarks>
        /// 事件数据来源为用户在直播端上玩法画面中的操作输入，经过RTC流，传到云端Pod的游戏进程。
        /// </remarks>
        void ProcessInput(PlayerOperate operate);

        void ProcessMouseEvent(CloudMouseData data);
        void ProcessTouchEvent(IList<CloudTouchData> touchesData);
        void ProcessKeyboardEvent(KeyboardAction action, KeyCode keyCode);

        void EnableDebugger();

        /// <summary>
        /// 当前鼠标是否经过 UI Object
        /// 同 EventSystem.Current.IsPointerOverGameObject()
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException">注意，FGUI 暂未实现</exception>
        bool IsPointerOverGameObject()
        {
            throw new NotImplementedException("IsPointerOverGameObject not implemented!");
        }

        void Close();
    }
}