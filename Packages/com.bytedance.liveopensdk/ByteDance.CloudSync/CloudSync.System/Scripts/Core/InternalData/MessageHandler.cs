using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

// ReSharper disable InconsistentNaming

namespace ByteDance.CloudSync
{
    internal static class MessagePools
    {
        public static readonly ObjectPool<PlayerOperateMessage> PlayerOperateMessagePool = new(() => new PlayerOperateMessage());
    }

    // note: CloudGameMessageBase 都是CloudGame模块内部message，不要暴露给模块外或TS
    // note: 目前 CloudGameMessageBase 的消息，都是用于内部的队列，主要解决sdk过来的回调信息是在子线程。
    internal abstract class CloudGameMessageBase
    {
        private SeatIndex _index;

        // ReSharper disable once InconsistentNaming
        public SeatIndex index
        {
            get => _index;
            set
            {
                _index = value;
                if (_index != UserInfo.Index)
                    UserInfo.SetIndex(_index);
            }
        }

        // 在哪一帧 enqueue 的
        public int writeFrame;
        // 在哪一帧 dequeue、被读取（消费） 的
        public int readFrame;

        public CloudUserInfo UserInfo;
    }

    internal interface ICloudGameMessageWriter
    {
        void Write(CloudGameMessageBase msg);
    }

    internal interface ICloudGameMessageReader
    {
        /// <summary>
        /// 读取所有非 input 事件
        /// </summary>
        /// <param name="messageQueue"></param>
        void ReadAll(Queue<CloudGameMessageBase> messageQueue);

        /// <summary>
        /// 读取所有 input 事件
        /// </summary>
        /// <param name="messageQueue"></param>
        void ReadAllInput(Queue<CloudGameMessageBase> messageQueue);
    }

    internal class MessageHandler : ICloudGameMessageWriter, ICloudGameMessageReader
    {
        private ConcurrentQueue<CloudGameMessageBase> _messages = new();
        private ConcurrentQueue<CloudGameMessageBase> _inputMessages = new();

        private int _readFrame;
        private int _writeFrame;

        public void Write(CloudGameMessageBase msg)
        {
            msg.writeFrame = _writeFrame;
            if (msg is PlayerOperateMessage opMsg)
            {
                // if (CloudGameSdk.IsVerboseLogForInput)
                //     CGLogger.Log($"Operate Write index: {msg.index}, {opMsg.operateData}");
                _inputMessages.Enqueue(msg);
            }
            else
            {
                _messages.Enqueue(msg);
            }
        }

        public void ReadAll(Queue<CloudGameMessageBase> messageQueue)
        {
            SetReadFrame();
            while (_messages.TryDequeue(out var msg))
            {
                messageQueue.Enqueue(msg);
            }

            SetWriteFrame();
        }

        public void ReadAllInput(Queue<CloudGameMessageBase> messageQueue)
        {
            SetReadFrame();
            while (_inputMessages.TryDequeue(out var msg))
            {
                msg.readFrame = _readFrame;
                messageQueue.Enqueue(msg);
            }

            SetWriteFrame();
        }

        private void SetReadFrame()
        {
            // 当前正要读取的messages，使用此readFrame值
            _readFrame = Time.frameCount;
        }

        private void SetWriteFrame()
        {
            // 后续写入的messages，使用此writeFrame值
            _writeFrame = Time.frameCount; // Time 接口不能在子线程访问
        }
    }
}