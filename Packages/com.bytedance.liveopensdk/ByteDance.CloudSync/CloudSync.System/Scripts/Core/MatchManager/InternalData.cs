using System;
using System.Collections.Generic;
using System.Linq;
using ByteCloudGameSdk;
using ByteDance.CloudSync.Match;

namespace ByteDance.CloudSync.MatchManager
{
    internal static class SimpleMatchConfigExtension
    {
        // StarkAppId 5040 对外默认通用 （抖音开放平台-通用匹配）
        internal static readonly int PublicMatchAppId = 5040;

        internal static string GetSimpleMatchPool(SimpleMatchPoolType poolType)
        {
            return poolType switch
            {
                SimpleMatchPoolType.P1v1 => "1v1_no_rule",
                SimpleMatchPoolType.P2v2 => "2v2_no_rule",
                SimpleMatchPoolType.P1x4 => "x4_no_rule",
                SimpleMatchPoolType.P1x3 => "x3_no_rule",
                _ => string.Empty
            };
        }

        internal static MatchConfig ToMatchConfig(this SimpleMatchConfig config)
        {
            var pool = GetSimpleMatchPool(config.PoolType);
            return new MatchConfig
            {
                MatchAppId = PublicMatchAppId,
                PoolName = pool,
                MatchTag = config.MatchTag
            };
        }

        internal static string ToStr(this SimpleMatchConfig self)
        {
            return $"{self.PoolType}, {self.MatchTag}";
        }
    }

    internal class InternalMatchConfig : MatchConfig
    {
        public string AppId { get; private set; }

        internal static InternalMatchConfig CreateFrom(MatchConfig from, string appId)
        {
            var config = new InternalMatchConfig
            {
                MatchAppId = from.MatchAppId,
                PoolName = from.PoolName ?? string.Empty,
                MatchTag = from.MatchTag ?? string.Empty,
            };
            config.SetTagAppId(appId);
            return config;
        }

        private void SetTagAppId(string appId)
        {
            AppId = appId;
            if (string.IsNullOrEmpty(appId))
                return;
            var append = $"-{appId}";
            if (MatchTag.EndsWith(append, StringComparison.Ordinal))
                return;
            MatchTag = $"{MatchTag}{append}";
        }
    }

    internal interface IMatchResultEx : IMatchResult
    {
        /// 房主切流Token（默认无需使用，仅手动切流用）
        object SwitchHostToken { get; }
    }

    /// <summary>
    /// 匹配结果（仅内部）
    /// </summary>
    internal class CloudMatchUsersResult : IMatchResultEx
    {
        public bool IsSuccess => Code == MatchResultCode.Success;
        public MatchResultCode Code { get; internal set; }
        public string Message { get; internal set; }
        public string MatchId { get; internal set; }
        public bool IsHost { get; internal set; }
        public SeatIndex MyIndex { get; internal set; }

        public MatchResultUser HostUser { get; internal set; }
        public List<MatchResultTeam> Teams { get; internal set; }
        public List<MatchResultUser> AllUsers { get; internal set; }
        public object SwitchHostToken { get; internal set; }

        internal string HostCloudGameToken { get; set; }

        public string ToStr() => $"{{ IsHost: {IsHost}, MyIndex: {MyIndex}, Teams: {Teams?.Count ?? 0}, AllUsers: {AllUsers?.Count ?? 0}" +
                                 $", MatchId: {MatchId}, HostCloudGameToken: {HostCloudGameToken} }}";

        public ApiMatchParams ToMatchStreamParams()
        {
            var param = new ApiMatchParams
            {
                hostToken = HostCloudGameToken,
                matchKey = MatchId,
                roomIndex = (int)MyIndex
            };
            return param;
        }
    }



    /// <summary>
    /// 同玩结束回包（仅内部）
    /// </summary>
    internal class MatchEndResult : IEndResult
    {
        public bool IsSuccess => Code == EndResultCode.Success;

        public EndResultCode Code { get; internal set; }

        public string Message { get; internal set; }

        public IEndSeatResponse[] SeatResponses { get; internal set; } = { };

        public MatchEndResult Accept(IEndSeatResponse[] responses)
        {
            var length = responses.Length;
            SeatResponses = responses.ToArray();

            var isSuccess = length == 0 || responses.All(s => s.Code == EndResultCode.Success);
            var isTimeout = !isSuccess && length > 0 && responses.All(s => s.Code == EndResultCode.Timeout);
            if (isSuccess)
                Code = EndResultCode.Success;
            else if (isTimeout)
                Code = EndResultCode.Timeout;
            else
                Code = EndResultCode.Error;
            Message = string.Join(", ", responses.Where(s => !string.IsNullOrEmpty(s.Message)).Select(s => s.Message).ToArray());
            return this;
        }

        public MatchEndResult AcceptMsgResponses(MatchEndPodMsgResponse[] responses)
        {
            return Accept(responses.Cast<IEndSeatResponse>().ToArray());
        }

        // 用新的response列表，覆盖已存在的 roomIndex 的元素
        public IEndResult Merge(IEndSeatResponse[] newList)
        {
            var mergeList = SeatResponses.ToList();
            var newsIndexes = newList.Select(s => s.RoomIndex);
            mergeList.RemoveAll(s => newsIndexes.Contains(s.RoomIndex));
            mergeList.AddRange(newList);
            mergeList.Sort((a, b) => a.RoomIndex - b.RoomIndex);
            Code = EndResultCode.Error;
            Message = null;
            Accept(mergeList.ToArray());
            return this;
        }
    }

    /// <summary>
    /// 同玩结束回包（仅内部）
    /// </summary>
    internal class MatchEndSeatResponse : IEndSeatResponse
    {
        public SeatIndex RoomIndex { get; internal set; }
        public bool IsSuccess => Code == EndResultCode.Success;
        public EndResultCode Code { get; internal set; }
        public string Message { get; internal set; }
        public string LogId { get; internal set; }

        public MatchEndSeatResponse Accept(ApiMatchStreamResponse response)
        {
            RoomIndex = (SeatIndex)response.roomIndex;
            Code = response.code.ToEndGameCode();
            Message = $"{response.code} ({(int)response.code}) {response.message}";
            LogId = response.logId;
            return this;
        }
    }

    /// <summary>
    /// 同玩结束透传消息回包（仅内部）
    /// </summary>
    internal class MatchEndPodMsgResponse : IEndSeatResponse
    {
        public SeatIndex RoomIndex { get; internal set; }
        public bool IsSuccess => Code == EndResultCode.Success;
        public EndResultCode Code { get; internal set; }
        public string Message { get; internal set; }
        public string LogId { get; } = null;

        public MatchEndPodMsgResponse Accept(SeatIndex roomIndex, ICloudGameAPI.Response response)
        {
            RoomIndex = roomIndex;
            Code = ToEndSeatCode(response.code);
            Message = $"{response.code} ({(int)response.code}) {response.message}";
            return this;
        }

        internal static EndResultCode ToEndSeatCode(ICloudGameAPI.ErrorCode code)
        {
            // ReSharper disable once ConvertSwitchStatementToSwitchExpression
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (code)
            {
                case ICloudGameAPI.ErrorCode.Success:
                    return EndResultCode.Success;
                // 异常：如果 gamesdk SendPodCustomMessage 返回 MatchErrorCode 为 Err_MC_Send_Timeout, Err_Frontier_Send_Timeout，返回超时 Code: Timeout
                case ICloudGameAPI.ErrorCode.Err_MC_Send_Timeout:
                case ICloudGameAPI.ErrorCode.Err_Frontier_Send_Timeout:
                    return EndResultCode.Timeout;
                default:
                    return EndResultCode.Error;
            }
        }
    }

    internal static class MatchDataExtensions
    {
        public static EndResultCode ToEndGameCode(this MatchErrorCode sdkMatchErrorCode)
        {
            // ReSharper disable once ConvertSwitchStatementToSwitchExpression
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (sdkMatchErrorCode)
            {
                case MatchErrorCode.Success:
                    return EndResultCode.Success;
                // 异常：如果 sendMatchEnd 返回 MatchErrorCode 为 CustomMessageTimeout, SwitchTimeout，返回超时 Code: Timeout
                case MatchErrorCode.CustomMessageTimeout:
                case MatchErrorCode.SwitchTimeout:
                    return EndResultCode.Timeout;
                default:
                    return EndResultCode.Error;
            }
        }
    }

    public enum PlayerInfoResultCode
    {
        /// 成功
        Success = 0,

        /// 错误
        Error,
    }

    /// <summary>
    /// 用户信息请求结果（仅内部）
    /// </summary>
    internal class PlayerInfoTaskResult
    {
        public PlayerInfoResultCode Code;
        public string Message;
        public GetWebCastInfoResult WebCastInfoResponse;
        public AnchorPlayerInfo PlayerInfo;
    }

    /// <summary>
    /// 匹配的云游戏信息（仅内部）
    /// </summary>
    [Serializable]
    internal class MatchCloudGameInfo
    {
        /// 云游戏token，用于调度
        public string cloudGameToken;

        /// <summary>
        /// room token
        /// </summary>
        public string roomToken;

        /// 云游戏RTC的唯一用户id，用于框架内部的用户标识区分、校验
        public string rtcUserId;

        public bool IsValid => !string.IsNullOrEmpty(cloudGameToken) &&
                               !string.IsNullOrEmpty(rtcUserId) &&
                               !string.IsNullOrEmpty(roomToken);

        public string ToStr() => $"{{ rtcUserId: {rtcUserId}, cloudGameToken: {cloudGameToken}, roomToken: {roomToken} }}";
    }

    internal enum MatchPodMessageType
    {
        DefaultMsg,
        EndEvent
    }

    /// <summary>
    /// 自定义封装一层消息协议格式，用于同玩的Pod间透传消息（仅内部）。
    /// </summary>
    /// <remarks>
    /// 会转为 json 后，赋值给 <see cref="ApiPodMessageData"/> 的`message`字段，
    /// 在 <see cref="HostRoom.SendPodMessage"/> 中发送，
    /// 然后调用 CloudGameAPI 的 <see cref="CloudGameAPIWindows.SendPodCustomMessage"/> 发送透传消息。
    /// 接收者（玩家B）在 <see cref="IMatchAPIListener.OnPodCustomMessage"/> 中接收。
    /// </remarks>
    [Serializable]
    internal class MatchPodMessage
    {
        /// 自定义封装的消息类型
        public MatchPodMessageType type;

        /// 结束同玩事件类型
        public EndEventType endType;

        /// 透传消息内容
        public string info;

        /// 发送者Id，通常是openId
        public string fromId;

        /// 唯一标识玩家参与一次同玩的会话id
        public string playerSessionId;

        public string ToStr() => $"{{ type: {type}, endType: {endType}, info: {info}, fromId: {fromId} }}";
    }

    /// <summary>
    /// 结束同玩事件（仅内部）
    /// </summary>
    internal class CloudMatchEndEvent : IEndEvent
    {
        public EndEventType EndType { get; set; }
        public string EndInfo { get; set; }
        public bool HasEndInfo() => !string.IsNullOrEmpty(EndInfo);
        public string FromId { get; set; }

        /// 唯一标识玩家参与一次同玩的会话id（可校验判断是否已收到过该场次的结束信息，若重复可忽略）
        public string PlayerSessionId { get; set; }

        public static CloudMatchEndEvent CreateFrom(MatchPodMessage podMessage)
        {
            return new CloudMatchEndEvent
            {
                EndType = podMessage.endType,
                EndInfo = podMessage.info,
                FromId = podMessage.fromId,
                PlayerSessionId = podMessage.playerSessionId,
            };
        }
    }
}