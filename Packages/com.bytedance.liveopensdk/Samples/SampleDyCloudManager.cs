// Copyright (c) Bytedance. All rights reserved.
// Description:

#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ByteDance.Live.Foundation.Logging;
using ByteDance.LiveOpenSdk.DyCloud;
using ByteDance.LiveOpenSdk.Report;
using ByteDance.LiveOpenSdk.Runtime.Utilities;
using ByteDance.LiveOpenSdk.Utilities;

namespace Douyin.LiveOpenSDK.Samples
{
    /// <summary>
    /// 直播开放 SDK 集成的抖音云相关功能的接入示例代码。
    /// 演示了基于抖音云的短连接、长连接、互动消息消费与履约。
    /// 配置项：<see cref="EnvId"/>,<see cref="ServiceId"/>,<see cref="IsDebug"/>,<see cref="DebugIpAddress"/>
    /// </summary>
    public static class SampleDyCloudManager
    {
        /// <summary>
        /// 抖音云环境的 env_id 参数。
        /// </summary>
        public static string EnvId { get; set; } = "";

        /// <summary>
        /// 抖音云环境的 service_id 参数。
        /// </summary>
        public static string ServiceId { get; set; } = "";

        /// 是否为抖音云调试模式。调试模式下允许使用空的 token，并且支持流量转发。
        public static bool IsDebug { get; set; } = true;

        /// 调试模式下使用的本地流量转发 IP 地址。
        public static string DebugIpAddress { get; set; } = "";

        private static IDyCloudApi DyCloudApi => SampleLiveOpenSdkManager.Sdk.GetDyCloudApi();
        private static IMessageAckService MessageAckService => SampleLiveOpenSdkManager.Sdk.GetMessageAckService();

        private static readonly LogWriter Log = new LogWriter(SdkUnityLogger.LogSink, "SampleDyCloudManager");

        private static bool _isDyCloudInitialized;

        private static IDyCloudWebSocket? _webSocket;

        /// <summary>
        /// 初始化抖音云。
        /// </summary>
        public static async Task Init()
        {
            if (_isDyCloudInitialized) return;
            var initParams = new DyCloudInitParams
            {
                EnvId = EnvId,
                DefaultServiceId = ServiceId,
                DebugIpAddress = DebugIpAddress,
                IsDebug = IsDebug
            };
            try
            {
                await DyCloudApi.InitializeAsync(initParams);
                Log.Info("抖音云初始化：成功");
                _isDyCloudInitialized = true;
            }
            catch (Exception)
            {
                Log.Error("抖音云初始化：失败");
            }
        }

        /// <summary>
        /// 抖音云短连接能力演示：开启推送任务。
        /// </summary>
        /// <remarks>
        /// 开发者需要在抖音云的服务上调用 OpenAPI 实现开启推送任务。
        /// 这里的请求仅是通知开发者服务端对局开始，你需要替换为自己实现的等价操作。
        /// </remarks>
        public static async Task StartTasks()
        {
            // 注意：这里的 path 仅供演示使用，请替换为自己的实际地址。
            const string startTasksPath = "/live_data/task/start";
            try
            {
                var response = await DyCloudApi.CallContainerAsync(
                    startTasksPath,
                    "",
                    "POST",
                    "",
                    new Dictionary<string, string>());

                if (response.StatusCode != 200)
                {
                    Log.Error($"抖音云开始推送任务：失败 HTTP {response.StatusCode} {response.Body}");
                    return;
                }

                var respData = SampleUtils.FromJsonString<StartTasksResponse>(response.Body);
                foreach (var entry in respData.Result)
                {
                    var resultStr = entry.Value;
                    var result = SampleUtils.FromJsonString<StartTaskResponse>(resultStr);
                    Log.Info($"抖音云开始推送任务：任务名称：{entry.Key} 结果：{result.ErrNo == 0} 详情：{result.ErrMsg}");
                }
            }
            catch (Exception e)
            {
                Log.Error($"抖音云开始推送任务：失败 {e.GetType().FullName}: {e.Message}");
            }
        }

        /// <summary>
        /// 抖音云长连接能力演示：建立推送通道。
        /// </summary>
        /// <remarks>
        /// 开发者需要在抖音云的服务上推送消息。
        /// 这里的请求仅是连接上抖音云的推送通道。
        /// </remarks>
        public static async Task ConnectWebSocket()
        {
            // 注意：这里的 path 仅供演示使用，请替换为自己的实际地址。
            const string webSocketPath = "/web_socket/on_connect/v2";
            if (_webSocket == null)
            {
                _webSocket = DyCloudApi.WebSocket;
                _webSocket.OnOpen += OnOpen;
                _webSocket.OnMessage += OnMessage;
                _webSocket.OnError += OnError;
                _webSocket.OnClose += OnClose;
            }

            try
            {
                await _webSocket.ConnectContainerAsync(webSocketPath);
                Log.Info($"抖音云连接 WebSocket：成功");
            }
            catch (Exception)
            {
                Log.Error($"抖音云连接 WebSocket：失败");
            }
        }

        private static void OnOpen()
        {
            Log.Info("抖音云 WebSocket 回调：OnOpen");
        }

        private static void OnClose()
        {
            Log.Info("抖音云 WebSocket 回调：OnClose");
        }

        private static void OnError(IDyCloudWebSocketError error)
        {
            Log.Error($"抖音云 WebSocket 回调：OnError {error}");
            if (error.WillReconnect == true)
            {
                // 本次错误系统会自动重连，开发者上层不需要重新发起连接
            }
            else
            {
                // 连接失败，开发者上层需要自己选择处理，可以稍后重试建连、或先弹框提示再点击后重试建连。
            }
        }

        private static void OnMessage(string data)
        {
            if (string.IsNullOrEmpty(data)) return;
            var message = SampleUtils.FromJsonString<DyCloudSocketMessage>(data);
            var msgId = message.MsgId;
            var msgType = message.MsgType;

            Log.Info($"抖音云 WebSocket 回调：OnMessage, msg_type: {msgType}, msg_id: {msgId}, data: {data}");

            if (!string.IsNullOrEmpty(msgId) && !string.IsNullOrEmpty(msgType))
            {
                // 完成指令渲染后发送履约
                MessageAckService.ReportAck(msgId, msgType);
            }
        }

        /// <summary>
        /// 主动关闭 WebSocket 连接。
        /// </summary>
        public static void CloseWebSocket()
        {
            if (_webSocket == null)
                return;
            Log.Info("主动关闭抖音云 WebSocket");
            _webSocket?.Close();
        }


        /// <summary>
        /// 这是短连接演示时用到的数据结构，这不是任何公开 API 的一部分，切勿照搬。
        /// </summary>
        [DataContract]
        [Serializable]
        private class StartTasksResponse
        {
            [DataMember(Name = "result")] public Dictionary<string, string> Result;
        }

        /// <summary>
        /// 这是短连接演示时用到的数据结构，这不是任何公开 API 的一部分，切勿照搬。
        /// </summary>
        [DataContract]
        [Serializable]
        private class StartTaskResponse
        {
            [DataMember(Name = "err_no")] public int ErrNo;
            [DataMember(Name = "err_msg")] public string ErrMsg;
            [DataMember(Name = "data")] public Payload Data;

            [DataContract]
            [Serializable]
            public class Payload
            {
                [DataMember(Name = "task_id")] public string TaskId;
            }
        }
    }
}