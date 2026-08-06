using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ByteDance.CloudSync.Match;
using ByteDance.CloudSync.TeaSDK;
using MatchPb;
using Newtonsoft.Json;

namespace ByteDance.CloudSync.MatchManager
{
    /// <summary>
    /// 匹配用户：调用 MatchService 匹配用户
    /// </summary>
    internal class MatchUserOperation : BaseMatchOperation
    {
        public MatchUserOperation(InternalMatchConfig config, string matchParamJson, AnchorPlayerInfo myPlayerInfo, MatchCloudGameInfo myCloudGameInfo,
            SwitchTokenGetterDelegate switchTokenGetter)
        {
            MatchConfig = config;
            MatchParamJson = matchParamJson;
            MyPlayerInfo = myPlayerInfo;
            MyCloudGameInfo = myCloudGameInfo;
            SwitchTokenGetter = switchTokenGetter;
        }

        /// 开发者传入的匹配配置
        public InternalMatchConfig MatchConfig { get; set; }
        public string MatchParamJson { get; set; }

        public AnchorPlayerInfo MyPlayerInfo { get; set; }
        public MatchCloudGameInfo MyCloudGameInfo { get; set; }

        internal delegate string SwitchTokenGetterDelegate(MatchResultUser user);
        private SwitchTokenGetterDelegate SwitchTokenGetter { get; set; }

        // ReSharper disable once InconsistentNaming
        private const int OLYMPUS_APP_ID = 480295;

        public async Task<CloudMatchUsersResult> Run(CancellationToken cancelToken)
        {
            Debug.Log("MatchUserOperation Run");

            // 1. 发起
            var tcs = new TaskCompletionSource<CloudMatchUsersResult>();
            StartMatchUser(tcs, cancelToken);

            // 2. 等待
            var taskOp = tcs;
            var task = taskOp.Task;
            while (!task.IsCompleted)
            {
                await Task.Yield();
            }

            // 3. 结果
            if (task.IsCanceled)
                return CanceledResult("Cancelled");
            if (task.IsFaulted)
                return ErrorResult(ExceptionToMessage(task.Exception));

            var opResult = task.Result;
            return opResult;
        }

        internal static CloudMatchUsersResult CanceledResult(string errMsg = "Cancelled")
        {
            return new CloudMatchUsersResult
            {
                Code = MatchResultCode.Cancelled,
                Message = errMsg
            };
        }

        internal static CloudMatchUsersResult ErrorResult(string errMsg, MatchResultCode code = MatchResultCode.Error)
        {
            return new CloudMatchUsersResult
            {
                Code = code,
                Message = errMsg
            };
        }

        private async void StartMatchUser(TaskCompletionSource<CloudMatchUsersResult> taskOp, CancellationToken cancelToken)
        {
            TeaReport.Report_matchservice_start_match_start();
            var callIdName = MakeCallId("StartMatchUser");
            Debug.Log(callIdName);
            try
            {
                var valid = ValidateArgs(callIdName, MatchConfig, MyPlayerInfo, MyCloudGameInfo);
                if (!valid)
                {
                    Debug.LogError(valid.Message);
                    taskOp.SetResult(ErrorResult(valid.Message));
                    return;
                }

                // appid校验：
                // 1. 先本地校验匹配的appid
                // 2. 并且在真实环境 MatchService 会通过服务器鉴权校验appid。如果appid错误，能校验到、且会联网建联失败，且发起匹配也会失败
                var appId = MatchConfig.AppId;
                if (string.IsNullOrEmpty(appId))
                {
                    var errMsg = $"{callIdName} error! AppId is empty! 匹配错误，AppId为空！";
                    Debug.LogError(errMsg);
                    taskOp.SetResult(ErrorResult(errMsg));
                    return;
                }

                // MatchConfig 匹配服务对外，开发者可配置
                var matchInfo = MatchConfig.ToMatchInfo(OLYMPUS_APP_ID);
                var matchParamsJson = MatchParamJson;
                var extraInfo = JsonConvert.SerializeObject(MyCloudGameInfo);
                var resp = await MatchService.StartMatch(matchInfo, matchParamsJson, extraInfo, cancelToken);
                HandleResponse(taskOp, callIdName, resp);
            }
            catch (QuittingException e)
            {
                Debug.LogWarning(e.ToString());
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                taskOp.SetException(e);
            }
        }

        private ValidationResult ValidateArgs(string callIdName, InternalMatchConfig matchConfig, AnchorPlayerInfo myPlayerInfo, MatchCloudGameInfo myCloudGameInfo)
        {
            if (matchConfig == null)
                return ValidationResult.Fail("匹配失败：匹配参数错误！ arg error: matchConfig is null!");
            if (myPlayerInfo == null)
                return ValidationResult.Fail("匹配失败：获取主播用户信息失败！ myPlayerInfo is null!");

            if (myCloudGameInfo == null)
                return ValidationResult.Fail("匹配失败： 云游戏匹配信息为空！ cloudGameInfo is null!");
            if (!myCloudGameInfo.IsValid)
                return ValidationResult.Fail("匹配失败： 云游戏匹配信息无效！ cloudGameToken is NOT valid!");
            if (!string.IsNullOrEmpty(matchConfig.MatchTag))
            {
                var tag = matchConfig.MatchTag;
                var isErrorChar = tag.Any(s => s > 127);
                if (isErrorChar)
                {
                    for (var i = 0; i < tag.Length; i++)
                    {
                        if (tag[i] > 127)
                            return ValidationResult.Fail($"匹配失败： MatchTag含有非法字符！必须为普通AscII字符。 Illegal char pos: {i}, MatchTag: {tag}");
                    }
                }
            }

            Debug.LogDebug($"{callIdName} matchConfig: {matchConfig.ToStr()}");
            Debug.LogDebug($"{callIdName} myPlayerInfo: {myPlayerInfo.ToStr()}");
            Debug.LogDebug($"{callIdName} myCloudGameInfo: {myCloudGameInfo.ToStr()}");
            return ValidationResult.Success();
        }

        private void HandleResponse(TaskCompletionSource<CloudMatchUsersResult> taskOp, string callIdName, MatchResult resp)
        {
            var myPlayerInfo = MyPlayerInfo;
            var respCode = resp.Code;
            var isSuccess = false;
            switch (respCode)
            {
                case ResultCode.RequestDone:
                {
                    // MatchService: 请求完成，但具体是否成功要看 .Result .Error
                    MatchResultNty resultNotify = resp.Result;
                    MatchErrorNty errorNotify = resp.Error;
                    if (resultNotify != null)
                    {
                        Debug.Log($"{callIdName} response success, result: true");
                        isSuccess = true;
                        var matchResult = ParseMatchServiceResult(resultNotify, myPlayerInfo, SwitchTokenGetter);
                        Debug.LogDebug($"{callIdName} matchResult: {matchResult.ToStr()}");
                        taskOp.SetResult(matchResult);
                    }
                    else
                    {
                        var statusCodeObj = errorNotify?.StatusCode;
                        //错误码 对应 StatusCode->code
                        var statusCode = (MatchErrCode)(statusCodeObj?.Code ?? -1);
                        var resultCode = (statusCode == MatchErrCode.OverTime) ? MatchResultCode.Timeout : MatchResultCode.Error;
                        var errMsg = statusCodeObj?.Message ?? "resp.Result is null";
                        if (resultCode == MatchResultCode.Timeout)
                            Debug.LogWarning($"{callIdName} response statusCode: {statusCode} ({(int)statusCode}), errMsg: {errMsg}");
                        else
                            Debug.LogError($"{callIdName} response error! statusCode: {statusCode} ({(int)statusCode}), errMsg: {errMsg}");
                        taskOp.SetResult(ErrorResult(errMsg, resultCode));
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
                    var msg = $"{callIdName} Error! Code: {respCode}, ErrorMsg: {resp.ErrorMsg}";
                    Debug.LogError(msg);
                    taskOp.SetResult(ErrorResult(msg));
                }
                    break;
            }
            TeaReport.Report_matchservice_start_match_end((int)resp.Code, OLYMPUS_APP_ID.ToString(), isSuccess);
        }

        internal static CloudMatchUsersResult ParseMatchServiceResult(MatchResultNty resultNotify, AnchorPlayerInfo myPlayerInfo,
            SwitchTokenGetterDelegate switchTokenGetter)
        {
            try
            {
                var matchId = resultNotify.MatchResultId;
                var inTeams = resultNotify.MatchTeams;
                var teamsCount = inTeams.Count;
                Debug.LogDebug($"parse MatchResult: teams: {teamsCount}, matchId: {matchId}, my OpenId: {myPlayerInfo.OpenId}");
                MatchResultUser hostUser = null;
                var teams = new List<MatchResultTeam>();
                var allUsers = new List<MatchResultUser>();

                var index = SeatIndex.Index0;
                var myRoomIndex = SeatIndex.Index0;
                var switchHostToken = string.Empty;
                var hostCloudGameToken = string.Empty;
                foreach (var iterTeam in inTeams)
                {
                    var usersInTeam = new List<MatchResultUser>();
                    foreach (var iterUser in iterTeam.MatchUsers)
                    {
                        var user = new MatchResultUser().Accept(iterUser, index);
                        var cloudGameInfo = user.CloudStreamInfo;
                        usersInTeam.Add(user);
                        allUsers.Add(user);
                        Debug.LogDebug($"parse MatchResult: user {index}: {user.ToStr()}");
                        Debug.LogDebug($"parse MatchResult: cloudGameInfo {index}: {cloudGameInfo?.ToStr()}");
                        if (cloudGameInfo == null)
                            Debug.LogError($"parse MatchResult: cloudGameInfo error!  user.ExtraInfo: {user.ExtraInfo}");

                        if (iterUser.OpenId == myPlayerInfo.OpenId)
                            myRoomIndex = index;

                        if (index == SeatIndex.Index0)
                        {
                            hostUser = user;
                            switchHostToken = switchTokenGetter(hostUser);
                            hostCloudGameToken = cloudGameInfo?.cloudGameToken;
                            Debug.LogDebug($"parse MatchResult: HostUser: index: {index}, openId: {user.OpenId}, {cloudGameInfo?.ToStr()}");
                        }

                        index++;
                    }

                    var outTeam = new MatchResultTeam
                    {
                        Users = usersInTeam
                    };
                    teams.Add(outTeam);
                }

                if (teams.Count <= 0)
                    throw new Exception("teams.Count <= 0!");
                if (hostUser == null)
                    throw new Exception("hostUser not found!");

                var isHost = myPlayerInfo.OpenId == hostUser.OpenId;
                var result = new CloudMatchUsersResult
                {
                    Code = MatchResultCode.Success,
                    Message = string.Empty,
                    MatchId = matchId,
                    IsHost = isHost,
                    HostUser = hostUser,
                    MyIndex = myRoomIndex,
                    Teams = teams,
                    AllUsers = allUsers,
                    SwitchHostToken = switchHostToken,
                    HostCloudGameToken = hostCloudGameToken,
                };
                return result;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return ErrorResult(ExceptionToMessage(e));
            }
        }
    }
}