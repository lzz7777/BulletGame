// Copyright@www.bytedance.com
// Author: DONEY Dong
// Date: 2024/04/25
// Description:

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// ReSharper disable MemberCanBePrivate.Global

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnassignedGetOnlyAutoProperty
// ReSharper disable RedundantDefaultMemberInitializer
// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace
namespace ByteDance.CloudSync
{
    internal enum MessageCallState
    {
        None,
        InProgress,
        Success,
        Failed,
    }

    internal enum MessageCallMethodType
    {
        Func,
        AsyncFunc,
    }

    internal struct MessageCallResponse
    {
        public bool Success;
        public int StatusCode;
        public string ErrorMsg;
        public Exception Exception;

        public MessageCallResponse(bool success, int statusCode)
        {
            Success = success;
            StatusCode = statusCode;
            ErrorMsg = null;
            Exception = null;
        }


        public MessageCallResponse(bool success, int statusCode, string errorMsg)
        {
            Success = success;
            StatusCode = statusCode;
            ErrorMsg = errorMsg;
            Exception = null;
        }

        public MessageCallResponse(Exception e)
        {
            Success = false;
            StatusCode = 4;
            ErrorMsg = e.Message;
            Exception = e;
        }
    }

    internal interface IMessageFailHandler
    {
        void Handle(IMessageCall call, bool retry);
    }

    // todo: [端上交互] 长连消息可靠性：
    internal interface IMessageCall
    {
        public void CallRetry();
    }

    internal interface IMessageCall<TParam> : IMessageCall
    {
        public TParam Param { get; }

        public Func<TParam, MessageCallResponse> Func { get; }
        public Func<TParam, Task<MessageCallResponse>> AsyncFunc { get; }

        public void Call(TParam param, IMessageFailHandler failRetryHandler);
        public Task CallAsync(TParam param, IMessageFailHandler failRetryHandler);
    }

    // todo: [端上交互] 长连消息可靠性：
    /// 消息调用，包含计数器、可配置重试机制。失败后自动退避重试、成功后清空退避状态
    internal class MessageCall<TParam> : IMessageCall<TParam>
    {
        public virtual string Tag => "MessageCall";
        protected virtual string LogTag => "[MessageCall] ";
        public string Name { get; }
        public string CallID => $"{InstanceName}#{CallCount}";
        public Type ParamType { get; }
        public MessageCallState State { get; protected set; }
        public MessageCallState PrevState { get; protected set; }

        public MessageCallResponse CallResponse { get; protected set; }
        public Exception Exception { get; protected set; }

        public TParam Param { get; protected set; }

        public MessageCallMethodType MethodType { get; }
        public Func<TParam, MessageCallResponse> Func { get; }
        public Func<TParam, Task<MessageCallResponse>> AsyncFunc { get; }

        public IMessageFailHandler RetryHandler { get; set; }

        /// 状态：已调用次数，正常只累积、不归零
        public int CallCount { get; private set; } = 0;

        /// 状态：已重试次数，并以此退避，并在成功后重置归零
        public int RetryCount { get; private set; } = 0;

        /// 状态：重试等待时间（秒），按已重试次数退避增加
        public int RetryWaitSec
        {
            get
            {
                var count = RetryCount;
                count = count <= 0 ? 0 : count;
                var wait = Conf_RetryWaitSec + count * Conf_RetryWaitSecPerCount;
                wait = wait <= 0 ? 0 : wait;
                return wait;
            }
        }

        /// 配置：重试开启
        /// <remarks>特殊情况例如app退出，应阻止重试</remarks>
        // ReSharper disable once MemberInitializerValueIgnored
        public bool RetryEnabled { get; set; } = true;

        /// 配置：重试等待时长（秒）
        public int Conf_RetryWaitSec { get; set; } = 1;

        /// 配置：按重试次数的等待时长（秒），形成退避作用
        public int Conf_RetryWaitSecPerCount { get; set; } = 1;

        /// 配置：重试次数上限，达到后不再重试
        public int Conf_RetryCountLimit { get; set; } = 10;

        public string CallStateMsg => $"call {CallID}: {State}";
        public string CallStateChangeMsg => $"call {CallID}: {PrevState} -> {State}";

        internal long InstanceIndex { get; private set; }
        internal string InstanceName { get; private set; }

        private static SdkDebugLogger Debug => CloudGameSdkManager.Debug;
        private static readonly Dictionary<string, long> s_namedCounts = new Dictionary<string, long>();

        // ReSharper disable once MemberCanBeProtected.Global
        public MessageCall(string name, Func<TParam, MessageCallResponse> func, bool retryEnabled = true)
        {
            Name = name;
            InitInstanceIndex(name);
            Func = func;
            MethodType = MessageCallMethodType.Func;
            ParamType = typeof(TParam);
            RetryEnabled = retryEnabled;
        }

        // ReSharper disable once MemberCanBeProtected.Global
        public MessageCall(string name, Func<TParam, Task<MessageCallResponse>> asyncFunc, bool retryEnabled = true)
        {
            Name = name;
            InitInstanceIndex(name);
            AsyncFunc = asyncFunc;
            MethodType = MessageCallMethodType.AsyncFunc;
            ParamType = typeof(TParam);
            RetryEnabled = retryEnabled;
        }

        private void InitInstanceIndex(string name)
        {
            var counts = s_namedCounts;
            if (counts.TryGetValue(name, out long index))
                index = counts[name] + 1;
            else
                index = 1;
            counts[name] = index;
            InstanceIndex = index;
            InstanceName = index > 1 ? $"{Name}_{index}" : Name;
        }

        public void CallRetry()
        {
            Call(Param, RetryHandler);
        }

        public async void Call(TParam param, IMessageFailHandler failRetryHandler)
        {
            await CallAsync(param, failRetryHandler);
        }

        public async Task CallAsync(TParam param, IMessageFailHandler failRetryHandler)
        {
            Param = param;
            RetryHandler = failRetryHandler;
            Exception = null;
            CallResponse = new MessageCallResponse
            {
                StatusCode = -1,
            };

            try
            {
                switch (MethodType)
                {
                    case MessageCallMethodType.Func:
                        if (!ValidateMethod(Func))
                            return;
                        Process1_InProgress();
                        CallResponse = Func.Invoke(Param);
                        break;

                    case MessageCallMethodType.AsyncFunc:
                        if (!ValidateMethod(AsyncFunc))
                            return;
                        Process1_InProgress();
                        CallResponse = await AsyncFunc.Invoke(Param);
                        break;
                }
            }
            catch (Exception e)
            {
                CallResponse = new MessageCallResponse(e);
                Debug.LogError(LogTag + $"{CallID} exception: " + e);
            }

            if (CallResponse is { Success: true, Exception: null })
            {
                Process2_Success();
                return;
            }

            bool retry = await Process3_FailRetry(null);
            RetryHandler?.Handle(this, retry);
        }

        private bool ValidateMethod<TMethod>(TMethod func)
        {
            if (func != null)
                return true;
            Debug.LogError(LogTag + $"{CallID} func ${nameof(TMethod)} is null");
            return false;
        }

        /// <summary>
        /// 过程 1. 在即将调用API前，标志`InProgress`状态。也使其他调用能判断到已进行、或即将准备进行
        /// </summary>
        protected void Process1_InProgress()
        {
            State = MessageCallState.InProgress;
            ++CallCount;
            // CheckAssertState(State, APIState.InProgress);
            Debug.Log(LogTag + CallStateChangeMsg);
        }

        /// <summary>
        /// 过程 2. 调用API成功，设置`Success`状态
        /// </summary>
        protected void Process2_Success()
        {
            RetryCount = 0;
            State = MessageCallState.Success;
            Debug.Log(LogTag + $"{CallStateChangeMsg} 成功");
        }

        /// <summary>
        /// 过程 3. 调用API失败，设置`Failed`状态、决定是否要重试、等待重试
        /// </summary>
        /// <param name="extraGiveUpReasoner">额外放弃原因回调，做额外的重试条件判断。 如果返回空，表示允许重试； 如果返回有内容字符串，表示不要重试、并返回放弃重试原因</param>
        /// <returns>isRetry 是否继续重试</returns>
        protected async Task<bool> Process3_FailRetry(Func<string> extraGiveUpReasoner)
        {
            State = MessageCallState.Failed;
            var isRetry = Is3_FailedAndKeepRetry(extraGiveUpReasoner, out var reason);
            if (!isRetry)
            {
                // 如果放弃重试，`RetryCount`归零
                RetryCount = 0;
                Debug.LogWarning(LogTag + $"{CallStateChangeMsg} 放弃重试 reason: {reason}");
                return false;
            }

            // 4.1
            isRetry = await Set41_AwaitNextRetryState();
            if (!isRetry)
                return false;

            // 4.2
            Set42_AddRetry();
            return true;
        }

        /// <summary>
        /// 子过程 3.0 仅作判断，是否API失败、并判断是否要重试
        /// </summary>
        /// <param name="extraGiveUpReasoner">放弃原因回调，做额外的条件判断后，如果返回空，表示条件通过；如果返回有内容字符串，表示不通过、放弃重试的的原因</param>
        /// <param name="giveUpReason">决定放弃的原因</param>
        /// <returns>是否继续重试</returns>
        protected bool Is3_FailedAndKeepRetry(Func<string> extraGiveUpReasoner, out string giveUpReason)
        {
            giveUpReason = string.Empty;
            var keepRetry = true;
            if (State == MessageCallState.InProgress)
            {
                keepRetry = false;
                giveUpReason = "进行中，不重试";
            }

            if (State == MessageCallState.Success)
            {
                keepRetry = false;
                giveUpReason = "已成功，不重试";
            }

            // todo: IsAppQuitting
            // if (keepRetry && QuitHelper.IsAppQuitting())
            // {
            //     keepRetry = false;
            //     giveUpReason = "app退出";
            // }

            if (keepRetry && !RetryEnabled)
            {
                keepRetry = false;
                giveUpReason = "配置为不重试";
            }

            // 检查放弃原因：达到重试次数上限
            if (keepRetry && RetryCount >= Conf_RetryCountLimit)
            {
                keepRetry = false;
                giveUpReason = $"重试次数达到上限 (retry count: {RetryCount} / {Conf_RetryCountLimit})";
            }

            // 检查其他放弃原因：例如请求为协议错误（不是网络错误）
            if (keepRetry && extraGiveUpReasoner != null)
            {
                try
                {
                    giveUpReason = extraGiveUpReasoner();
                }
                catch (Exception e)
                {
                    Debug.LogError(LogTag + e);
                }
            }

            keepRetry = string.IsNullOrEmpty(giveUpReason);
            if (!keepRetry)
            {
                return false;
            }

            Debug.Log(LogTag + $"{CallStateMsg} 可重试 (retry count: {RetryCount} / {Conf_RetryCountLimit})");
            return true;
        }

        /// <summary>
        /// 子过程 4.1 确定并等待下次重试，标志`InProgress`状态，并进行等待。
        /// </summary>
        protected async Task<bool> Set41_AwaitNextRetryState()
        {
            State = MessageCallState.InProgress;
            var waitSec = RetryWaitSec;
            Debug.Log(LogTag + $"{CallStateChangeMsg} 等待重试 wait {waitSec}s");

            await Task.Delay(new TimeSpan(0, 0, waitSec));

            // check app after time elapsed
            if (!ValidateAppState(Name))
            {
                RetryEnabled = false;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 子过程 4.2 执行重试前，递增`RetryCount`表示第n次重试
        /// </summary>
        protected void Set42_AddRetry()
        {
            RetryCount++;
            // CheckAssertState(State, APIState.InProgress);
            Debug.Log(LogTag + $"{CallStateMsg} 进行重试 retry #{RetryCount}");
        }

        /// <summary>
        /// 检查app状态。 如果app退出，返回false
        /// </summary>
        protected bool ValidateAppState(string caller)
        {
            // todo: IsAppQuitting
            // if (QuitHelper.IsAppQuitting())
            // {
            //     Debug.Log(LogTag + $"\"{caller}\" stop. app is quiting.");
            //     return false;
            // }

            return true;
        }
    }
}