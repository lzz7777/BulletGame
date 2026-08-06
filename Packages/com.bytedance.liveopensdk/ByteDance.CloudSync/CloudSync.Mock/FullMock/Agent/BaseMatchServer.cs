using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ByteDance.CloudSync.Mock.Agent
{
    internal class MatchClientInfo
    {
        public IRtcClientService Client;
        public IRtcRoomService Room;
        public string PlayInfo;
        public string ExtraInfo;
        public string PoolName;
        public string MatchTag;

        public virtual string Uid => Client.RtcUserId;

        /// <summary>
        /// 请求匹配时的 index，将先请求匹配的端作为主端
        /// </summary>
        public int ReqIndex;

        public MockMatchUser ToMatchUser(SeatIndex index)
        {
            var info = JsonUtility.FromJson<AnchorPlayerInfo>(PlayInfo);
            return new()
            {
                roomIndex = index,
                openId = info.openId,
                nickname = info.nickName,
                liveRoomId = info.liveRoomId,
                avatarUrl = info.avatarUrl,
                rtcUserId = Client.RtcUserId,
                extraInfo = ExtraInfo
            };
        }
    }

    internal class MatchPoolConfig
    {
        public string PoolNameRegex;
        public int PlayersCount;
        public int TeamsCount;

        public MatchPoolConfig(string poolNameRegex, int playersCount, int teamsCount)
        {
            PoolNameRegex = poolNameRegex;
            PlayersCount = playersCount;
            TeamsCount = teamsCount;
        }
    }

    /// <summary>
    /// Mock 匹配服
    /// </summary>
    internal abstract class BaseMatchServer<TClientInfo> where TClientInfo : MatchClientInfo
    {
        protected int _reqIdCounter;

        protected readonly Dictionary<string, TClientInfo> _matchClientInfos = new();

        private readonly MatchPoolConfig _defaultPoolConfig = new(".*", 2, 2);

        private readonly List<MatchPoolConfig> _poolConfigs = new()
        {
            new MatchPoolConfig(@".*1v1.*", 2, 2),
            new MatchPoolConfig(@".*2v2.*", 4, 2),
            new MatchPoolConfig(@".*x3.*", 3, 3),
            new MatchPoolConfig(@".*x4.*", 4, 4),
        };

        protected MatchPoolConfig GetPoolConfig(MatchReq req) => GetPoolConfig(req.poolName);

        protected MatchPoolConfig GetPoolConfig(string poolName)
        {
            foreach (var config in _poolConfigs)
            {
                if (Regex.IsMatch(poolName, config.PoolNameRegex))
                    return config;
            }

            return _defaultPoolConfig;
        }

        protected static Func<KeyValuePair<string, TClientInfo>, bool> IsMatch(MatchReq req)
        {
            return s => IsMatch(s, req);
        }

        protected static bool IsMatch(KeyValuePair<string, TClientInfo> s, MatchReq req)
        {
            return s.Value.PoolName == req.poolName && s.Value.MatchTag == req.matchTag;
        }

        // Mock 匹配2-4人
        protected void CheckMatch(MatchReq req)
        {
            var poolName = req.poolName;
            var matchTag = req.matchTag;
            var config = GetPoolConfig(poolName);
            var targetCount = config.PlayersCount;
            var teamsCount = config.TeamsCount;
            var matched = _matchClientInfos.Where(IsMatch(req)).ToList();
            var currentCount = _matchClientInfos.Count;
            Debug.Log($"current players: {currentCount}, matched: {matched.Count} / {targetCount}, poolName: {poolName}, matchTag: {matchTag}");
            if (currentCount < targetCount)
                return;
            if (matched.Count < targetCount)
            {
                // 在匹配中的 currentCount 已满足人数，但符合匹配的 matched.Count 却仍不足，打印详情方便调试
                Debug.LogWarning($"Mock匹配服：匹配的玩家不足！ matching players {matched.Count} / {targetCount} for poolName: {poolName}, matchTag: {matchTag}");
                Debug.LogWarning($"Mock匹配服：请求匹配的玩家为：uid: {req.Uid} poolName: {req.poolName} matchTag: {req.matchTag}");
                var mismatched = _matchClientInfos.Where(s => !IsMatch(s, req)).ToList();
                var listMsg = string.Join(",\n", mismatched.Select(s => $"uid: {s.Value.Uid} poolName: {s.Value.PoolName} matchTag: {s.Value.MatchTag}"));
                Debug.LogWarning($"Mock匹配服：不匹配的玩家为：{listMsg}");
                return;
            }

            Debug.Log($"Mock匹配服：匹配成功 Match success, match players: {targetCount}, teams: {teamsCount}, poolName: {poolName}, matchTag: {matchTag}");

            var selected = matched.Select(s => s.Value).ToList();
            selected.Sort((a, b) => a.ReqIndex - b.ReqIndex);
            selected = selected.Take(targetCount).ToList();
            foreach (var info in selected)
            {
                _matchClientInfos.Remove(info.Uid);
            }

            var matchId = $"MockMatch-{System.Guid.NewGuid()}";
            var matchResp = new MatchResp
            {
                matchReq = req,
                success = true,
                code = MatchResultCode.Success,
                message = "match success.",
                teams = new List<MockMatchResultTeam>(),
                allusers = new List<MockMatchUser>(),
                matchId = matchId,
            };

            SetTeamsUsers(matchResp, selected, req);

            OnCheckMatchSuccess(matchResp, selected, req);
        }

        protected abstract void OnCheckMatchSuccess(MatchResp matchResp, List<TClientInfo> selected, MatchReq req);

        protected void SetTeamsUsers(MatchResp matchResp, List<TClientInfo> clients, MatchReq req)
        {
            var config = GetPoolConfig(req.poolName);
            var playersCount = config.PlayersCount;
            var teamsCount = config.TeamsCount;
            var index = 0;
            var allUsers = matchResp.allusers;
            for (var i = 0; i < playersCount; i++)
            {
                var user = clients[index].ToMatchUser((SeatIndex)index);
                allUsers.Add(user);
                index++;
            }

            index = 0;
            var countInTeam = playersCount / teamsCount;
            if (countInTeam < 1)
                countInTeam = 1;
            for (var iTeam = 0; iTeam < teamsCount; iTeam++)
            {
                var usersInTeam = new List<MockMatchUser>();
                for (var j = 0; (j < countInTeam || iTeam == teamsCount - 1) && index < playersCount; j++)
                {
                    usersInTeam.Add(allUsers[index]);
                    index++;
                }

                var team = new MockMatchResultTeam
                {
                    users = usersInTeam
                };
                Debug.Log($" - team[{iTeam}] {team.users.Count} users");
                matchResp.teams.Add(team);
            }
        }
    }
}