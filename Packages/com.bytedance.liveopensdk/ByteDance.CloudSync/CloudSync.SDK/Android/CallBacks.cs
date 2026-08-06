using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ByteDance.CloudSync.CloudGameAndroid
{
    #region InitCallBack
    public delegate void InitCallBack(bool isSuccess);
    internal class InitCallBackProxy : AndroidJavaProxy
    {
        private InitCallBack _callback;
        public InitCallBackProxy(InitCallBack callback) : base("com.bytedance.cloudplay.gamesdk.api.base.InitCallBack")
        {
            _callback = callback;
        }
        public void onResult(AndroidJavaObject result)
        {

            bool isSuccess = result.Call<bool>("isSuccess");
            string msg = result.Get<string>("message");
            Debug.Log($"result isSuccess: {isSuccess}, msg : {msg}");
            _callback.Invoke(isSuccess);
        }

    }
    #endregion


    #region PluginListener

    public delegate void OnLoadSuccess();
    public delegate void OnLoadFailed();
    internal class LoadPluginCallBackProxy : AndroidJavaProxy
    {
        private OnLoadSuccess _onLoadSuccessCallback;
        private OnLoadFailed _onLoadFailedCallback;

        public LoadPluginCallBackProxy(OnLoadSuccess onLoadSuccessCallback, OnLoadFailed onLoadFailedCallback) : base("com.bytedance.cloudplay.gamesdk.api.PluginListener")
        {
            _onLoadSuccessCallback = onLoadSuccessCallback;
            _onLoadFailedCallback = onLoadFailedCallback;
        }
        public void onLoadSuccess()
        {
            _onLoadSuccessCallback.Invoke();
        }

        public void onLoadFailed()
        {
            _onLoadFailedCallback.Invoke();
        }
    }
        #endregion


    #region AccountSceneListener
    public delegate void CheckTokenCallBack(bool isSuccess, string token);
    internal class AccountSceneListener : AndroidJavaProxy
    {
        private CheckTokenCallBack _callback;
        public AccountSceneListener(CheckTokenCallBack callback) : base("com.bytedance.cloudplay.gamesdk.api.scene.AccountScene$SceneListener")
        {
            _callback = callback;
        }
        public void onCheckTokenResult(AndroidJavaObject result, string token)
        {
            _callback.Invoke(result.Call<bool>("isSuccess"), token);
        }

        public void onCheckTokenResult(AndroidJavaObject result, string token, AndroidJavaObject extra)
        {
            _callback.Invoke(result.Call<bool>("isSuccess"), token);
        }

    }
    #endregion

    #region PaySceneListener
    public delegate void SendPayOrderCallBack(bool isSuccess, string orderId);
    public delegate void ClientPayCallBack(bool isSuccess, string orderId);
    internal class PaySceneListener : AndroidJavaProxy
    {
        private SendPayOrderCallBack _sendPayOrderCallBack;
        private ClientPayCallBack _clientPayCallBack;

        public PaySceneListener(SendPayOrderCallBack sendPayOrderCallBack, ClientPayCallBack clientPayCallBack) : base("com.bytedance.cloudplay.gamesdk.api.scene.PayScene$SceneListener")
        {
            _sendPayOrderCallBack = sendPayOrderCallBack;
            _clientPayCallBack = clientPayCallBack;

        }

        public void onSendPayOrderResult(AndroidJavaObject result, string orderId)
        {
            bool isSuccess = result.Call<bool>("isSuccess");
            string msg = result.Get<string>("message");

            Debug.Log($"onSendPayOrderResult result:{isSuccess},msg :{msg}");
            _sendPayOrderCallBack.Invoke(isSuccess, orderId);
        }

        public void onClientPayResult(AndroidJavaObject result, string orderId)
        {
            bool isSuccess = result.Call<bool>("isSuccess");
            string msg = result.Get<string>("message");

            Debug.Log($"onClientPayResult result:{isSuccess},msg :{msg}");
            _clientPayCallBack.Invoke(isSuccess, orderId);
        }
    }
    #endregion


    #region MultiplayerSceneListener

    public delegate void OnRenderReady();
    public delegate void OnSendCustomMessageResult(bool result, string messageId);
    public delegate void OnReceiveCustomMessage(int roomIndex, string messageId, string message);
    public delegate void OnGameStart(List<string> roomIdList);
    public delegate void OnQueryRoomInfoResult(int roomIndex, bool result, PlayerJoinExtra extra);

    public delegate void OnPlayerJoin(int roomIndex, string rtcUserId, bool result);
    public delegate void OnPlayerExit(int roomIndex, string rtcUserId, bool result);
    public delegate void OnPlayerOperate(int roomIndex, string input);
    public delegate void OnPlayerInput(InputEventResponse res);


    public class InputEventResponse {
        public int roomIndex;
        public string input;
    }

    public class PlayerJoinExtra
    {
        public string init_params;
        public string open_id;
        public string link_room_id;

        public override string ToString()
        {
            return $"initParams: {init_params}, openId :{open_id}, mLinkRoomId: {link_room_id}";
        }
    }

    public enum ExitRoomReason {
        UNKNOW,
        USER_EXIT
    }

    internal class MultiplayerSceneListener : AndroidJavaProxy
    {
        private OnRenderReady _onRenderReady;
        private OnSendCustomMessageResult _onSendCustomMessageResult;
        private OnReceiveCustomMessage _onReceiveCustomMessage;
        private OnGameStart _onGameStart;
        private OnQueryRoomInfoResult _onQueryRoomInfoResult;
        private OnPlayerJoin _onPlayerJoin;
        private OnPlayerExit _onPlayerExit;
        private OnPlayerOperate _onPlayerOperate;
        private OnPlayerInput _onPlayerInput;

        public MultiplayerSceneListener(OnRenderReady onRenderReady,
            OnSendCustomMessageResult onSendCustomMessageResult, OnReceiveCustomMessage onReceiveCustomMessage ,OnGameStart onGameStart, OnQueryRoomInfoResult onQueryRoomInfoResult
            ,OnPlayerJoin onPlayerJoin, OnPlayerExit onPlayerExit, OnPlayerOperate onPlayerOperate, OnPlayerInput onPlayerInput) : base("com.bytedance.cloudplay.gamesdk.api.scene.MultiplayerScene$SceneListener")
        {
            _onRenderReady = onRenderReady;
            _onSendCustomMessageResult = onSendCustomMessageResult;
            _onReceiveCustomMessage = onReceiveCustomMessage;
            _onGameStart = onGameStart;
            _onPlayerJoin = onPlayerJoin;
            _onPlayerExit = onPlayerExit;
            _onPlayerOperate = onPlayerOperate;
            _onPlayerInput = onPlayerInput;
            _onQueryRoomInfoResult = onQueryRoomInfoResult;
        }

        public void onRenderReady()
        {
            Debug.Log("onRenderReady");
            _onRenderReady.Invoke();
        }

        public void onSendCustomMessageResult(AndroidJavaObject result, String messageId)
        {
            Debug.Log("onSendCustomMessageResult");

            bool isSuccess = result.Call<bool>("isSuccess");
            string msg = result.Get<string>("message");

            _onSendCustomMessageResult(isSuccess, messageId);
        }

        public void onReceiveCustomMessage(int roomIndex, String messageId, String message)
        {
            Debug.Log("onReceiveCustomMessage");

            _onReceiveCustomMessage(roomIndex, messageId, message);
        }

        void onGameStart(AndroidJavaObject roomIdList)
        {
            Debug.Log("onGameStart");
            List<string> list = new List<string>();
            using (var iterator = roomIdList.Call<AndroidJavaObject>("iterator"))
            {
                while (iterator.Call<bool>("hasNext"))
                {
                    using (var item = iterator.Call<AndroidJavaObject>("next"))
                    {
                        list.Add(item.Call<string>("toString"));
                    }
                }
            }
            _onGameStart(list);
        }

        void onQueryRoomInfoResult(int roomIndex, AndroidJavaObject result, String extra)
        {
            Debug.Log("onQueryRoomInfoResult");
            bool isSuccess = result.Call<bool>("isSuccess");
            string msg = result.Get<string>("message");
            PlayerJoinExtra playerJoinExtra = JsonUtility.FromJson<PlayerJoinExtra>(extra);
            _onQueryRoomInfoResult.Invoke(roomIndex, isSuccess, playerJoinExtra);
        }


        void onPlayerJoin(int roomIndex, string rtcUserId, AndroidJavaObject result)
        {
            Debug.Log("onPlayerJoin");
            bool isSuccess = result.Call<bool>("isSuccess");
            string msg = result.Get<string>("message");
            _onPlayerJoin(roomIndex, rtcUserId, isSuccess);
        }

        void onPlayerExit(int roomIndex, string rtcUserId, AndroidJavaObject result)
        {
            Debug.Log("onPlayerExit");
            bool isSuccess = result.Call<bool>("isSuccess");
            string msg = result.Get<string>("message");
            _onPlayerExit(roomIndex, rtcUserId, isSuccess);
        }

        /**
         * 玩家操作回调
         * @param roomIndex 房间id
         * @param Input 操作指令数据
         */
        void onPlayerOperate(int roomIndex, string input)
        {
            _onPlayerOperate(roomIndex, input);
        }

        /**
         * 输入法回调
         * @param res 响应数据
         */
        void onPlayerInput(AndroidJavaObject res)
        {
            int roomIndex = res.Get<int>("roomIndex");
            string input = res.Get<string>("input");

            _onPlayerInput(new InputEventResponse()
            {
                roomIndex = roomIndex,
                input = input
            });
        }
    }

    #endregion

}
