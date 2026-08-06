using System;
using System.Threading.Tasks;
using ByteDance.CloudSync.TeaSDK;

namespace ByteDance.CloudSync.MatchManager
{
    internal class PodMessageOperation : BaseMatchOperation
    {
        public SeatIndex Index;
        public string Token;
        public ApiPodMessageData MsgData;

        public async Task<ICloudGameAPI.Response> Run()
        {
            Debug.Log("PodMessageOperation Run");
            // todo: FIXME: 埋点，这个`Token`不是发送者`from`，而是目标
            TeaReport.Report_sdk_send_pod_message(Token);
            var valid = ValidateArgs();
            if (!valid)
                return ErrorResult(valid.Message);

            try
            {
                // 1. 发起
                var task = MatchAPI.SendPodCustomMessage(Token, MsgData);

                // 2. 等待
                await task;

                // 3. 结果
                if (task.IsCanceled)
                    return CanceledResult("Cancelled");
                if (task.IsFaulted)
                    return ErrorResult(ExceptionToMessage(task.Exception));

                var result = task.Result;
                HandleResponse("PodMessageOperation", result);
                return result;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return ErrorResult(ExceptionToMessage(e));
            }
        }

        private ValidationResult ValidateArgs()
        {
            Debug.LogDebug($"PodMessageOperation args: Index: {Index}, Token: {Token}, MsgData.from: {MsgData?.from}");

            if (!Index.IsValid())
                return ValidationResult.Fail($"index is invalid! Index: {Index} from: {MsgData?.from}");

            if (string.IsNullOrEmpty(Token))
                return ValidationResult.Fail($"token is empty! Index: {Index} from: {MsgData?.from}");

            if (MsgData == null)
                return ValidationResult.Fail($"msgData is null! Index: {Index}");

            return ValidationResult.Success();
        }

        private void HandleResponse(string callIdName, ICloudGameAPI.Response resp)
        {
            var code = resp.code;
            if (code.IsSuccessOrAlready())
            {
                Debug.LogDebug($"{callIdName} response success, Index: {Index}, response: {resp.ToStr()}");
            }
            else
            {
                Debug.LogError($"{callIdName} response error! Index: {Index}, response: {resp.ToStr()}");
            }
        }

        private ICloudGameAPI.Response CanceledResult(string errMsg)
        {
            Debug.Log($"[{GetType().Name}] CanceledResult");
            return new ICloudGameAPI.Response
            {
                code = ICloudGameAPI.ErrorCode.Error,
                message = errMsg
            };
        }

        private ICloudGameAPI.Response ErrorResult(string errMsg)
        {
            Debug.LogError($"[{GetType().Name}] ErrorResult: {errMsg}");
            return new ICloudGameAPI.Response
            {
                code = ICloudGameAPI.ErrorCode.Error,
                message = errMsg
            };
        }
    }
}