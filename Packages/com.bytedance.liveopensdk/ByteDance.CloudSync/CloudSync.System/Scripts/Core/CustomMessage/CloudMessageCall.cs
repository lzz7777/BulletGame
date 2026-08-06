// Copyright@www.bytedance.com
// Author: DONEY Dong
// Date: 2024/04/26
// Description:

using System;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace ByteDance.CloudSync
{
    internal class CloudMessageCall<TParam>: MessageCall<TParam>
    {
        public override string Tag => "CloudMessageCall";
        protected override string LogTag => "[CloudMessageCall] ";

        public CloudMessageCall(string name, Func<TParam, MessageCallResponse> func, bool retryEnabled = true) : base(name, func, retryEnabled)
        {
        }

        public CloudMessageCall(string name, Func<TParam, Task<MessageCallResponse>> asyncFunc, bool retryEnabled = true) : base(name, asyncFunc, retryEnabled)
        {
        }
    }
}