using System;
using UnityEngine;

namespace ByteDance.CloudSync.CloudGameAndroid
{
    public class MultiplayerScene : CloudGameScene
    {
        private static string TAG = "MultiplayerScene";
        internal static AndroidJavaClass MultiplayerSceneJavaClass { get; set; } = new AndroidJavaClass("com.bytedance.cloudplay.gamesdk.api.scene.MultiplayerScene");
        internal MultiplayerScene(AndroidJavaObject ajo) : base(ajo) { }

        public bool InitRendering(int width, int height)
        {
            return LogUtils.WrapExceptionLog(() => SceneJavaObject.Call<bool>("init", width, height), TAG);
        }

        public void ReleaseRendering()
        {
            LogUtils.WrapExceptionLog(() => SceneJavaObject.Call("release"), TAG);
        }

        public void UpdateTexture(int roomIndex, int textureId)
        {
            LogUtils.WrapExceptionLog(() => SceneJavaObject.Call("updateTexture", roomIndex, textureId), TAG);
        }

        public void QueryRoomInfo(int roomIndex)
        {
            LogUtils.WrapExceptionLog(() => SceneJavaObject.Call("queryRoomInfo", roomIndex), TAG);
        }

        public void SendEnterRoomRequest(int roomIndex)
        {
            LogUtils.WrapExceptionLog(() => SceneJavaObject.Call("playerJoin", roomIndex), TAG);
        }

        public void SendLeaveRoomRequest(string roomIndex)
        {
            LogUtils.WrapExceptionLog(() => SceneJavaObject.Call("playerExit", roomIndex), TAG);
        }

        public void DestroyRoom()
        {
            LogUtils.WrapExceptionLog(() => SceneJavaObject.Call("destroyRoom"), TAG);
        }

        public void SendPodQuit()
        {
            LogUtils.WrapExceptionLog(() => SceneJavaObject.Call("sendPodQuit"), TAG);
        }

        public void SetAudioEnabled(int roomIndex, bool enabled)
        {
            LogUtils.WrapExceptionLog(() => SceneJavaObject.Call("setAudioEnabled", roomIndex, enabled), TAG);
        }

        public string SendCustomMessage(int roomIndex, string message)
        {
            return LogUtils.WrapExceptionLog(() => SceneJavaObject.Call<string>("sendCustomMessage",roomIndex, message), TAG);
        }

        public void SetSceneListener(OnRenderReady onRenderReady,
            OnSendCustomMessageResult onSendCustomMessageResult, OnReceiveCustomMessage onReceiveCustomMessage,
            OnGameStart onGameStart, OnQueryRoomInfoResult onQueryRoomInfoResult
            , OnPlayerJoin onPlayerJoin, OnPlayerExit onPlayerExit, OnPlayerOperate onPlayerOperate,
            OnPlayerInput onPlayerInput)
        {
            LogUtils.WrapExceptionLog(() => SceneJavaObject.Call("setSceneListener", new MultiplayerSceneListener(
                onRenderReady, onSendCustomMessageResult, onReceiveCustomMessage, onGameStart, onQueryRoomInfoResult,
                onPlayerJoin, onPlayerExit, onPlayerOperate, onPlayerInput)), TAG);
        }
    }
}