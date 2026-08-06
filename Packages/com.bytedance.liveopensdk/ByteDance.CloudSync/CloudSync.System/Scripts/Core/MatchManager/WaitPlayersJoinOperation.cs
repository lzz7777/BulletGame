using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ByteDance.CloudSync.MatchManager
{
    /// <summary>
    /// 等待玩家：
    /// 作为房主A，等待其他玩家BCD加入Join
    /// </summary>
    internal class WaitPlayersJoinOperation : BaseMatchOperation
    {
        public delegate bool CheckPlayersJoin(List<MatchResultUser> users);

        public CloudMatchUsersResult MatchUsersResult { get; set; }
        public CheckPlayersJoin CheckPlayersJoinFunc { get; set; }
        public int MaxTimeoutMs { get; set; } = 30000; // 毫秒

        private string _waitErrorMsg;

        public WaitPlayersJoinOperation(CloudMatchUsersResult matchUsersResult, CheckPlayersJoin checkPlayersJoinFunc)
        {
            MatchUsersResult = matchUsersResult;
            CheckPlayersJoinFunc = checkPlayersJoinFunc;
        }

        public async Task<IMatchResult> Run()
        {
            Debug.Log("WaitPlayersJoinOperation Run");
            var callIdName = MakeCallId("WaitPlayersJoin");

            if (!ValidateArgs(callIdName, out string errorMsg))
                return ErrorResult(errorMsg);

            CancellationTokenSource cancelSource = new();
            using (cancelSource)
            {
                var taskJoin = WaitJoin(cancelSource.Token);
                var taskTimeout = Task.Delay(MaxTimeoutMs, cancelSource.Token);

                var completedTask = await Task.WhenAny(taskJoin, taskTimeout);
                if (completedTask == taskJoin)
                {
                    cancelSource.Cancel();
                    if (taskJoin.IsCanceled)
                        return CanceledResult("Cancelled");
                    if (taskJoin.IsFaulted)
                        return ErrorResult(ExceptionToMessage(taskJoin.Exception));
                    var result = taskJoin.Result;
                    if (!result)
                        return ErrorResult(_waitErrorMsg);
                    Debug.Log("WaitPlayersJoinOperation return");
                    return MatchUsersResult;
                }
                else
                {
                    cancelSource.Cancel();
                    _waitErrorMsg = $"WaitPlayersJoinOperation timeout! (time = {MaxTimeoutMs/1000f:F1}s)";
                    Debug.LogError(_waitErrorMsg);
                    return ErrorResult(_waitErrorMsg);
                }
            }
        }

        private async Task<bool> WaitJoin(CancellationToken cancelToken)
        {
            var users = MatchUsersResult.AllUsers;
            while (!cancelToken.IsCancellationRequested)
            {
                var allJoined = CheckPlayersJoinFunc.Invoke(users);
                if (allJoined)
                    return true;
                await Task.Yield();
            }

            _waitErrorMsg = "WaitPlayersJoin failed!";
            return false;
        }

        private bool ValidateArgs(string callIdName, out string errorMsg)
        {
            errorMsg = null;
            if (MatchUsersResult == null)
            {
                errorMsg = $"{callIdName} arg error! MatchUsersResult == null";
                Debug.LogError(errorMsg);
                return false;
            }

            var user = MatchUsersResult.AllUsers;
            if (user == null || user.Count == 0)
            {
                errorMsg = $"{callIdName} arg error! MatchUsersResult.AllUsers is empty!";
                Debug.LogError(errorMsg);
                return false;
            }

            if (CheckPlayersJoinFunc == null)
            {
                errorMsg = $"{callIdName} arg error! CheckPlayersJoinFunc == null";
                return false;
            }

            return true;
        }

        private static CloudMatchUsersResult CanceledResult(string errMsg) =>
            MatchUserOperation.CanceledResult(errMsg);

        private static CloudMatchUsersResult ErrorResult(string errMsg, MatchResultCode code = MatchResultCode.Error) =>
            MatchUserOperation.ErrorResult(errMsg, code);

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}