using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace ByteDance.CloudSync.Mock
{
    /// <summary>
    /// Mock的客户端Rtc流，可以从流中获取画面帧，并可以对其发送输入事件
    /// </summary>
    public interface IClientRtcStream : IDisposable
    {
        /// <summary>
        /// 拉流，获取画面帧。
        /// 会由 <see cref="MockPlay"/> 和 <see cref="ClientRtc"/> 调用来拉流。
        /// </summary>
        Texture GetVideoFrame();

        /// <summary>
        /// 输入事件发送器。
        /// 会由 <see cref="MockPlay"/> 和 <see cref="ClientRtc"/> 调用该接口发送输入事件，给云端Pod，并由<see cref="PodRtcRoom"/>接收消息。
        /// </summary>
        IInputEventSender InputEventSender { get; }

        Task Init();
    }

    public class MockLocalStream : IClientRtcStream
    {
        private readonly SeatIndex _index;

        public MockLocalStream(SeatIndex index)
        {
            _index = index;
            InputEventSender = new MockInputEventSender(index);
        }

        public void Dispose()
        {
        }

        public IInputEventSender InputEventSender { get; }

        public Texture GetVideoFrame() => VirtualDeviceSystem.Find(_index)?.Screen.RenderTexture;

        public Task Init()
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 可切换的Rtc流，可以切流到目标Rtc房间后，从流中获取画面帧，并可以对其发送输入事件
    /// </summary>
    internal class MockSwitchableRtcStream : IClientRtcStream
    {
        private IClientRtcStream _currentSource;
        private readonly Stack<IClientRtcStream> _sourceStack = new ();

        /// <summary>
        /// 切流
        /// </summary>
        /// <param name="options">目标Rtc房间参数</param>
        /// <param name="index">目标座位 index</param>
        /// <param name="rtcUuid">我方（请求连接者）的uuid</param>
        /// <param name="isLocalDevice">使用本地设备。 true: 不需要 Rtc 拉流，直接读取本地设备画面</param>
        public async Task<bool> SwitchRtc(RtcConnectOptions options, int index, string rtcUuid, bool isLocalDevice)
        {
            var clientRtc = new ClientRtc();
            var ok = await clientRtc.Connect(options, rtcUuid, isLocalDevice, index);
            if (ok) await Push(clientRtc);
            else clientRtc.Dispose();
            return ok;
        }

        private async Task Push(IClientRtcStream source)
        {
            await source.Init();
            _sourceStack.Push(source);
            _currentSource = source;
        }

        public async Task Pop()
        {
            _sourceStack.Pop();

            var next = _sourceStack.Peek();
            await next.Init();
            _currentSource = next;
        }

        public void Clean()
        {
            foreach (var device in _sourceStack)
            {
                device.Dispose();
            }
            _sourceStack.Clear();
        }

        public void Dispose()
        {
            Clean();
        }

        public IInputEventSender InputEventSender => _currentSource?.InputEventSender;

        /// <summary>
        /// 从当前来源流，获取画面帧。
        /// 会由 <see cref="MockPlay"/> 和 <see cref="ClientRtc"/> 调用来拉流。
        /// </summary>
        /// <inheritdoc cref="IClientRtcStream.GetVideoFrame"/>
        public Texture GetVideoFrame() => _currentSource?.GetVideoFrame();

        public Task Init()
        {
            if (_currentSource == null)
                return Task.CompletedTask;

            return _currentSource.Init();
        }
    }
}