using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using MatchPb;
using Newtonsoft.Json;
using StarkNetwork;

namespace ByteDance.CloudSync.Match
{
    public class MatchService: IMatchService
    {
        private NetworkClient _client;
        private IConnectionManager _connectionManager;
        private IMessageManager _messageManager;
        private static DebugLogger Debug { get; } = new();

        /// <summary>
        /// 用于记录当前操作的返回结果
        /// </summary>
        private readonly Dictionary<long, IMessage> _opResultMap = new Dictionary<long, IMessage>();
        /// <summary>
        /// 用于记录当前匹配的返回结果
        /// </summary>
        private readonly Dictionary<long, IMessage> _matchResultMap = new Dictionary<long, IMessage>();
        private readonly Dictionary<int, ServerInstanceConfig> _serverInstanceConfigMap = new Dictionary<int, ServerInstanceConfig>();

        private CancellationTokenSource _crtMatchTaskSource;

        private ServerInstanceConfig _crtServerInstanceConfig;

        private readonly AsyncTimer _heartBeatTimer;

        private WebCastInfo _webCastInfo;

        public static string GenerateConnectionToken(string appId, string webcastToken)
        {
            return JsonConvert.SerializeObject(new
            {
                type = "webcast",
                token = WebUtility.UrlEncode(webcastToken),
                appid = appId,
                proj_id = "",
            });
        }

        public MatchService(string ip, int port, string token, bool isFake = false)
        {
            Debug.Log("MatchService ctor");
            _client = new NetworkClient();
            // _client.SetDebugEnabled(false);
            NetworkDebugger.SetDebuggerType(NetworkDebuggerType.CLIENT, true);
            // NetworkDebugger.SetDebuggerType(NetworkDebuggerType.NATIVE, true);
            // NetworkDebugger.ActivateNativeDebug();
            _connectionManager = new ConnectionManager(_client, new ConnectOption().SetServer(ip, port).SetToken(token).SetIsFake(isFake));
            _connectionManager.EnsureNetworkConnection(true);
            _messageManager = new MessageManager(_client);
            _messageManager.OnMessage += OnMessage;

            _heartBeatTimer = new AsyncTimer();
        }

        public async void Reconnect(string ip, int port, string token, bool isFake = false)
        {
            _connectionManager?.Dispose();
            _connectionManager = null;
            await Task.Delay(500);
            _connectionManager = new ConnectionManager(_client, new ConnectOption().SetServer(ip, port).SetToken(token).SetIsFake(isFake));
            _connectionManager.EnsureNetworkConnection(true);
        }

        public async Task<MatchResult> StartMatch(MatchInfo matchInfo, string matchParamJson, string extraInfo, CancellationToken token)
        {
            // 包一层，用于重复调用时取消前一次操作
            if (_crtMatchTaskSource != null)
            {
                _crtMatchTaskSource.Cancel();
                _crtMatchTaskSource.Dispose();
                _crtMatchTaskSource = null;
                await Task.Delay(100, token); // 延迟 100 ms，避免重复调用时，下一次的开始匹配请求先早于前一次取消匹配请求发出
            }
            _crtMatchTaskSource = new CancellationTokenSource();
            var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(_crtMatchTaskSource.Token, token).Token;
            var result = await DoStartMatch(matchInfo, matchParamJson, extraInfo, combinedToken);
            _crtMatchTaskSource?.Dispose();
            _crtMatchTaskSource = null;
            return result;
        }

        private async Task<MatchResult> DoStartMatch(MatchInfo matchInfo, string matchParamJson, string extraInfo, CancellationToken token)
        {
            try
            {
                // 1. 确保网络链接
                _connectionManager.EnsureNetworkConnection(true);
                await WaitUtilConnectResultOrCanceled(token);
                token.ThrowIfCancellationRequested();

                if (_client.State == ClientState.STOP)
                {
                    return new MatchResult
                    {
                        Code = ResultCode.NetworkError,
                        ErrorMsg = "未能和服务器建立连接"
                    };
                }

                // 2. 数据预处理
                if (string.IsNullOrEmpty(matchInfo.MatchTag))
                {
                    matchInfo.MatchTag = "default"; // Server logic require this field.
                }

                // 3. 获取服务器多实例相关配置
                var (success, serverInstanceConfig) = await GetServerInstanceConfig(matchInfo, token);
                token.ThrowIfCancellationRequested();
                if (!success)
                {
                    return new MatchResult
                    {
                        Code = ResultCode.NetworkError,
                        ErrorMsg = "获取服务器多实例对象相关参数失败"
                    };
                }
                _crtServerInstanceConfig = serverInstanceConfig;

                // 4. 发送匹配请求 & 等待 ack
                var reqId = _messageManager.StartMatch(matchInfo, matchParamJson, extraInfo, _crtServerInstanceConfig.ModuleType, _crtServerInstanceConfig.InstanceId);
                var message = await WaitForResponseOrCancel(reqId, token);
                token.ThrowIfCancellationRequested();

                if (message is not StartMatchResp resp)
                {
                    return new MatchResult
                    {
                        Code = ResultCode.NetworkError,
                        ErrorMsg = "StartMatch 回包格式错误"
                    };
                }

                if (resp is not { StatusCode: { Code: 0 } })
                {
                    return new MatchResult
                    {
                        Code = ResultCode.NetworkError,
                        ErrorMsg = resp.StatusCode?.Message ?? "StartMatch 回包数据错误"
                    };
                }

                StartHeartBeat(resp.HeartbeatInterval);

                // 5. 等待匹配结果
                var matchResult = await WaitForMatchResultOrCancel(reqId, token);
                if (token.IsCancellationRequested)
                {
                    // Notify the server.
                    CancelMatch(matchInfo, _crtServerInstanceConfig.ModuleType, _crtServerInstanceConfig.InstanceId);
                }
                token.ThrowIfCancellationRequested();
                if (matchResult is MatchResultNty matchResultNty)
                {
                    return new MatchResult
                    {
                        Code = ResultCode.RequestDone,
                        Result = matchResultNty
                    };
                }
                else if (matchResult is MatchErrorNty matchErrorNty)
                {
                    return new MatchResult
                    {
                        Code = ResultCode.RequestDone,
                        Error = matchErrorNty
                    };
                }
            }
            catch (OperationCanceledException)
            {
                return new MatchResult
                {
                    Code = ResultCode.UserCanceled
                };
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return new MatchResult
                {
                    Code = ResultCode.Undefined
                };
            }
            finally
            {
                // 6. 释放资源
                _connectionManager.EnsureNetworkConnection(false);
                StopHeartBeat();
                _crtServerInstanceConfig = default;
            }
            // no way to reach here
            return default;
        }

        /// <summary>
        /// 获取服务端多实例对象相关参数，如果有缓存则直接返回，否则发送请求获取
        /// </summary>
        /// <param name="matchInfo"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task<(bool, ServerInstanceConfig)> GetServerInstanceConfig(MatchInfo matchInfo, CancellationToken token)
        {
            var hash = matchInfo.GetHashCode();
            if (_serverInstanceConfigMap.TryGetValue(hash, out var config))
            {
                return (true, config);
            }

            var reqId = _messageManager.InitStartMatch(matchInfo);
            var message = await WaitForResponseOrCancel(reqId, token);
            if (token.IsCancellationRequested)
            {
                return (false, default);
            }
            if (message is not InitStarkMatchResp { StatusCode: { Code: 0 } } resp)
            {
                return (false, default);
            }
            var serverInstanceConfig = new ServerInstanceConfig
            {
                ModuleType = resp.ModuleType,
                InstanceId = resp.MatchPoolInstId
            };
            _serverInstanceConfigMap.Add(hash, serverInstanceConfig);
            return (true, serverInstanceConfig);
        }

        private void OnTimerEvent()
        {
            _messageManager.HeartBeat(_crtServerInstanceConfig.ModuleType, _crtServerInstanceConfig.InstanceId);
        }

        private void StartHeartBeat(int interval)
        {
            StopHeartBeat();
            if (interval <= 0)
            {
                return;
            }
            // _messageManager.HeartBeat();    // 初次发送一次心跳
            _heartBeatTimer.Start(interval * 1000, OnTimerEvent);
        }

        private void StopHeartBeat()
        {
            _heartBeatTimer.Stop();
        }

        private void CancelMatch(MatchInfo matchInfo, ulong targetModule, ulong targetId)
        {
            if (_client.State == ClientState.STOP)
            {
                return;
            }

            _messageManager.CancelMatch(matchInfo, targetModule, targetId);
        }

        public bool IsConnected
        {
            get
            {
                return _client?.State == ClientState.RUNNING;
            }
        }

        public async Task<GetWebCastInfoResult> GetWebCastInfo(CancellationToken token)
        {
            try
            {
                return await DoGetWebCastInfo(token);
            }
            catch (OperationCanceledException)
            {
                return new GetWebCastInfoResult { Code = ResultCode.UserCanceled };
            }
        }

        private async Task<GetWebCastInfoResult> DoGetWebCastInfo(CancellationToken token)
        {
            if (_webCastInfo != null)
            {
                return new GetWebCastInfoResult { Code = ResultCode.RequestDone, Result = _webCastInfo };
            }
            _connectionManager.EnsureNetworkConnection(true);
            await WaitUtilConnectResultOrCanceled(token);
            token.ThrowIfCancellationRequested();
            if (_client.State == ClientState.STOP)
            {
                return new GetWebCastInfoResult
                {
                    Code = ResultCode.NetworkError,
                    ErrorMsg = "未能和服务器建立连接"
                };
            }

            var reqId = _messageManager.GetWebCastInfo();
            var message = await WaitForResponseOrCancel(reqId, token);
            token.ThrowIfCancellationRequested();

            if (message is not GetWebCastInfoResp resp)
            {
                return new GetWebCastInfoResult
                {
                    Code = ResultCode.NetworkError,
                    ErrorMsg = "GetWebCastInfo 回包格式错误"
                };
            }

            if (resp is not { StatusCode: { Code: 0 } })
            {
                return new GetWebCastInfoResult
                {
                    Code = ResultCode.NetworkError,
                    ErrorMsg = resp.StatusCode?.Message ?? "GetWebCastInfo 回包数据错误"
                };
            }

            _webCastInfo = new WebCastInfo
            {
                OpenID = resp.OpenId,
                AvatarURL = resp.AvatarUrl,
                NickName = resp.NickName,
                LiveRoomID = resp.LiveRoomId,
            };

            return new GetWebCastInfoResult { Code = ResultCode.RequestDone, Result = _webCastInfo };
        }

        private void OnMessage(long requestId, MsgID msgType,  IMessage msg)
        {
            switch (msgType)
            {
                case MsgID.StartMatchResp:
                // case MsgID.CancelMatchResp:
                case MsgID.GetWebCastInfoResp:
                case MsgID.InitStarkMatchResp:
                    _opResultMap.TryAdd(requestId, msg);
                    break;
                case MsgID.MatchErrorNty:
                case MsgID.MatchResultNty:
                    _matchResultMap.TryAdd(requestId, msg);
                    break;
            }
        }

        public async Task WaitUtilConnectResultOrCanceled(CancellationToken token)
        {
            while (_connectionManager.IsConnectingOrRetrying && !token.IsCancellationRequested)
            {
                await Task.Yield();
            }
        }

        private async Task<IMessage> WaitForResponseOrCancel(long requestId, CancellationToken token)
        {
            IMessage result = null;
            while (!_opResultMap.TryGetValue(requestId, out result) && !token.IsCancellationRequested)
            {
                await Task.Yield();
            }

            _opResultMap.Remove(requestId);
            return result;
        }

        private async Task<IMessage> WaitForMatchResultOrCancel(long requestId, CancellationToken token)
        {
            IMessage result = null;
            while (!_matchResultMap.TryGetValue(requestId, out result) && !token.IsCancellationRequested)
            {
                await Task.Yield();
            }

            _matchResultMap.Remove(requestId);
            return result;
        }

        public void Dispose()
        {
            // NetworkDebugger.DeactivateNativeDebug();
            _connectionManager?.Dispose();
            _connectionManager = null;
            _messageManager.OnMessage -= OnMessage;
            _messageManager?.Dispose();
            _messageManager = null;
            _opResultMap.Clear();
            _matchResultMap.Clear();
            _serverInstanceConfigMap.Clear();
            _crtMatchTaskSource?.Dispose();
            _crtMatchTaskSource = null;
            StopHeartBeat();
            _heartBeatTimer.Dispose();
            _client?.Dispose();
            _client = null;
        }
    }
}