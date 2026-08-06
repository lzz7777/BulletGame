using System.Collections.Generic;
using UnityEngine;

namespace ByteDance.CloudSync.Mock.Agent
{
    /// <summary>
    /// Mock 的匹配服
    /// </summary>
    internal class MatchServer : BaseMatchServer<MatchClientInfo>
    {
        private readonly AgentServer _agentServer;

        protected static readonly IMockLogger Debug = IMockLogger.GetLogger(nameof(MatchServer));

        public MatchServer(AgentServer agentServer)
        {
            _agentServer = agentServer;
            _SelfTest();
        }

        private void _SelfTest()
        {
            var config = GetPoolConfig("1v1_no_rule");
            Debug.Assert(config is { PlayersCount: 2, TeamsCount: 2 }, "Assert test 1v1_no_rule failed");
            config = GetPoolConfig("2v2_no_rule");
            Debug.Assert(config is { PlayersCount: 4, TeamsCount: 2 }, "Assert test 2v2_no_rule failed");
            config = GetPoolConfig("x3_no_rule");
            Debug.Assert(config is { PlayersCount: 3, TeamsCount: 3 }, "Assert test x3_no_rule failed");
            config = GetPoolConfig("x4_no_rule");
            Debug.Assert(config is { PlayersCount: 4, TeamsCount: 4 }, "Assert test x4_no_rule failed");
        }

        public void OnClientExit(IRtcClientService client, IRtcRoomService room)
        {
            _matchClientInfos.Remove(client.RtcUserId);
        }

        public void MatchReq(IRtcClientService client, IRtcRoomService room, MatchReq req)
        {
            RequestMatch(client, room, req);
        }

        private void RequestMatch(IRtcClientService client, IRtcRoomService room, MatchReq req)
        {
            var uid = req.Uid;
            if (_matchClientInfos.ContainsKey(uid))
                return;

            Debug.Log($"RequestMatch, uid: {uid}, roomId: {room.RtcRoomId}, poolName: {req.poolName}, matchTag: {req.matchTag}");

            _matchClientInfos.Add(uid, new MatchClientInfo
            {
                Client = client,
                Room = room,
                PlayInfo = req.playerInfoJson,
                ExtraInfo = req.extraInfo,
                PoolName = req.poolName,
                MatchTag = req.matchTag,
                ReqIndex = _reqIdCounter++
            });

            CheckMatch(req);
        }

        protected override void OnCheckMatchSuccess(MatchResp matchResp, List<MatchClientInfo> selected, MatchReq req)
        {
            var data = JsonUtility.ToJson(matchResp);
            foreach (var client in selected)
            {
                client.Room.Send(MessageWrapper.CreateRequest(MessageId.MatchResp, data));
            }
        }

        private void ResponseCancel(IRtcRoomService room, CancelMatchReq req, int code, string message = "")
        {
            var resp = new CancelMatchResp
            {
                success = code == 0,
                code = code,
                message = message
            };
            Debug.Log($"CancelMatch, code: {resp.code} uid: {req.Uid}");
            var data = JsonUtility.ToJson(resp);
            room.Send(MessageWrapper.CreateRequest(MessageId.CancelMatchResp, data));
        }

        public void CancelMatch(IRtcRoomService room, CancelMatchReq req)
        {
            var uid = req.Uid;
            Debug.Log($"CancelMatch, uid: {uid}, roomId: {room.RtcRoomId}");
            var found = _matchClientInfos.Remove(uid);
            if (found)
            {
                ResponseCancel(room, req, 0);
            }
            else
            {
                Debug.LogError($"CancelMatch, remove failed: client not in match, uid: {uid}");
                ResponseCancel(room, req, 1, "client not in match");
            }
        }

        public void EndGameReq(IRtcClientService client, IRtcRoomService room)
        {
            var notify = new EndGameNotify();
            var data = JsonUtility.ToJson(notify);
            client.Send(MessageWrapper.CreateRequest(MessageId.EndGameNotify, data));
            room.ExitRoom(client);
        }
    }
}