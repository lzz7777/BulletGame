using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Timers;
using AOT;
using Newtonsoft.Json;
using ByteDance.CloudSync.CloudGameAndroid;
using UnityEngine;

namespace ByteDance.CloudSync
{
    internal class CloudGameAPIAndroid : ICloudGameAPI, ICloudGameAPIEx, ICloudGameMatchAPI
    {
        private const string TAG = "CloudGameAPIAndroid";
        public string FileVersion => "1.0";

        private const string MockedStartAppParam = @"";
        private static int SendMessageTimeoutInMillis = 10000;

        public static CloudGameAPIAndroid Instance => _instance;

        private static readonly CloudGameAPIAndroid _instance = new CloudGameAPIAndroid();

        public ISdkEnv SdkEnv { get; set; }
        public IMultiplayerListener MultiplayerListener => _multiplayerListener;

        private MultiplayerScene _multiplayerScene;

        private IMultiplayerListener _multiplayerListener;
        private IMatchAPIListener _matchAPIListener;
        // note: 存疑：这些线程安全码？ 没有用ConcurrentDictionary?
        private Dictionary<SeatIndex, long> _renderingTextureIDs = new();
        private Dictionary<int, int> _retryJoinCount = new();
        private Dictionary<int, int> _retryQueryCount = new();
        private Dictionary<int, string> _rtcUserIDs = new();
        private Dictionary<string, SendMessageRecord> _sendMsgTasks = new();
        private readonly object _sendMsgLock = new ();
        private readonly Stopwatch _stopwatch = new ();

        private class SendMessageRecord
        {
            public TaskCompletionSource<ICloudGameAPI.Response> CompletionSource { get; set; }
            public SeatIndex RoomIndex { get; set; }
            public Timer Timer { get; set; }
        }

        private CloudGameAPIAndroid()
        {
        }

        private delegate void GLRenderThreadDelegate(int roomIndex);

        private GLRenderThreadDelegate _glRenderThreadDelegate;

        [MonoPInvokeCallback(typeof(GLRenderThreadDelegate))]
        private static void StaticGLRenderThreadCallback(int roomIndex)
        {
            Instance.GLRenderThreadCallback((SeatIndex)roomIndex);
        }

        public void SetMultiplayerListener(IMultiplayerListener listener)
        {
            _multiplayerListener = listener;
        }

        public async Task<ICloudGameAPI.Response> Init()
        {
            var tcs = new TaskCompletionSource<ICloudGameAPI.Response>();
            DebugLog($"[{TAG}] Init called");
            _glRenderThreadDelegate = StaticGLRenderThreadCallback;
            CloudGameSDK.InitEnv(DateTime.Now.Ticks.ToString(), success =>
            {
                DebugLog(
                    $"[{TAG}] InitEnv callback, success: {success}, isRunningCloud: {CloudGameSDK.IsRunningCloud()}");
                SetSceneListener();
                var resp = new ICloudGameAPI.Response(ICloudGameAPI.ErrorCode.Error, "error");
                if (CloudGameSDK.IsRunningCloud())
                {
                    CloudGameSDK.InitCloudScene("1", isSuccess =>
                    {
                        DebugLog($"[{TAG}] InitCloudScene callback, success: {success}");
                        if (success)
                        {
                            resp.code = ICloudGameAPI.ErrorCode.Success;
                            resp.message = "ok";
                            var launchParams = Instance.TryParseLaunchParams();
                            SdkEnv.Merge(EnvParser.ParseLaunchParams(launchParams));
                        }

                        tcs.SetResult(resp);
                    });
                }
                else
                {
                    DebugLog($"[{TAG}] InitLocalScene callback, success: {success}");
                    resp.code = ICloudGameAPI.ErrorCode.Success;
                    resp.message = "ok";
                    tcs.SetResult(resp);
                }
            });
            var resp = await tcs.Task;
            DebugLog($"[{TAG}] InitEnv end, message: {resp.message}");
            return resp;
        }

        public Task<ICloudGameAPI.Response> InitMultiplayer()
        {
            var ok = _multiplayerScene?.InitRendering(1080, 1920) ?? false;
            var result = new ICloudGameAPI.Response(
                ok ? ICloudGameAPI.ErrorCode.Success : ICloudGameAPI.ErrorCode.Error,
                ok ? "ok" : "error");
            return Task.FromResult(result);
        }

        /// 是否可用api: QueryRoomInfo
        public bool CanUse_QueryRoomInfo() => true;

        /// 查询房间用户信息，手动调用
        /// <para>最新版本sdk：OnPlayerJoin 时 只信任 `int roomIndex` 和 `param.RTCUserId`，总是去做 QueryRoomInfo</para>
        public void QueryRoomInfo(int roomIndex)
        {
            _multiplayerScene?.QueryRoomInfo(roomIndex);
        }

        public void SetLogFunction(Action<string> sdkLog, Action<string> sdkLogError)
        {
        }

        public void SendPodQuit()
        {
            CGLogger.Log("[SdkAPIAndroid] SendPodQuit");
            _multiplayerScene?.SendPodQuit();
        }


        public async Task<ICloudGameAPI.Response> SendOpenServiceCustomMessage(SeatIndex roomIndex, string msg)
        {
            var messageId = _multiplayerScene?.SendCustomMessage(roomIndex.ToInt(), msg);
            if (string.IsNullOrEmpty(messageId))
            {
                DebugLog($"[{TAG}] SendOpenServiceCustomMessage error: message id is null", true);
                return new ICloudGameAPI.Response(ICloudGameAPI.ErrorCode.Error, "no message id");
            }

            DebugLog($"[{TAG}] SendOpenServiceCustomMessage, roomIndex: {roomIndex}, message: {msg}, messageId: {messageId}");
            var tcs = new TaskCompletionSource<ICloudGameAPI.Response>();
            var record = new SendMessageRecord();

            Timer timer = new Timer(SendMessageTimeoutInMillis)
            {
                AutoReset = false
            };
            timer.Elapsed += (sender, e) =>
            {
                if (tcs.Task.IsCompleted)
                {
                    return;
                }

                DebugLog($"{TAG} SendCustomMessage task timeout, message id: {messageId}, roomIndex: {roomIndex}");
                lock (_sendMsgLock)
                {
                    _sendMsgTasks.Remove(messageId);
                }

                tcs.SetResult(new ICloudGameAPI.Response(ICloudGameAPI.ErrorCode.Err_Frontier_Init_Timeout,
                    "send timeout"));
            };
            record.CompletionSource = tcs;
            record.RoomIndex = roomIndex;
            record.Timer = timer;
            timer.Start();
            lock (_sendMsgLock)
            {
                _sendMsgTasks[messageId] = record;
            }

            var resp = await tcs.Task;
            DebugLog($"[{TAG}] SendOpenServiceCustomMessage result, code: {resp.code}, message: {resp.message}");
            return resp;
        }

        public ICloudGameAPI.ErrorCode SendVideoFrame(SeatIndex roomIndex, long textureId)
        {
            // DebugLog($"[{TAG}] SendVideoFrame roomIndex: {roomIndex}, textureId: {textureId}");
            _renderingTextureIDs[roomIndex] = textureId;
            GL.IssuePluginEvent(
                Marshal.GetFunctionPointerForDelegate(_glRenderThreadDelegate),
                roomIndex.ToInt());
            return ICloudGameAPI.ErrorCode.Success;
        }

        public void SetAudioEnabled(SeatIndex roomIndex, bool enabled)
        {
            _multiplayerScene?.SetAudioEnabled(roomIndex.ToInt(), enabled);
        }

        public void InitMatchAPI(IMatchAPIListener listener)
        {
            _matchAPIListener = listener;
        }

        public Task<ApiMatchStreamResponse> SendMatchBegin(ApiMatchParams matchParam)
        {
            throw new NotImplementedException();
        }

        public Task<ApiMatchStreamResponse[]> SendMatchEnd()
        {
            throw new NotImplementedException();
        }

        public Task<ApiMatchStreamResponse> SendMatchEnd(int roomIndex)
        {
            throw new NotImplementedException();
        }

        public Task<ICloudGameAPI.Response> SendPodCustomMessage(string token, ApiPodMessageData msgData)
        {
            throw new NotImplementedException();
        }

        private void SetSceneListener()
        {
            _multiplayerScene = CloudGameSDK.GetMultiplayerScene();
            _multiplayerScene.SetSceneListener(OnRenderReady, OnSendCustomMessageResult,
                OnReceiveCustomMessage, OnGameStart, OnQueryRoomInfoResult, OnPlayerJoin,
                OnPlayerExit, OnPlayerOperate, OnPlayerInput);
        }

        private void GLRenderThreadCallback(SeatIndex roomIndex)
        {
            _stopwatch.Restart();


            if (_renderingTextureIDs.TryGetValue(roomIndex, out long textureId))
            {
                // DebugLog($"GLRenderThreadCallback - UpdateTexture - roomIndex: {roomIndex}， textureId： {textureId}");
                _multiplayerScene.UpdateTexture(roomIndex.ToInt(), (int)textureId);
            }
            else
            {
                DebugLog($"[{TAG}] GLRenderThreadCallback - can not get textureId for roomIndex: {roomIndex}", true);
            }

            _stopwatch.Stop();

            if(_stopwatch.ElapsedMilliseconds > 50)
            {
                DebugLog($"[{TAG}] GLRenderThreadCallback - TimeTooLong: {roomIndex} {_stopwatch.ElapsedMilliseconds}", true);
            }
        }

        private CloudGameAndroidLaunchParams TryParseLaunchParams()
        {
            try
            {
                var extraInfo = CloudGameSDK.GetExtraInfo();
                if (string.IsNullOrEmpty(extraInfo))
                {
                    return null;
                }

                if (!string.IsNullOrEmpty(extraInfo))
                {
                    DebugLog($"[{TAG}] CloudGameSDK.GetExtraInfo - extraInfo: " + extraInfo);
                }
                return JsonConvert.DeserializeObject<CloudGameAndroidLaunchParams>(extraInfo);
            }
            catch (Exception exception)
            {
                DebugLog($"[{TAG}] TryParseLaunchParams error: {exception}", true);
            }

            return null;
        }

        private void DebugLog(string log, bool isError = false)
        {
            if (isError)
            {
                CGLogger.LogError(log);
            }
            else
            {
                CGLogger.Log(log);
            }
        }

        #region MultiplayerSceneCallbacks

        private void OnRenderReady()
        {
            DebugLog($"[{TAG}] OnRenderReady");
        }

        private void OnSendCustomMessageResult(bool result, string messageId)
        {
            if (string.IsNullOrEmpty(messageId))
            {
                DebugLog($"{TAG} OnSendCustomMessageResult - result: {result}, message id is empty", true);
                return;
            }

            SendMessageRecord record = null;
            lock (_sendMsgLock)
            {
                if (!_sendMsgTasks.TryGetValue(messageId, out record))
                {
                    DebugLog(
                        $"{TAG} OnSendCustomMessageResult - send message task not found for message_id: {messageId}",
                        true);
                    return;
                }

                _sendMsgTasks.Remove(messageId);

                if (record == null)
                {
                    DebugLog($"{TAG} OnSendCustomMessageResult - empty task record, message id: {messageId}", true);
                    return;
                }
            }
            record.Timer.Stop();

            var resp = new ICloudGameAPI.Response(
                result ? ICloudGameAPI.ErrorCode.Success : ICloudGameAPI.ErrorCode.Error, result ? "success" : "error");
            record.CompletionSource.SetResult(resp);
            if (result)
            {
                DebugLog(
                    $"[{TAG}] SendCustomMessage succeed, messageId: {messageId}, roomIndex: {record.RoomIndex}");
            }
            else
            {
                DebugLog($"{TAG} SendCustomMessage error, message id: {messageId}, roomIndex: {record.RoomIndex}", true);
            }
        }

        private void OnReceiveCustomMessage(int roomIndex, string messageId, string message)
        {
            DebugLog(
                $"[{TAG}] OnReceiveCustomMessage - roomIndex: {roomIndex}, messageId: {messageId}, message: {message}");
            _multiplayerListener?.OnCustomMessage(roomIndex, message);
        }

        private void OnGameStart(List<string> roomIdList)
        {
            DebugLog(
                $"[{TAG}] OnGameStart - roomIdList: {roomIdList.Count}, _multiplayerListener: {_multiplayerListener}");
            _multiplayerListener?.OnGameStart(roomIdList.ToArray());
        }

        public void OnQueryRoomInfoResult(int roomIndex, bool success, PlayerJoinExtra extra)
        {
            if (!_rtcUserIDs.TryGetValue(roomIndex, out string rtcUserId))
            {
                DebugLog(
                    $"[{TAG}] OnQueryRoomInfoResult - unable to get rtcUserId for roomIndex: {roomIndex}", true);
            }

            var initParam = extra?.init_params ?? "";
            var openId = extra?.open_id ?? "";
            var linkRoomID = extra?.link_room_id ?? "";
            if (success)
            {
                DebugLog(
                    $"[{TAG}] OnQueryRoomInfoResult - success: {true}, roomIndex: {roomIndex}, rtcUserId: {rtcUserId}");
                _multiplayerListener?.OnQueryRoomInfo(roomIndex,
                    new JoinRoomParam(initParam, rtcUserId, linkRoomID, ICloudGameAPI.ErrorCode.Success, ""));
            }
            else
            {
                DebugLog(
                    $"[{TAG}] OnQueryRoomInfoResult error, roomIndex: {roomIndex}, init_params: {initParam}, open_id: {openId}, link_room_id: {linkRoomID}");
                _multiplayerListener?.OnQueryRoomInfo(roomIndex,
                    new JoinRoomParam(initParam, rtcUserId,
                        linkRoomID, ICloudGameAPI.ErrorCode.Error,
                        "QueryRoomInfo error"));
            }
        }

        private void OnPlayerJoin(int roomIndex, String rtcUserId, bool result)
        {
            DebugLog($"[{TAG}] OnPlayerJoin - roomIndex: {roomIndex}, rtcUserId: {rtcUserId}, result: {result}");
            if (result)
            {
                _rtcUserIDs[roomIndex] = rtcUserId;
                _multiplayerListener?.OnPlayerJoin(roomIndex,
                    new JoinRoomParam("", rtcUserId, "", ICloudGameAPI.ErrorCode.Success, "success"));
            }
            else
            {
                _multiplayerListener?.OnPlayerJoin(roomIndex,
                    new JoinRoomParam("", rtcUserId, "", ICloudGameAPI.ErrorCode.Error, "error"));
            }
        }

        private void OnPlayerExit(int roomIndex, string rtcUserId, bool result)
        {
            DebugLog($"[{TAG}] OnPlayerExit - roomIndex: {roomIndex}, rtcUserId: {rtcUserId}, result: {result}");
            _multiplayerListener?.OnPlayerExit(roomIndex, new ExitRoomParam(ExitRoomReason.Exit, rtcUserId));
            _rtcUserIDs.Remove(roomIndex);
            _retryJoinCount.Remove(roomIndex);
        }

        private void OnPlayerOperate(int roomIndex, string input)
        {
            if (CloudGameSdk.IsVerboseLogForInput)
                DebugLog($"[{TAG}] OnPlayerOperate - roomIndex: {roomIndex}, input: {input}, multiTouchEnabled: {Input.multiTouchEnabled}");
            _multiplayerListener?.OnPlayerOperate(roomIndex, input);
        }

        private void OnPlayerInput(CloudGameAndroid.InputEventResponse res)
        {
            // DebugLog($"[{TAG}] OnPlayerInput - roomIndex: {res.roomIndex}, input: {res.input}");
            _multiplayerListener?.OnPlayerInput(new InputEventResponse(res.roomIndex, res.input));
        }

        #endregion
    }
}