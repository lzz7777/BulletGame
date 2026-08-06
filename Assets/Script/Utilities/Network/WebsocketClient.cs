
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using BestHTTP.JSON;
using BestHTTP.WebSocket;
using Google.Protobuf;
using UnityEngine;


namespace Script.Utilities.Network
{
    public class RequestRecord
    {
        public RequestParam requestParam;
        public byte[] sendBuffer;
        public DateTime sendTime;
        public DateTime receiveTime;
        public int session;
        public Dictionary<string, object> m;
    }

    public class RequestParam
    {
        public NetMessageInfo messageInfo;
        public Dictionary<string, object> messageData;
        public Dictionary<string, object> extraData;

        public RequestParam(NetMessageInfo messageInfo, Dictionary<string, object> messageData, Dictionary<string, object> extraData = null)
        {
            this.messageInfo = messageInfo;
            this.messageData = messageData;
            this.extraData = extraData;
        }
    }
    
    public class NetMessageInfo
    {
        public int id;
        public string name;
        public string pb;
        public string nm_type;
        public string desc;

        public NetMessageInfo(string name, string pb, string nm_type, string desc, int id)
        {
            this.name = name;
            this.pb = pb;
            this.nm_type = nm_type;
            this.desc = desc;
            this.id = id;
        }
    }
    
    public class WsClient
    {
        static public List<RequestParam> g_requestList = new List<RequestParam>();//全局请求队列
        static public List<RequestRecord> g_requestPackageList = new List<RequestRecord>();//全局请求队列
        static public int g_session = 100;
        const int pingTime = 5;
        const int reqTimeOut = 3;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void reload()
        {
            g_requestList.Clear();
            g_requestPackageList.Clear();
            g_session = 100;
        }
        public event Action<RequestRecord> err_handler;
        public event Action<RequestRecord> on_message;
        public event Action<bool> on_auth_succeed;
        public event Action<string> on_auth_err;
        public event Action<int> on_timeout;
        public event Action<string> on_kick;
        public event Action<string> on_logout;
        public event Action<RequestRecord> on_push;
        public Dictionary<string, Action<RequestRecord>> msg_handler = new Dictionary<string, Action<RequestRecord>>();

        const int OPT_STR_DATA = 1;
        const int OPT_STR_PING = 2;
        const int OPT_STR_COMPRESS_DATA = 3;
        const int OPT_STR_CMD = 5;
        const int OPT_STR_DATA_SUB_BEGIN = 6;
        const int OPT_STR_DATA_SUB = 7;
        const int OPT_STR_DATA_SUB_END = 8;

        const UInt16 optPine = 0;
        const UInt16 optPush = 1;
        const UInt16 optRequest = 2;
        const UInt16 optKick = 3;
        const UInt16 optLogout = 4;
        const UInt16 optPingTime = 5;


        WebSocket webSocket;
        
        // SYWs gameWs;
        byte[] secret;
        DateTime autuSendTime;
        byte[] subid;


        bool isAuth = false;

        bool isTimeout = false;
        public int timeoutCount = 0;
        int ver = 0;


        string authHost;
        int authPort;
        string gameHost;
        int gamePort;
        int aid;
        int player_id;
        string token;
        bool isLoadAll;

        int session = g_session;
        Dictionary<int, RequestRecord> requestRecordDic = new Dictionary<int, RequestRecord>();//请求记录
        Dictionary<int, RequestRecord> requestRecordDicEx = new Dictionary<int, RequestRecord>();//请求记录(带清除)

        public List<RequestParam> requestList = new List<RequestParam>();//请求队列

        private List<byte> cacheBuff = new List<byte>(); //大包处理缓存

        public DateTime lastSendTime = DateTime.Now;

        public void Update(double dt)
        {
            if (!isTimeout)
            {
                TimeSpan timeSpan = DateTime.Now - lastSendTime;
                if (timeSpan.TotalSeconds > pingTime)
                {
                    SendPine();
                }

                //检测验证超时
                if (!isAuth)
                {
                    TimeSpan d = DateTime.Now - autuSendTime;
                    if (d.TotalSeconds > reqTimeOut && !isTimeout)
                    {
                        isTimeout = true;
                        timeoutCount++;
                        Debug.LogWarning("检测验证超时:" + timeoutCount);
                        on_timeout?.Invoke(timeoutCount);
                        return;
                    }
                }
                else
                {
                    //检测请求超时
                    foreach (RequestRecord requestRecord in requestRecordDicEx.Values)
                    {
                        TimeSpan d = DateTime.Now - requestRecord.sendTime;
                        if (d.TotalSeconds > reqTimeOut && !isTimeout)
                        {
                            isTimeout = true;
                            timeoutCount++;
                            Debug.LogWarning("检测请求超时:" + timeoutCount + " requestRecord:" + requestRecord.requestParam.messageInfo.name);
                            on_timeout?.Invoke(timeoutCount);
                            break;
                        }
                    }
                }


            }

        }
        public void Open(string authHost, int authPort, string gameHost, int gamePort, int aid, int player_id, string token, bool isLoadAll = false)
        {
            isTimeout = false;
            this.authHost = authHost;
            this.authPort = authPort;
            this.gameHost = gameHost;
            this.gamePort = gamePort;
            this.aid = aid;
            this.player_id = player_id;
            this.token = token;
            this.isLoadAll = isLoadAll;
            OpenGeme();
        }

        public void Close(string msg = "")
        {
            Debug.LogWarning(timeoutCount + ":" + msg);

            webSocket?.Close(1000, msg);
            webSocket = null;
        }

        void OpenGeme()
        {
            ver++;
            isTimeout = false;
            isAuth = false;

            string url = string.Format("wss://{0}:{1}", gameHost, gamePort);
            autuSendTime = DateTime.Now;

            // 使用 BestHTTP.WebSocket 建立连接并注册事件
            webSocket = new WebSocket(new Uri(url));

            webSocket.OnOpen += (ws) =>
            {
                Auth();
            };
            webSocket.OnMessage += (ws, text) =>
            {
                if (!isAuth)
                {
                    // 简单的鉴权逻辑：服务端返回 "200 OK" 视为成功  TODO .... 
                    if (text == "200 OK")
                    {
                        isAuth = true;
                        isTimeout = false;
                        on_auth_succeed?.Invoke(isLoadAll);
                        // 鉴权成功后发送积压的请求
                        foreach (var rp in requestList)
                        {
                            SendRequest(rp);
                        }
                        requestList.Clear();
                    }
                    else
                    {
                        on_auth_err?.Invoke(text);
                    }
                }
                else
                {
                    // 文本消息直接转发
                    on_message?.Invoke(new RequestRecord { m = new Dictionary<string, object> { { "text", text } }, receiveTime = DateTime.Now });
                }
            };
            webSocket.OnBinary += (ws, data) =>
            {
                // 二进制包走现有解析流程
                OnPackage(data);
            };
            webSocket.OnClosed += (ws, code, message) =>
            {
                // 清理与回调收敛（保持与原流程一致但更简）
                // TODO 与wsclient 相比， g_requestList、g_requestPackageList 没有处理这个
                requestRecordDic.Clear();
                requestRecordDicEx.Clear();
                webSocket = null;

                err_handler = null;
                on_message = null;
                on_auth_succeed = null;
                on_auth_err = null;
                on_timeout = null;
                on_kick = null;
                on_logout = null;
                on_push = null;
            };
            webSocket.OnError += (ws, reason) =>
            {
                Debug.LogWarning("WebSocket Error: " + reason);
            };

            webSocket.Open();
        }

        void Auth()
        {
            isAuth = false;
            autuSendTime = DateTime.Now;
            // gameWs.SendStr(token);
            webSocket?.Send(token);
        }

        void OnPackage(byte[] buffer)
        {
            int opt = buffer[0];
            byte[] data = new byte[buffer.Length - 2];
            Array.Copy(buffer, 2, data, 0, buffer.Length - 2);
            switch (opt)
            {   
                case OPT_STR_DATA:
                    ProcessData(data);
                    break;
                case OPT_STR_COMPRESS_DATA:
                    ProcessData(data);
                    break;
                case OPT_STR_DATA_SUB_BEGIN:
                    cacheBuff.AddRange(data);
                    break;
                case OPT_STR_DATA_SUB:
                    cacheBuff.AddRange(data);
                    break;
                case OPT_STR_DATA_SUB_END:
                    cacheBuff.AddRange(data);
                    ProcessData(cacheBuff.ToArray());
                    cacheBuff.Clear();
                    break;

                default:
                    break;
            }


        }

        void ProcessData(byte[] buffer)
        {
            int op_id = buffer[0]*256 + buffer[1];
            int msg_id = buffer[2] * 256 + buffer[3];
            int is_lz4 = buffer[4];
            byte[] msg;
            //Debug.Log.e("is_lz4 =" + is_lz4 + " msg_id = " + msg_id);
            if (is_lz4 == 1)
            {
                byte[] msgLz4 = new byte[buffer.Length - 5 - 5 - 4];
                int customLength = buffer[buffer.Length - 6] + buffer[buffer.Length - 7] * 256 + buffer[buffer.Length - 8] * 255 * 255 + buffer[buffer.Length - 9] * 255 * 255 * 255;
                Array.Copy(buffer, 5, msgLz4, 0, msgLz4.Length);


                //msg = LZ4.decompressBuffer(msgLz4, false, customLength);
                msg = new byte[customLength];
            }
            else
            {
                //Debug.Log.w(buffer.Length);
                msg = new byte[buffer.Length - 5 - 5];
                Array.Copy(buffer, 5, msg, 0, buffer.Length - 5 - 5);
            }

            
            int ok = buffer[buffer.Length - 5];
            int session = buffer[buffer.Length - 4] * 256 * 256 * 256
                + buffer[buffer.Length - 3] * 256 * 256
                + buffer[buffer.Length - 2] * 256
                + buffer[buffer.Length - 1];
            

            switch (op_id)
            {
                case optPine:
                    break;
                case optPush:
                    OnMessage(msg_id, session, msg, true);
                    break;
                case optRequest:
                    // UIMgr.RemoveNetMaskRequestRecord(session);
                    OnMessage(msg_id, session, msg);
                    break;
                case optKick:
                    // UIMgr.NetMaskClear();
                    on_kick?.Invoke(System.Text.Encoding.UTF8.GetString(msg));
                    //Close("kick");
                    break;
                case optLogout:
                    // UIMgr.NetMaskClear();
                    on_logout?.Invoke("");
                    Close("logout");
                    break;
                default:
                    break;
            }

            
        }

        void OnMessage(int msg_id, int session, byte[] msg, bool isPush = false)
        {
            //时间校准
            //M.offsetTime = data.T;
            //M.offsetTimePrecise = data.PreciseT;

            RequestRecord requestRecord;
            if (!isPush)
            {
                requestRecord = requestRecordDic[session];
                requestRecord.receiveTime = DateTime.Now;
                requestRecordDicEx.Remove(session);
            }
            else
            {
                // NetMessageInfo msg_info = MN.GetMsg(msg_id);
                NetMessageInfo msg_info = new NetMessageInfo($"PUSH_{msg_id}", "NULL", "", "", msg_id);
                requestRecord = new RequestRecord();
                requestRecord.requestParam = new RequestParam(msg_info, new Dictionary<string, object>());
            }

            Dictionary<string, object> root = null;
            try
            {
                var text = Encoding.UTF8.GetString(msg);
                root = Json.Decode(text) as Dictionary<string, object>;
                Debug.LogWarning($"OnMessage JSON decode {root}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"OnMessage JSON decode failed: {ex.Message}");
            }
            
//             // TODO  error code 处理
//             if (e != EC.Get("OK").id)
//             {
//                 Debug.LogWarning("e:" + e);
//                 err_handler?.Invoke(requestRecord);
//                 Debug.LogWarning(Json.Decode(data.M));
//             }
//             else
//             {
//                 Dictionary<string, object> m = Json.Decode(data.M) as Dictionary<string, object>;
//                 requestRecord.m = m ?? new Dictionary<string, object>();
//                 NetMessageInfo messageInfo = requestRecord.requestParam.messageInfo;
//
//                 //int msg_id = messageInfo.id;
//                 string nm_type = messageInfo.nm_type;
//
//                 if(nm_type.Equals("INITDATA"))
//                 {
//                     // M.ResetData(m);
//                 }
//
//                 if(data.ChangeTab != null)
//                 {
//                     // M.UpdateData(data.ChangeTab, requestRecord);
//                 }
//
//                 DateTime currentTime = DateTime.Now;
//                 DateTime epochTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
//                 TimeSpan timeSpan = currentTime.ToUniversalTime() - epochTime;
//                 int timestamp = (int)timeSpan.TotalSeconds;
//                 // M.SetOffsetTime(data.T - timestamp);
//
//                 on_message?.Invoke(requestRecord);
//                 if(isPush)
//                 {
//                     on_push?.Invoke(requestRecord);
//                 }
//
//                 if (msg_handler.ContainsKey(messageInfo.name))
//                 {
//
// #if UNITY_EDITOR
//                     Debug.Log($"OnMessage ==== {messageInfo.name} {messageInfo.desc} " +
//                         $"{Json.Encode(requestRecord.m)}");
// #endif
//
//                     msg_handler[messageInfo.name](requestRecord);
//                 }
//
//             }
        }
        
        public void SendRequest(RequestParam requestParam)
        {
            if (!isAuth)
            {
                requestList.Add(requestParam);
            }
            else
            {
                NetMessageInfo message_info = requestParam.messageInfo;
                int msg_id = requestParam.messageInfo.id;

                session++;
                g_session = session;

                int _session = session;
                if (msg_id >= 201 && msg_id <= 230)
                {
                    _session = msg_id - 200;
                }

                RequestRecord requestRecord = new RequestRecord();
                requestRecord.requestParam = requestParam;
                requestRecord.sendTime = DateTime.Now;
                requestRecord.session = _session;
                requestRecordDic[_session] = requestRecord;
                requestRecordDicEx[_session] = requestRecord;

                byte[] msg;
                int size = 0;
                if(message_info.pb.Equals("NULL"))
                {
                    string json = Json.Encode(requestParam.messageData);
                    msg = System.Text.Encoding.UTF8.GetBytes(json);
                    size = 2 + 2 + msg.Length + 4;
                }
                else
                {
                    string json = Json.Encode(requestParam.messageData);
                    msg = System.Text.Encoding.UTF8.GetBytes(json);
                    size = 2 + 2 + msg.Length + 4;
                }


                byte[] buffer = new byte[size];
                byte[] bufferOpt = System.BitConverter.GetBytes((UInt16)(optRequest));
                byte[] bufferCmd = System.BitConverter.GetBytes((UInt16)msg_id);
                byte[] bufferSession = System.BitConverter.GetBytes(_session);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(bufferOpt);
                    Array.Reverse(bufferCmd);
                    Array.Reverse(bufferSession);
                }

                BinaryWriter br = new BinaryWriter(new MemoryStream(buffer));
                br.Write(bufferOpt);
                br.Write(bufferCmd);
                br.Write(msg);
                br.Write(bufferSession);
                // gameWs?.Send(buffer);
                webSocket?.Send(buffer);
                requestRecord.sendBuffer = buffer;

                // UIMgr.AddNetMaskRequestRecord(_session, requestRecord);
                lastSendTime = DateTime.Now;
            }

        }


        public void SendPine()
        {
            // if(gameWs != null)
            if (webSocket != null)
            {
                session++;
                g_session = session;

                byte[] buffer = new byte[8];
                byte[] bufferOpt = System.BitConverter.GetBytes((UInt16)(optPine));
                byte[] bufferCmd = System.BitConverter.GetBytes((UInt16)0);
                byte[] bufferSession = System.BitConverter.GetBytes(session);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(bufferOpt);
                    Array.Reverse(bufferCmd);
                    Array.Reverse(bufferSession);
                }

                BinaryWriter br = new BinaryWriter(new MemoryStream(buffer));
                br.Write(bufferOpt);
                br.Write(bufferCmd);
                br.Write(bufferSession);
                // gameWs.Send(buffer);
                webSocket.Send(buffer);
                lastSendTime = DateTime.Now;
            }
            
        }
        public void RegisterNetCallback(string key, Action<RequestRecord> f)
        {
            Debug.Assert(!msg_handler.ContainsKey(key), "Have already included:" + key);
            msg_handler.Add(key, f);
        }
    }
}