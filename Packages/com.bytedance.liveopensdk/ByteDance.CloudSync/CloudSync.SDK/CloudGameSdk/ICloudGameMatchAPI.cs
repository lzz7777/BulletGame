using System.Threading.Tasks;

namespace ByteDance.CloudSync
{
    internal interface ICloudGameMatchAPI
    {
        /// <summary>
        /// 初始化，设置事件回调
        /// </summary>
        void InitMatchAPI(IMatchAPIListener listener);

        /// <summary>
        /// 发送匹配开始事件，gamesdk 侧向云游戏后端发起匹配请求，并在请求成功后通知 xplay 侧切流
        /// </summary>
        Task<ApiMatchStreamResponse> SendMatchBegin(ApiMatchParams matchParam);

        /// <summary>
        /// 发送匹配结束事件，gamesdk 向 xplay 发送结束，通知 xplay 侧切流（所有同玩用户，回流到单播）
        /// </summary>
        Task<ApiMatchStreamResponse[]> SendMatchEnd();

        /// <summary>
        /// 发送匹配结束事件，gamesdk 向 xplay 发送结束，通知 xplay 侧切流（单个用户，回流到单播）
        /// </summary>
        Task<ApiMatchStreamResponse> SendMatchEnd(int roomIndex);

        /// <summary>
        /// 发送Pod间透传消息
        /// </summary>
        /// <param name="token">目标流的云游戏token</param>
        /// <param name="msgData">消息数据</param>
        Task<ICloudGameAPI.Response> SendPodCustomMessage(string token, ApiPodMessageData msgData);
    }

    internal class ApiMatchParams : ByteCloudGameSdk.MatchParams
    {
        public ApiMatchParams Accept(ByteCloudGameSdk.MatchParams data)
        {
            // 目标主机的云游戏 token
            hostToken = data.hostToken;
            // 标识某一局匹配游戏, 目前仅用于做 debug，建议传入游戏侧匹配结果相关的id
            matchKey = data.matchKey;
            // 我自己的座位号
            roomIndex = data.roomIndex;
            return this;
        }

        public string ToStr() => $"{{ hostToken: {hostToken}, roomIndex: {roomIndex}, matchKey: {matchKey} }}";
    }

    internal class ApiMatchStreamResponse : ByteCloudGameSdk.MatchResponse
    {
        public bool IsSuccess => code == ByteCloudGameSdk.MatchErrorCode.Success;

        public ApiMatchStreamResponse Accept(ByteCloudGameSdk.MatchResponse data)
        {
            code = data.code;
            message = data.message;
            logId = data.logId;
            roomIndex = data.roomIndex;

            return this;
        }

        public string ToStr() => $"{{ code: {code} ({(int)code}), message: {message}, logId: {logId}, roomIndex: {roomIndex} }}";
    }

    /// 字段对齐 gamesdk 的 <see cref="ByteCloudGameSdk.PodMessage"/>.
    /// <remarks>
    /// 调用 CloudGameAPI 的 <see cref="CloudGameAPIWindows.SendPodCustomMessage"/> 发送透传消息。
    /// 底层调用 gamesdk 的 <see cref="ByteCloudGameSdk.Sdk.SendPodCustomMessage"/> 发送透传消息。
    /// </remarks>
    internal class ApiPodMessageData
    {
        /// 透传消息来源，例如可以传自己的openId
        public string from;

        /// 透传消息内容
        public string message;

        public ApiPodMessageData Accept(ByteCloudGameSdk.PodMessage data)
        {
            from = data.from;
            message = data.message;
            return this;
        }

        public string ToStr() => $"{{ from: {from}, message: {message} }}";
    }

    internal class ApiMatchCommandMessage : ByteCloudGameSdk.MatchCommandMessage
    {
        public ApiMatchCommandMessage Accept(ByteCloudGameSdk.MatchCommandMessage data)
        {
            command = data.command;
            rawCode = data.rawCode;
            code = data.code;
            message = data.message;
            roomIndex = data.roomIndex;
            return this;
        }

        public string ToStr()
        {
            return $"{command}, roomIndex: {roomIndex}, code: {code}, rawCode: {rawCode}, message: {message}";
        }
    }
}