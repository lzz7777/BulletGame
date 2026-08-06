using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using MatchPb;

namespace ByteDance.CloudSync.Match
{
    public interface IMatchService: IDisposable
    {
        /// <summary>
        /// 获取当前直播间信息
        /// 注意：返回结果前请勿多次调用
        /// </summary>
        /// <param name="token">取消异步等待</param>
        /// <returns></returns>
        Task<GetWebCastInfoResult> GetWebCastInfo(CancellationToken token);

        /// <summary>
        /// 开始匹配操作
        /// </summary>
        /// <param name="matchInfo">中台云相关匹配配置</param>
        /// <param name="matchParamJson">Json格式的匹配参数</param>
        /// <param name="extraInfo">需要透传的字段，用户数据</param>
        /// <param name="token">用于取消匹配过程和等待过程</param>
        /// <returns>操作结果</returns>
        Task<MatchResult> StartMatch(MatchInfo matchInfo, string matchParamJson, string extraInfo, CancellationToken token);
    }

    public enum ResultCode
    {
        /// <summary>
        /// 请求完成，但具体是否成功要看各自的 StatusCode
        /// </summary>
        RequestDone,
        /// <summary>
        /// 用户取消
        /// </summary>
        UserCanceled,
        /// <summary>
        /// 网络错误
        /// </summary>
        NetworkError,
        /// <summary>
        /// 未知原因内部错误
        /// </summary>
        Undefined,
    }

    /// <summary>
    /// 匹配结果
    /// </summary>
    public struct MatchResult
    {
        public ResultCode Code;
        /// <summary>
        /// NetworkError 时的错误信息
        /// </summary>
        public string ErrorMsg;
        /// <summary>
        /// 协议交互正常，但匹配失败
        /// </summary>
        public MatchErrorNty Error;
        /// <summary>
        /// 协议交互正常，且匹配成功
        /// </summary>
        public MatchResultNty Result;
    }
    
    /// <summary>
    /// 获取直播间信息结果
    /// </summary>
    public struct GetWebCastInfoResult
    {
        public ResultCode Code;
        /// <summary>
        /// NetworkError 时的错误信息
        /// </summary>
        public string ErrorMsg;
        /// <summary>
        /// 协议交互正常，且获取信息成功
        /// </summary>
        public WebCastInfo Result;
    }

    /// <summary>
    /// 直播间信息
    /// </summary>
    public class WebCastInfo
    {
        /// <summary>
        /// 开放平台用户 open_id
        /// </summary>
        public string OpenID;
        /// <summary>
        /// 用户头像
        /// </summary>
        public string AvatarURL;
        /// <summary>
        /// 用户昵称
        /// </summary>
        public string NickName;
        /// <summary>
        /// 直播间id
        /// </summary>
        public long LiveRoomID;
    }
    
    
    public interface IConnectionManager: IDisposable
    {
        /// <summary>
        /// 设置是否需要保持和服务器的连接状态
        /// </summary>
        /// <param name="requireConnection"></param>
        void EnsureNetworkConnection(bool requireConnection);
        
        /// <summary>
        /// 是否正在连接或者重连
        /// </summary>
        bool IsConnectingOrRetrying { get; }
    }
    
    public interface IMessageManager: IDisposable
    {
        long GetWebCastInfo();
        
        
        /// <summary>
        /// 用于获取服务端多实例对象相关参数
        /// </summary>
        /// <param name="matchInfo">中台云相关匹配配置</param>
        /// <returns>请求ID</returns>
        long InitStartMatch(MatchInfo matchInfo);

        /// <summary>
        /// 发送匹配请求
        /// </summary>
        /// <param name="matchInfo">中台云相关匹配配置</param>
        /// <param name="matchParamJson">Json格式的匹配参数</param>
        /// <param name="extraInfo">需要透传的字段</param>
        /// <param name="targetModule">用于标识服务端多实例对象，从 InitStarkMatch 获取</param>
        /// <param name="targetId">用于标识服务端多实例对象，从 InitStarkMatch 获取</param>
        /// <returns>请求ID</returns>
        long StartMatch(MatchInfo matchInfo, string matchParamJson, string extraInfo, ulong targetModule = 0, ulong targetId = 0);

        /// <summary>
        /// 取消匹配请求
        /// </summary>
        /// <param name="matchInfo">中台云相关匹配配置</param>
        /// <param name="targetModule">用于标识服务端多实例对象，从 InitStarkMatch 获取</param>
        /// <param name="targetId">用于标识服务端多实例对象，从 InitStarkMatch 获取</param>
        /// <returns>请求ID</returns>
        long CancelMatch(MatchInfo matchInfo, ulong targetModule = 0, ulong targetId = 0);

        /// <summary>
        /// 消息接收事件
        /// </summary>
        event Action<long, MsgID, IMessage> OnMessage;
        
        /// <summary>
        /// 发送心跳，仅在匹配过程中需要
        /// <param name="targetModule">用于标识服务端多实例对象，从 InitStarkMatch 获取</param>
        /// <param name="targetId">用于标识服务端多实例对象，从 InitStarkMatch 获取</param>
        /// </summary>
        void HeartBeat(ulong targetModule = 0, ulong targetId = 0);
    }
}