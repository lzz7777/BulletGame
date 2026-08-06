using System;
using System.Collections.Generic;
using UnityEngine;
using UnityWebSocket;

namespace XN
{
    /// <summary>
    /// GM 命令
    /// </summary>
    public class GMAuthRequest
    {
        // 命令ID
        public long CMDID = 1;

        /// <summary>
        /// 房间ID
        /// </summary>
        public string RoomID;
    }
    
    public class GMAuthResponse
    {
        /// <summary>
        /// 成功或失败, 0:成功， -1 :失败
        /// </summary>
        public int State;
    }
    
    public class GMCmd
    {
        /// <summary>
        /// 玩家ID
        /// </summary>
        public string PlayerId;

        /// <summary>
        /// 命令参数
        /// </summary>
        public string cmd;
    }
    public class SocketManager : MonoSingleton<SocketManager>
    {
        /// <summary>
        /// "wss://{我方服务器}:{端口}"
        /// </summary>
        private string address;
        private GMAuthRequest _authRequest;
        private IWebSocket socket;

        private int pingTime=3;
        public DateTime LastSendTime = DateTime.Now;

        private void Update()
        {
            // if (!GameStateCtrl.IsGameAllState) return;   // 这里不能这样会断心跳
            if (socket != null && socket.ReadyState == WebSocketState.Open)
            {
                TimeSpan timeSpan = DateTime.Now - LastSendTime;
                if (timeSpan.Seconds > pingTime)
                {
                    SendPine();
                }
            }
        }

        protected override void OnInit()
        {
        }

        public void InitSocket(string roomId)
        {
            address = TotalConfigManager.ConfigManager.ConstConfigCategory.SocketUrl;
            _authRequest = new GMAuthRequest()
            {
                CMDID = 1,
                RoomID = roomId,
            };
            CloseSocket();
            ReOpen();
        }

        public void ReOpen()
        {
            LastSendTime = DateTime.Now;
            socket = new WebSocket(address);
            socket.OnOpen += OnOpen;
            socket.OnMessage += OnMessageRecv;
            socket.OnClose += OnClose;
            socket.OnError += OnError;
            Debug.Log("Socket Connect....."+address);
            socket.ConnectAsync();
        }

        private void OnOpen(object sender, OpenEventArgs e)
        {
            if (!string.IsNullOrEmpty(_authRequest?.RoomID))
            {
                Debug.Log(socket.ReadyState);
                var jsonStr = JsonUtility.ToJson(_authRequest);
                LastSendTime = DateTime.Now;
                Debug.Log("Socket OpenSend....."+jsonStr);
                socket.SendAsync(jsonStr);
            }
        }

        private void OnMessageRecv(object sender, MessageEventArgs e)
        {
            if (e.IsBinary)
            {
                Debug.Log(string.Format("Socket OnMessageReceive Bytes: {0}",e.Data));
                GMCmd cmd = JsonUtility.FromJson<GMCmd>(e.Data);
                if (cmd != null)
                {
                    CmdManager.Instance.GMCmd(cmd.PlayerId, cmd.cmd);
                }
            }
            else if (e.IsText)
            {
                Debug.Log(string.Format("Socket OnMessageReceive: {0}",e.Data));
            }
        }
        
        private void OnClose(object sender, CloseEventArgs e)
        {
            Debug.LogWarning($"Socket OnClose: code={e.StatusCode}, reason={e.Reason}");
            socket = null;
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            Debug.LogWarning($"Socket OnError: {e.Message}");
            socket = null;
            // ReOpen();
        }

        protected override void OnRemove()
        {
            CloseSocket();
            socket = null;
        }

        public void CloseSocket()
        {
            if (socket != null && socket.ReadyState != WebSocketState.Closed)
            {
                LastSendTime = DateTime.Now;
                socket.CloseAsync();
            }
        }
        
        private void OnApplicationQuit()
        {
            CloseSocket();
        }


        public void SendPine()
        {
            if (socket != null)
            {
                string jsonStr = "{}";
                LastSendTime = DateTime.Now;
                Debug.Log("Socket Send ping....." + LastSendTime.ToLocalTime());
                socket.SendAsync(jsonStr);
            }
        }
    }

}
