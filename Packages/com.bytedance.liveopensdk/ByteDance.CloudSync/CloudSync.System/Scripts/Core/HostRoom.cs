using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ByteDance.CloudSync.MatchManager;
using UnityEngine;

namespace ByteDance.CloudSync
{
    internal partial class HostRoom : IHostRoomEx
    {
        public interface IHandler
        {
            void OnEndHostRoom(IHostRoomEx room);
        }

        internal IHandler RoomHandler => _handler;

        private readonly IHandler _handler;
        private readonly ICloudSwitchTokenProvider _tokenProvider;
        private readonly Dictionary<SeatIndex, SwitchTokenData> _switchTokens = new();
        private static long _nextIndex;
        private const string Tag = "HostRoom";
        internal static SdkDebugLogger Debug { get; set; } = new("HostRoom");

        public HostRoom(IHandler handler, IAnchorPlayerInfo anchor, ICloudSwitchTokenProvider tokenProvider)
        {
            Debug.Assert(anchor != null, "Assert anchor");
            var roomId = anchor?.LiveRoomId ?? "0000";
            ID = $"HostRoom_{roomId}_{++_nextIndex}";
            _handler = handler;
            _tokenProvider = tokenProvider;
        }

        public async Task<IEndResult> Kick(SeatIndex index, string info)
        {
            Debug.Log($"Kick one 云同步-结束同玩：单个用户 (index = {index}, info = {info})");
            return await _KickOne(index, info);
        }

        public async Task<IEndResult> End(string info)
        {
            Debug.Log($"End all 云同步-结束同玩：所有人 (info = {info})");
            var mapping = new InfoMapping(s => info);
            return await _EndAll(mapping);
        }

        public async Task<IEndResult> End(InfoMapping endInfoMapping)
        {
            Debug.Log($"End all 云同步-结束同玩：所有人 (infoMapping = {endInfoMapping})");
            return await _EndAll(endInfoMapping);
        }

        private async Task<IEndResult> _KickOne(SeatIndex index, string info)
        {
            // 结束单个同玩玩家
            //   - 回流步骤1：发透传消息 await sendPodMessage endInfo
            Debug.LogDebug($"before Kick one ({index.ToInt()}) - PodMessage, NonHost players states: {GetNonHostStatesDebugInfo()}");
            var msgResponse = await SendMatchEndPodMessage(index, info, MatchPodMessageType.EndEvent);
            if (!msgResponse.IsSuccess)
            {
                // 如果 发送 podMessage 失败： —— End接口返回失败 Code: Error
                return new MatchEndResult().Accept(new IEndSeatResponse[] { msgResponse });
            }

            //   - 回流步骤2：发回流指令 sendMatchEnd
            Debug.LogDebug($"before Kick one ({index.ToInt()}) - SendMatchEnd, NonHost players states: {GetNonHostStatesDebugInfo()}");
            var matchEndOp = new MatchEndOperation
            {
                EndRoomIndex = index,
            };
            var result = await matchEndOp.Run();

            //   - 回流步骤3：等rtc退房（gamesdk onPlayerExit）
            if (result != null && result.IsSuccess)
                await _End_WaitPlayerExit(result);

            Debug.LogDebug($"after Kick one ({index.ToInt()}), NonHost players states: {GetNonHostStatesDebugInfo()}");
            Debug.Assert(result != null, "Assert result");
            Debug.Assert(result != null && result.SeatResponses.Length > 0, "Assert responses.Length > 0");
            return result;

            // todo: 埋点
        }

        internal async Task<IEndResult> _EndAll_PodMessage(InfoMapping infoMapping)
        {
            //   - 回流步骤1：发透传消息 await sendPodMessage endInfo
            var clients = ICloudSync.Instance.GetNonHostSeats();
            var msgTasks = clients
                .Select(it => SendMatchEndPodMessage(it.Index, infoMapping.GetInfo(it.Index), MatchPodMessageType.EndEvent))
                .ToList();
            var msgResponses = await Task.WhenAll(msgTasks);

            var msgResults = new MatchEndResult().AcceptMsgResponses(msgResponses);
            if (msgResults.IsSuccess == false)
            {
                // ReSharper disable once InvertIf
                if (false)
                {
                    // 待定：可考虑优化：先给发送消息成功的回流
                    var sentPods = msgResponses.Where(s => s.IsSuccess);
                    var endTasks = sentPods
                        .Select(it => _EndForPod(it.RoomIndex, infoMapping))
                        .ToList();
                    var endResults = await Task.WhenAll(endTasks);
                    // 合并结果
                    return msgResults.Merge(endResults);
                }

                // 如果 发送 podMessage 失败： —— End接口返回失败 Code: Error
                return msgResults;
            }

            return msgResults;
        }

        private async Task<IEndResult> _EndAll(InfoMapping infoMapping)
        {
            // 结束所有同玩玩家
            Debug.LogDebug($"before Host End all - PodMessage, NonHost players states: {GetNonHostStatesDebugInfo()}");
            //   - 回流步骤1：发透传消息 sendPodMessage
            var msgResults = await _EndAll_PodMessage(infoMapping);
            if (msgResults.IsSuccess == false)
                return msgResults;

            //   - 回流步骤2：发回流指令 sendMatchEnd
            Debug.LogDebug($"before Host End all - SendMatchEnd, NonHost players states: {GetNonHostStatesDebugInfo()}");
            var matchEndOp = new MatchEndOperation
            {
                EndRoomIndex = SeatIndex.Invalid,
            };
            var result = await matchEndOp.Run();

            //   - 回流步骤3：等rtc退房（gamesdk onPlayerExit）
            if (result != null && result.IsSuccess)
                await _End_WaitPlayerExit(result);

            Debug.LogDebug($"after Host End all, NonHost players states: {GetNonHostStatesDebugInfo()}");
            Debug.Assert(result != null, "Assert result");
            if (result != null && result.IsSuccess)
            {
                _handler.OnEndHostRoom(this);
            }

            return result;

            // todo: 埋点
        }

        internal async Task _End_WaitPlayerExit(IEndResult result)
        {
            if (result != null && result.IsSuccess)
            {
                // 如果End成功，稍作等待，玩家退房
                // note: 但不能一直真的等待他退房，因为要避免特殊异常case：发送End但用户RTC不退房。
                await Task.Delay(500);
            }
        }

        private Task<IEndSeatResponse> _EndForPod(SeatIndex sentIndex, InfoMapping infoMapping)
        {
            throw new NotImplementedException();
        }

        private async Task<SwitchTokenData> GetSwitchTokenData(ICloudSeat seat, CancellationToken token)
        {
            if (_switchTokens.TryGetValue(seat.Index, out var tokenData))
                return tokenData;
            float startTime = Time.realtimeSinceStartup;
            var callName = $"回调 ICloudSwitchTokenProvider.GetToken: {seat.Index}";
            try
            {
                CGLogger.Log($"回调 {callName}");
                var t = await _tokenProvider.GetToken(seat, token);
                Debug.Log($"_tokenProvider.GetToken({seat.Index}) = {t}");
                var data = SwitchTokenData.FromToken(t);
                if (data.rtcUserId != seat.RtcUserId)
                    throw new Exception("The token is invalid. (RtcUserId mismatch!)");

                // cache
                _switchTokens.Add(seat.Index, data);
                return data;
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"回调 {callName} 被取消");
                throw;
            }
            catch (Exception e)
            {
                CGLogger.LogError($"回调 {callName} 异常, 请检查你的接入代码！ {e}");
                throw;
            }
            finally
            {
                float elapsedMs = (Time.realtimeSinceStartup - startTime) * 1000f;
                CGLogger.Log($"回调完成 {callName} 执行耗时: {elapsedMs:F2}ms");
            }

        }

        public string ID { get; private set; }

        public async Task<AnchorPlayerInfo> FetchPlayerInfo(ICloudSeat seat, CancellationToken token)
        {
            var data = await GetSwitchTokenData(seat, token);
            return data.anchor;
        }

        internal string GetNonHostStatesDebugInfo()
        {
            var nonHosts = CloudSyncSdk.InternalCurrent.GetNonHostClients();
            var debugInfo = string.Join(",", nonHosts.Select(s => $"{s.Index}: {s.State}"));
            return debugInfo;
        }
    }
}