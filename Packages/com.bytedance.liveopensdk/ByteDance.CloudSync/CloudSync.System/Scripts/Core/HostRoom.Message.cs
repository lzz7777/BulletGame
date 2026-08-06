using System.Threading;
using System.Threading.Tasks;
using ByteDance.CloudSync.MatchManager;
using Newtonsoft.Json;

namespace ByteDance.CloudSync
{
    internal partial class HostRoom
    {
        // 简单形式 1
        private async Task<ICloudGameAPI.Response> SendPodMessage(ICloudSeat seat, string message, MatchPodMessageType type)
        {
            // 用 get AnchorPlayerInfo 已缓存信息，来保证可靠性
            var hostInfo = CloudSyncSdk.InternalCurrent.AnchorPlayerInfo;
            if (hostInfo == null)
                Debug.LogError("_EndMatchSendInfo hostInfo is null!");
            var fromId = hostInfo?.OpenId ?? "";

            // 自定义封装一层消息协议格式
            var wrapMessage = new MatchPodMessage
            {
                type = type,
                endType = EndEventType.EndMatchGame,
                info = message,
                fromId = fromId,
                // todo: implement 唯一标识玩家参与一次同玩的会话id
                playerSessionId = null
            };
            var wrapMessageJson = JsonConvert.SerializeObject(wrapMessage);
            var msgData = new ApiPodMessageData
            {
                from = fromId,
                message = wrapMessageJson
            };

            var switchTokenData = await GetSwitchTokenData(seat, CancellationToken.None);

            var op = new PodMessageOperation
            {
                Index = seat.Index,
                Token = switchTokenData.hostToken,
                MsgData = msgData
            };
            return await op.Run();
        }

        /// <summary>
        /// 发送透传消息
        /// </summary>
        public Task<ICloudGameAPI.Response> SendPodMessage(SeatIndex roomIndex, string message, MatchPodMessageType type)
        {
            var seat = CloudSyncSdk.InternalCurrent.SeatManager.GetSeat(roomIndex);
            return SendPodMessage(seat, message, type);
        }

        /// <summary>
        /// 发送结束同玩的透传消息
        /// </summary>
        internal async Task<MatchEndPodMsgResponse> SendMatchEndPodMessage(SeatIndex roomIndex, string message, MatchPodMessageType type)
        {
            var response = await SendPodMessage(roomIndex, message, type);
            return new MatchEndPodMsgResponse().Accept(roomIndex, response);
        }
    }
}