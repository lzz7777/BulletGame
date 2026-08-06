using System;
using System.Threading;
using System.Threading.Tasks;
using ByteDance.CloudSync.Match;
using ByteDance.CloudSync.TeaSDK;
using UnityEngine;

namespace ByteDance.CloudSync.MatchManager
{
    /// <summary>
    /// 调用 MatchService 获取自身用户信息
    /// </summary>
    internal class FetchPlayerInfoOperation : BaseMatchOperation
    {
        private TaskCompletionSource<PlayerInfoTaskResult> _playerInfoTask;

        public async Task<PlayerInfoTaskResult> FetchPlayerInfo(CancellationToken cancelToken)
        {
            Debug.Log("FetchPlayerInfoOperation");

            // 1. 发起
            var tcs = _playerInfoTask;
            if (tcs == null)
            {
                tcs = _playerInfoTask = new TaskCompletionSource<PlayerInfoTaskResult>();
                RequestWebCastInfo(tcs);
            }
            else
            {
                Debug.Log("FetchPlayerInfoOperation 等待返回 wait running task ...");
            }

            // 2. 等待
            var task = tcs.Task;
            while (!task.IsCompleted)
            {
                if (cancelToken.IsCancellationRequested)
                    return null;
                await Task.Yield();
            }

            // 3. 结果
            _playerInfoTask = null;
            if (task.IsCanceled)
                return null;
            if (task.IsFaulted)
                return ErrorResult(ExceptionToMessage(task.Exception?.InnerException));

            return task.Result;
        }

        private static PlayerInfoTaskResult ErrorResult(string errMsg, GetWebCastInfoResult webcastInfo, PlayerInfoResultCode code = PlayerInfoResultCode.Error)
        {
            return new PlayerInfoTaskResult
            {
                Code = code,
                Message = errMsg,
                WebCastInfoResponse = webcastInfo,
                PlayerInfo = null
            };
        }

        private static PlayerInfoTaskResult ErrorResult(string errMsg, PlayerInfoResultCode code = PlayerInfoResultCode.Error)
        {
            return new PlayerInfoTaskResult
            {
                Code = code,
                Message = errMsg,
                WebCastInfoResponse = new GetWebCastInfoResult
                {
                    Code = ResultCode.Undefined,
                    ErrorMsg = string.Empty,
                    Result = null
                },
                PlayerInfo = null
            };
        }

        private async void RequestWebCastInfo(TaskCompletionSource<PlayerInfoTaskResult> taskOp)
        {
            TeaReport.Report_cloudmatchmanager_fetch_player_info_start();
            var callIdName = MakeCallId("RequestWebCastInfo");
            Debug.Log($"{callIdName} 获取直播信息 ...");
            Debug.Log($"{callIdName} 当前网络：{Application.internetReachability}");
            try
            {
                // request
                var resp = await MatchService.GetWebCastInfo(CancellationToken.None);
                // handle resp
                var respCode = resp.Code;
                switch (respCode)
                {
                    case ResultCode.RequestDone:
                    {
                        // MatchService: 请求完成，但具体是否成功要看 .Result
                        WebCastInfo webCastInfo = resp.Result;
                        if (webCastInfo != null)
                        {
                            Debug.Log($"{callIdName} 获取直播信息成功。 response success, result: true");
                            var playerInfo = new AnchorPlayerInfo().Accept(webCastInfo);
                            Debug.Log($"{callIdName} playerInfo: {playerInfo.ToStr()}");
                            var opResult = new PlayerInfoTaskResult
                            {
                                Code = PlayerInfoResultCode.Success,
                                WebCastInfoResponse = resp,
                                PlayerInfo = playerInfo
                            };
                            taskOp.SetResult(opResult);
                        }
                        else
                        {
                            var errMsg = "resp.Result is null";
                            var log = $"{callIdName} 获取直播信息失败！ response error! errMsg: {errMsg}";
                            Debug.LogError(log);
                            taskOp.SetResult(ErrorResult(errMsg, resp));
                        }
                    }
                        break;
                    case ResultCode.UserCanceled:
                    {
                        Debug.LogWarning($"{callIdName} UserCanceled");
                        taskOp.SetCanceled();
                    }
                        break;
                    case ResultCode.NetworkError:
                    default:
                    {
                        var errMsg = respCode == ResultCode.NetworkError ? "网络错误！" : "未知错误。";
                        errMsg += $" code: {respCode} ({(int)respCode})";
                        var log = $"{callIdName} 获取直播信息失败！ Error: {errMsg}";
                        Debug.LogError(log);
                        taskOp.SetResult(ErrorResult(errMsg, resp));
                    }
                        break;
                }
                TeaReport.Report_cloudmatchmanager_fetch_player_info_end((int)respCode,ResultCode.RequestDone == respCode);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                taskOp.SetException(e);
            }
        }
    }
}