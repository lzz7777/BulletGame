using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ByteDance.CloudSync.Match;
using MatchPb;
using UnityEngine;

namespace ByteDance.CloudSync.Mock
{
    internal class RtcMockMatchService : IMatchService
    {
        private IMessageChannel _agentChannel;
        private ICloudClientManager ClientManager => CloudSyncSdk.InternalCurrent.ClientManager;
        private readonly IMockLogger _logger = IMockLogger.GetLogger(nameof(RtcMockMatchService));
        private readonly Dictionary<string, string> _switchInfos = new();
        private TaskCompletionSource<MatchResult> _matchResultTask;
        private TaskCompletionSource<bool> _matchCancelTask;

        public void OnPodInstanceReady(IMessageChannel agentChannel)
        {
            _agentChannel = agentChannel;
            _agentChannel.OnMessageReceive += HandleAgentMessage;
        }

        private void HandleAgentMessage(MessageWrapper message)
        {
            if (message.id == MessageId.MatchResp)
            {
                var resp = message.To<MatchResp>();
                _logger.Log($"MatchResp: {resp.success}");

                resp.MyClient = ClientManager.GetClient(0);
                if (resp.success && resp.IsHost)
                {
                    foreach (var user in resp.allusers)
                    {
                        _switchInfos[user.rtcUserId] = user.extraInfo;
                    }
                }

                var result = resp.ToPbMatchResult();
                _matchResultTask?.SetResult(result);
                _matchResultTask = null;
            }
            else if (message.id == MessageId.CancelMatchResp)
            {
                var task = _matchCancelTask;
                _matchCancelTask = null;
                task?.SetResult(true);
            }
        }

        /// 获取直播用户信息。 会由 <see cref="RtcMock.MatchManager"/> 获取主播用户信息时，执行 <see cref="IAnchorPlayerInfoProvider.FetchPlayerInfo"/> 时调用到
        public async Task<GetWebCastInfoResult> GetWebCastInfo(CancellationToken token)
        {
            _logger.Log($"GetWebCastInfo 获取Mock直播用户信息 (frame: {Time.frameCount}f)");
            await Task.Delay(1, token);
            var playerInfo = RtcMock.MockSettings.PlayerInfo;
            var result = new GetWebCastInfoResult
            {
                Code = ResultCode.RequestDone,
                ErrorMsg = null,
                Result = new WebCastInfo
                {
                    OpenID = playerInfo.OpenId,
                    AvatarURL = playerInfo.AvatarUrl,
                    NickName = playerInfo.NickName,
                    LiveRoomID = ToPbLongId(playerInfo.LiveRoomId),
                }
            };
            return result;
        }

        internal static long ToPbLongId(string strId)
        {
            return long.Parse(strId);
        }

        public async Task<MatchResult> StartMatch(MatchInfo matchInfo, string matchParamJson, string extraInfo, CancellationToken cancelToken)
        {
            MatchReq req = null;
            try
            {
                var clientManager = ClientManager;
                var client = clientManager.GetHostClient();
                // todo: fix, client may be null
                req = new MatchReq
                {
                    poolName = matchInfo.ApiName,
                    matchTag = matchInfo.MatchTag,
                    rtcUserId = client.RtcUserId,
                    playerInfoJson = client.PlayerInfo.ToJson(),
                    extraInfo = extraInfo
                };
                _agentChannel.Send(MessageId.MatchReq, JsonUtility.ToJson(req));

                // wait:
                _matchResultTask = new();
                var task = _matchResultTask.Task;
                while (!task.IsCompleted)
                {
                    await Task.Yield();
                    if (cancelToken.IsCancellationRequested)
                    {
                        var cancelMatchReq = new CancelMatchReq
                        {
                            rtcUserId = client.RtcUserId
                        };
                        Debug.Assert(_matchCancelTask == null, "Assert _matchCancelTask == null");
                        _matchCancelTask = new TaskCompletionSource<bool>();
                        _agentChannel.Send(MessageId.CancelMatchReq, JsonUtility.ToJson(cancelMatchReq));
                        // 发出 CancelMatchReq ，等待 Resp
                        await _matchCancelTask.Task;
                    }

                    cancelToken.ThrowIfCancellationRequested();
                }

                var result = await task;
                return result;
            }
            catch (OperationCanceledException) // including TaskCanceledException
            {
                _matchResultTask = null;
                var resp = new MatchResp
                {
                    matchReq = req,
                    success = false,
                    code = MatchResultCode.Cancelled,
                    message = "Cancelled",
                };
                return resp.ToPbMatchResult();
            }
        }

        public void Dispose()
        {
        }
    }

    [Serializable]
    internal class MatchReq
    {
        public virtual string Uid => rtcUserId;
        public string rtcUserId;
        public string playerInfoJson;
        public string extraInfo;
        public string poolName;
        public string matchTag;

        public MatchInfo ToPbMatchInfo()
        {
            return new MatchInfo
            {
                OlympusAppId = 0,
                StarkAppId = 0,
                ApiName = poolName,
                MatchTag = matchTag
            };
        }
    }

    [Serializable]
    internal class MockMatchUser
    {
        /// 座位号 0,1,2,3 ： 0为主Host，1,2,3为加入的同玩玩家
        public SeatIndex roomIndex;

        public string rtcUserId;

        /// 开放平台open_id
        public string openId;

        /// 头像
        public string avatarUrl;

        /// 昵称
        public string nickname;

        /// 直播间roomId
        public string liveRoomId;

        /// 客户端透传信息(StartMatchReq带的用户透传字段，客户端自己解析)
        public string extraInfo;

        public MatchResultUser ToMatchResultUser()
        {
            return new MatchResultUser
            {
                RoomIndex = roomIndex,
                OpenId = openId,
                AvatarUrl = avatarUrl,
                Nickname = nickname,
                LiveRoomId = liveRoomId,
                ExtraInfo = extraInfo
            };
        }

        public MatchPb.MatchResultUser ToPbMatchUser()
        {
            return new MatchPb.MatchResultUser
            {
                OpenId = openId,
                AvatarUrl = avatarUrl,
                NickName = nickname,
                ExtraInfo = extraInfo,
                LiveRoomId = RtcMockMatchService.ToPbLongId(liveRoomId)
            };
        }
    }

    [Serializable]
    internal class MockMatchResultTeam
    {
        public List<MockMatchUser> users;

        public MatchPb.MatchTeam ToPbMatchTeam()
        {
            var team = new MatchTeam();
            team.MatchUsers.AddRange(users.Select(u => u.ToPbMatchUser()));
            return team;
        }
    }

    /// <summary>
    /// <see cref="ByteDance.CloudSync.Match.MatchResult"/>
    /// </summary>
    [Serializable]
    internal class MatchResp
    {
        public MatchReq matchReq;
        public bool success;
        public MatchResultCode code;
        public string message;
        public List<MockMatchResultTeam> teams;
        public List<MockMatchUser> allusers;

        public string matchId;
        public ICloudClient MyClient { get; set; }

        public SeatIndex MyIndex => allusers.FirstOrDefault(u => u.rtcUserId == MyClient.RtcUserId)?.roomIndex ?? SeatIndex.Index0;
        public MatchResultUser HostUser => teams[0].users[0].ToMatchResultUser();

        public List<MatchResultTeam> Teams => teams.Select(team => new MatchResultTeam
        {
            Users = team.users.Select(user => user.ToMatchResultUser()).ToList()
        }).ToList();

        public List<MatchResultUser> AllUsers => allusers.Select(user => user.ToMatchResultUser()).ToList();

        public bool IsHost => teams[0].users[0].rtcUserId == MyClient.RtcUserId;
        public object SwitchHostToken => teams[0].users[0].extraInfo;

        public MatchResult ToPbMatchResult()
        {
            switch (code)
            {
                case MatchResultCode.Success:
                    return new MatchResult
                    {
                        Code = ResultCode.RequestDone,
                        ErrorMsg = message,
                        Result = ToPbMatchResultNty()
                    };
                case MatchResultCode.Cancelled:
                    return new MatchResult
                    {
                        Code = ResultCode.UserCanceled,
                    };
                case MatchResultCode.Timeout:
                case MatchResultCode.Error:
                    var result = new MatchResult
                    {
                        Code = ResultCode.RequestDone,
                        ErrorMsg = message,
                        Error = ToPbMatchErrorNty(),
                    };
                    return result;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private MatchResultNty ToPbMatchResultNty()
        {
            var resultNty = new MatchResultNty
            {
                MatchResultId = matchId,
                MatchInfo = matchReq.ToPbMatchInfo(),
                MatchTeams = { }
            };

            foreach (var team in teams)
            {
                resultNty.MatchTeams.Add(team.ToPbMatchTeam());
            }

            return resultNty;
        }

        private MatchErrorNty ToPbMatchErrorNty()
        {
            var errorNty = new MatchErrorNty
            {
                MatchInfo = matchReq.ToPbMatchInfo(),
                StatusCode = new StatusCode
                {
                    Code = -1,
                    Message = message,
                }
            };
            if (code == MatchResultCode.Timeout)
                errorNty.StatusCode.Code = (int)MatchPb.MatchErrCode.OverTime;
            return errorNty;
        }
    }

    [Serializable]
    internal class CancelMatchReq
    {
        public virtual string Uid => rtcUserId;
        public string rtcUserId;
    }

    [Serializable]
    internal class CancelMatchResp
    {
        public bool success;
        public int code;
        public string message;
    }

    [Serializable]
    internal class EndGameReq
    {
        public SeatIndex index;
    }

    [Serializable]
    internal class EndGameNotify
    {
    }
}