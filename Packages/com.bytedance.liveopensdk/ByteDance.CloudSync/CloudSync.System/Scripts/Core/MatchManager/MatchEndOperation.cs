using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ByteCloudGameSdk;

namespace ByteDance.CloudSync.MatchManager
{
    /// <summary>
    /// 回流：结束匹配同玩
    /// </summary>
    internal class MatchEndOperation : BaseMatchOperation
    {
        public SeatIndex EndRoomIndex { get; set; }

        public async Task<IEndResult> Run()
        {
            Debug.Log("MatchEndOperation Run");

            // 1. 发起
            var tcs = new TaskCompletionSource<MatchEndResult>();
            SendMatchEnd(tcs, EndRoomIndex);

            // 2. 等待
            var task = tcs.Task;
            while (!task.IsCompleted)
            {
                await Task.Yield();
            }

            // 3. 结果
            if (task.IsFaulted)
                return ErrorResult(ExceptionToMessage(task.Exception));

            return task.Result;
        }

        internal static MatchEndResult ErrorResult(string errMsg, EndResultCode code = EndResultCode.Error)
        {
            return new MatchEndResult
            {
                Code = code,
                Message = errMsg
            };
        }

        private async void SendMatchEnd(TaskCompletionSource<MatchEndResult> taskOp, SeatIndex endRoomIndex)
        {
            var callIdName = MakeCallId("SendMatchEnd");
            Debug.Log(callIdName);
            try
            {
                var valid = ValidateArgs(callIdName, EndRoomIndex);
                if (!valid)
                {
                    Debug.LogError(valid.Message);
                    taskOp.SetResult(ErrorResult(valid.Message));
                    return;
                }

                // 调用API
                if (endRoomIndex == SeatIndex.Invalid)
                {
                    Debug.LogDebug("MatchAPI.SendMatchEnd");
                    var resps = await MatchAPI.SendMatchEnd();
                    HandleResponses(taskOp, callIdName, resps);
                }
                else
                {
                    Debug.LogDebug($"MatchAPI.SendMatchEnd endRoomIndex: {endRoomIndex}");
                    var resp = await MatchAPI.SendMatchEnd((int)endRoomIndex);
                    HandleResponses(taskOp, callIdName, new[] { resp });
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                taskOp.SetException(e);
            }
        }

        private ValidationResult ValidateArgs(string callIdName, SeatIndex endRoomIndex)
        {
            Debug.LogDebug($"{callIdName} args: endRoomIndex: {endRoomIndex}");

            if (endRoomIndex != SeatIndex.Invalid && !endRoomIndex.IsValid())
                return ValidationResult.Fail($"{callIdName} arg error! endRoomIndex is invalid! endRoomIndex: {endRoomIndex}");

            return ValidationResult.Success();
        }

        private void HandleResponses(TaskCompletionSource<MatchEndResult> taskOp, string callIdName, ApiMatchStreamResponse[] resps)
        {
            Debug.LogDebug($"HandleResponses {resps.Length} resps");
            var seatResponses = new List<IEndSeatResponse>();
            foreach (var resp in resps)
            {
                var seatResponse = HandleSeatResponse(callIdName, resp);
                seatResponses.Add(seatResponse);
            }

            var result = new MatchEndResult().Accept(seatResponses.ToArray());
            taskOp.SetResult(result);
        }

        private MatchEndSeatResponse HandleSeatResponse(string callIdName, ApiMatchStreamResponse resp)
        {
            var code = resp.code;
            var index = (SeatIndex)resp.roomIndex;
            var seatResponse = new MatchEndSeatResponse().Accept(resp);

            switch (code)
            {
                case MatchErrorCode.Success:
                    Debug.Log($"{callIdName} stream response success index: {index}");
                    Debug.LogDebug($"{callIdName} stream response success, {resp.ToStr()}");
                    break;
                default:
                    Debug.LogError($"{callIdName} stream response error! index: {index}" +
                                   $", response: {resp.ToStr()}");
                    break;
            }

            return seatResponse;
        }
    }
}