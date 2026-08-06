// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ByteDance.CloudSync
{
    internal partial class CloudSeat : ICloudSeat
    {
        public CloudSeat(int i)
        {
            Index = (SeatIndex)i;
            State = SeatState.Empty;
        }

        private const string Tag = "CloudSeat";

        private static readonly SdkDebugLogger Debug = new(Tag);

        /// <inheritdoc cref="ICloudSeat.OnSeatPlayerJoined"/>
        public event SeatEventHandler OnSeatPlayerJoined;

        /// <inheritdoc cref="ICloudSeat.OnSeatPlayerLeaving"/>
        public event SeatEventHandler OnSeatPlayerLeaving;

        /// <inheritdoc cref="ICloudSeat.OnWillDestroy"/>
        public event WillDestroyHandler OnWillDestroy;

        public SeatIndex Index { get; }

        public int IntIndex => (int)Index;

        public SeatState State { get; private set; }

        string ICloudSeat.RtcUserId => _client?.RtcUserId;
        public string RtcUserId => _client?.RtcUserId;

        public IPlayerInfo PlayerInfo => _client?.PlayerInfo;

        CloudClient ICloudSeat.Client => _client;

        public CloudClient Client => _client;
        internal ICloudClient IClient => _client;

        public ICloudView View => _view;

        private CloudClient _client;
        private ICloudView _view;

        public void EndMatchGame(string endInfo = null)
        {
            IClient?.EndMatchGame(endInfo);
        }

        public async Task WaitJoin(CancellationToken token)
        {
            Debug.Log($"Wait connecting: {IntIndex}");
            Stopwatch stopwatch = null;
            float lastCheck = 0;
            float lastError = 0;
            while (_client == null || !_client.IsConnected)
            {
                stopwatch ??= Stopwatch.StartNew();
                var elapsed = stopwatch.ElapsedMilliseconds / 1000f;
                if (elapsed < 5f && elapsed - lastCheck >= 1f)
                {
                    lastCheck = elapsed;
                    var msg = IntIndex == 0 ? "云同步-等待房主加入" : "云同步-等待用户加入";
                    Debug.Log($"{msg} wait join index: {IntIndex}, elapsed: {elapsed:F0}s");
                }

                if (elapsed - lastError >= 5f)
                {
                    lastError = elapsed;
                    var msg = IntIndex == 0 ? "云同步-等待房主加入超时！" : "云同步-等待用户加入超时!";
                    Debug.LogError($"{msg} wait join index: {IntIndex}, elapsed: {elapsed:F0}s");
                }

                token.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            stopwatch?.Stop();
            Debug.Log($"Wait connecting ok: {IntIndex}");
        }

        public void Bind(CloudClient client)
        {
            _client = client;
            _view = client.Device.Screen.View as ICloudView;
        }

        public async void Join(PlayerConnectingMessage message)
        {
            Debug.Log($"Seat Joining... 云同步-用户加入中... (index: {IntIndex})");
            if (_client == null)
            {
                Debug.LogError("Client is null");
                return;
            }
            await _client.OnConnecting(message);
            State = SeatState.InUse;
            Debug.Log($"Seat Joined 云同步-事件：用户加入了 (index: {IntIndex}) {ToUserInfo(_client.PlayerInfo)}");
            InvokeEvent($"{nameof(ICloudView)}.{nameof(ICloudView.OnPlayerJoined)}, seat: {Index}",() => _view?.OnPlayerJoined(this));
            InvokeEvent($"{nameof(ICloudSeat)}.{nameof(OnSeatPlayerJoined)}, seat: {Index}",() => OnSeatPlayerJoined?.Invoke(this));
            CloudSyncSdk.InternalCurrent?.OnSeatStateChanged(this, State);
        }

        public void Leave(CloudClient client)
        {
            var info = client.PlayerInfo;
            Debug.Log($"Seat Leave 云同步-事件：用户离开 (index: {IntIndex}) {ToUserInfo(_client.PlayerInfo)}");
            if (_client == client)
            {
                State = SeatState.Empty;
                InvokeEvent($"{nameof(ICloudView)}.{nameof(ICloudView.OnPlayerLeaving)}, seat: {Index}",() => _view?.OnPlayerLeaving(this));
                InvokeEvent($"{nameof(ICloudSeat)}.{nameof(OnSeatPlayerLeaving)}, seat: {Index}",() => OnSeatPlayerLeaving?.Invoke(this));
                CloudSyncSdk.InternalCurrent?.OnSeatStateChanged(this, State);

                CleanPushService();
                _client.Dispose();
                _client = null;
                _view = null;
            }
        }

        private string ToUserInfo(IPlayerInfo info)
        {
            if (info == null)
                return string.Empty;
            if (info is IAnchorPlayerInfo anchor)
                return $"({anchor.NickName} OpenId: {anchor.OpenId} LiveRoomId: {anchor.LiveRoomId})";
            return $"({info.NickName} OpenId: {info.OpenId})";
        }

        private static void InvokeEvent(string name, Action action) => CloudSyncSdk.InvokeEvent(name, action);

        public void Dispose()
        {
            CleanPushService();
            _client?.Dispose();
            _client = null;
        }

        public void WillDestroy(DestroyInfo destroyInfo)
        {
            try
            {
                Debug.Log($"seat {IntIndex} Notify event: WillDestroy, Reason: {destroyInfo.Reason}");
                InvokeEvent($"{nameof(ICloudView)}.{nameof(ICloudView.OnWillDestroy)}, seat: {Index}",() => _view?.OnWillDestroy(this, destroyInfo));
                InvokeEvent($"{nameof(ICloudSeat)}.{nameof(OnWillDestroy)}, seat: {Index}",() => OnWillDestroy?.Invoke(destroyInfo));
            }
            catch (Exception e)
            {
                Debug.LogError($"OnWillDestroy Exception! index: {IntIndex}, {e}");
            }
        }
    }
}