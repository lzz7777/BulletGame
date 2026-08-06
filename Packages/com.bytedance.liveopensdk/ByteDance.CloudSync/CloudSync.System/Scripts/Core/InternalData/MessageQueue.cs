// Copyright@www.bytedance.com
// Author: zhouxu.ken
// Date: 2024/06/11
// Description:

using System.Collections.Generic;

namespace ByteDance.CloudSync
{
    internal class MessageQueue : IMessageFailHandler
    {
        private readonly Queue<IMessageCall> _retryMessageQueue = new();
        private readonly List<IMessageCall> _reusedList = new();

        public int MessageRetryCount { get; set; } = 5;

        void IMessageFailHandler.Handle(IMessageCall call, bool retry)
        {
            if (retry)
                _retryMessageQueue.Enqueue(call);
        }

        public void Process()
        {
            var queue = _retryMessageQueue;
            if (queue.Count == 0) return;

            while (queue.TryDequeue(out var call))
            {
                _reusedList.Add(call);
            }

            foreach (var call in _reusedList)
            {
                call.CallRetry();
            }

            _reusedList.Clear();
        }
    }
}