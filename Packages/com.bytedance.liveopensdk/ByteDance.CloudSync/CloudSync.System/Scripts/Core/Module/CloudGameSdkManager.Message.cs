// Copyright@www.bytedance.com
// Author: zhouxu.ken
// Date: 2024/06/11
// Description: 处理与消息相关的

using System;
using System.Threading.Tasks;

namespace ByteDance.CloudSync
{
    internal partial class CloudGameSdkManager
    {
        private readonly MessageQueue _messageQueue = new();
        private int _checkMessageQueueCount;

        struct SendMessageParam
        {
            public SeatIndex RoomIndex;
            public string MsgName;
            public string Msg;
            public bool ShowMsgLog;
        }

        private void EnqueueMessage(int roomIndex, CloudGameMessageBase msg)
        {
            MessageHandler?.Write(msg);
            if (MessageHandler != null || _checkMessageQueueCount >= 3)
                return;
            _checkMessageQueueCount++;
            CGLogger.LogError($"{LogTag}_messageWriter is null! room: {roomIndex}");
        }

        private async Task<bool> _SendMessage(SeatIndex roomIndex, string msgName, string msg, bool log = true)
        {
            if (!_initResult.State.IsSuccessOrAlready())
            {
                CGLogger.LogError(LogTag + $"msg room: {roomIndex}, \"{msgName}\", init state error! code: {_initResult.Code}, errorMsg: {_initResult.Error}");
            }

            // ReSharper disable once UseObjectOrCollectionInitializer
            var call = new CloudMessageCall<SendMessageParam>(msgName, this._SendMessageImpl);
            call.Conf_RetryCountLimit = _messageQueue.MessageRetryCount;
            // TODO:此处先不重试, 等重试可以将结果返回给ts后再重试
            call.RetryEnabled = false;
            CGLogger.Log($"CloudGameSdkMessage SendMessage room: {roomIndex}, \"{msgName}\", Msg = {msg}, ShowMsgLog = {log}");
            await call.CallAsync(new SendMessageParam
            {
                RoomIndex = roomIndex,
                MsgName = msgName,
                Msg = msg,
                ShowMsgLog = log,
            }, _messageQueue);
            CGLogger.Log($"CloudGameSdkMessage SendMessage room: {roomIndex}, \"{msgName}\", call.CallAsync Finished");
            var resp = call.CallResponse;
            return resp.Success;
        }

        // todo: [端上交互] 长连消息可靠性：
        /*
            长连消息可靠性 04.25 方案：
            1. 发送消息返回状态值（新字段）保证感知是否错误、是否需要重试：
            2. 重发：使用seqId。  重发时的seq不变，新消息的递增seq+1。
                1. 接收成功时，更新本地 received seq
                2. 接收到seq跳变时的策略：信任收到的最新seq消息.
                3. code返回失败需要重发
            3. 网络问题、连续失败：客户端先预埋好逻辑、事件
            4. 断联与恢复 -- 暂时不做
         */
        private async Task<MessageCallResponse> _SendMessageImpl(SendMessageParam param)
        {
            var roomIndex = param.RoomIndex;
            var msgName = param.MsgName ??= "";
            var msg = param.Msg ??= "";
            if (param.ShowMsgLog)
                CGLogger.Log($"{LogTag}SendMessage room: {roomIndex}, \"{msgName}\", msg: {msg}");

            try
            {
                // 云游戏长连链路：发送 -> 端上
                var resp = await CloudGameSdk.API.SendOpenServiceCustomMessage(roomIndex, msg);
                // var resp = new ICloudGameAPI.Response(ICloudGameAPI.ErrorCode.Err_Frontier_Send_Failed, "本地测试用");
                var code = resp.code;
                var respMessage = resp.message;
                bool success;
                switch (code)
                {
                    case ICloudGameAPI.ErrorCode.Success:
                    case ICloudGameAPI.ErrorCode.Success_AlreadyInited:
                        success = true;
                        CGLogger.Log($"{LogTag}SendMessage room: {roomIndex}, \"{msgName}\" result code {code}, {respMessage}");
                        break;
                    default:
                        // error
                        success = false;
                        respMessage ??= code.ToString();
                        CGLogger.LogError($"{LogTag}SendMessage room: {roomIndex}, \"{msgName}\" result error: {code} = {(int)code}, {respMessage}");
                        break;
                }

                // todo: [端上交互] 长连消息可靠性：返回状态值（新字段）保证感知是否错误、是否需要重试
                return new MessageCallResponse(success, (int)code, respMessage);
            }
            catch (Exception e)
            {
                CGLogger.LogError(LogTag + $"SendMessage room: {roomIndex}, \"{msgName}\", exception: " + e);
                return new MessageCallResponse(e);
            }
        }

        /// <summary>
        /// 云游戏长连链路：发送 --&gt; 端上
        /// </summary>
        /// <param name="roomIndex"></param>
        /// <param name="msgName"></param>
        /// <param name="msg"></param>
        public Task<bool> SendMessage(SeatIndex roomIndex, string msgName, string msg) => _SendMessage(roomIndex, msgName, msg);

        public int MessageRetryCount
        {
            get => _messageQueue.MessageRetryCount;
            set => _messageQueue.MessageRetryCount = value;
        }
    }
}