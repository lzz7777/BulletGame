// Copyright@www.bytedance.com
// Author: zhouxu.ken
// Date: 2024/06/08
// Description:

namespace ByteDance.CloudSync
{
    /// <summary>
    /// IMultiplayerListener 线程安全转换器
    /// </summary>
    internal class SafeMultiplayerListener : IMultiplayerListener, ISafeActionsUpdatable
    {
        private SafeActionsProxy _proxy;
        private readonly IMultiplayerListener _inner;
        public SafeMultiplayerListener(IMultiplayerListener inner) 
        {
            _proxy = new SafeActionsProxy();
            _inner = inner;
        }

        // todo: split Input actions (playerOperate) for UnityThreadListener, and process dequeue in EarlyUpdate.
        public void Update()
        {
            _proxy.Update();
        }

        public void OnGameStart(string[] cloudRoomList)
        {
            _proxy.RunOnUnity(() => _inner.OnGameStart(cloudRoomList));
        }

        public void OnPlayerJoin(int roomIndex, JoinRoomParam param)
        {
            _proxy.RunOnUnity(() => _inner.OnPlayerJoin(roomIndex, param));
        }

        public void OnPlayerExit(int roomIndex, ExitRoomParam param)
        {
            _proxy.RunOnUnity(() => _inner.OnPlayerExit(roomIndex, param));
        }

        public void OnPlayerOperate(int roomIndex, string opData)
        {
            _proxy.RunOnUnity(() => _inner.OnPlayerOperate(roomIndex, opData));
        }

        public void OnTexturePush(long shareHandle)
        {
            _proxy.RunOnUnity(() => _inner.OnTexturePush(shareHandle));
        }

        public void OnPlayerInput(InputEventResponse res)
        {
            _proxy.RunOnUnity(() => _inner.OnPlayerInput(res));
        }

        public void OnCustomMessage(int roomIndex, string msg)
        {
            _proxy.RunOnUnity(() => _inner.OnCustomMessage(roomIndex, msg));
        }

        public void OnQueryRoomInfo(int roomIndex, JoinRoomParam param)
        {
            _proxy.RunOnUnity(() => _inner.OnQueryRoomInfo(roomIndex, param));
        }
    }
}