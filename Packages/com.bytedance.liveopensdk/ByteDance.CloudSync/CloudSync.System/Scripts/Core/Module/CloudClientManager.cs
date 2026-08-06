using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

// ReSharper disable once CheckNamespace
namespace ByteDance.CloudSync
{
    internal class CloudClientManager : ICloudClientManager, ICloudManager, ICloudSeatManager
    {
        private readonly IVirtualDeviceFactory _deviceFactory;

        private readonly CloudSeat[] _seats = new CloudSeat[(int)SeatIndex.MaxIndex3 + 1];

        private ICloudGameMessageReader _messageReader;

        private readonly Queue<CloudGameMessageBase> _tempMessageQueue = new();

        public CloudClientManager(IVirtualDeviceFactory deviceFactory)
        {
            _deviceFactory = deviceFactory;
            CGLogger.Log($"CloudClientManager ctor, frame: {Time.frameCount}");
        }

        public void Initialize()
        {
            CGLogger.Log($"CloudClientManager Initialize, frame: {Time.frameCount}");
            _messageReader = CloudSyncSdk.SdkManager.MessageHandler;

            // init seats
            for (var i = 0; i < _seats.Length; i++)
            {
                var seat = new CloudSeat(i);
                _seats[i] = seat;
            }

            // ReSharper disable once UseIndexFromEndExpression
            if (_seats.Length >= 1)
            {
                var seat0 = _seats[0];
                var seatMax = _seats[_seats.Length - 1];
                CGLogger.Log($"Seat created: {seat0.IntIndex} ({seat0.Index}) ~ {seatMax.IntIndex} ({seatMax.Index})");
            }
        }

        public void Update()
        {
            // 处理 Input 之外的消息
            ReadNonInputMessages();
        }

        public void EarlyUpdate()
        {
            // Input 事件在 EarlyUpdate 阶段（UpdateInputManager 之前）处理，在当前帧就派发给上层，避免延迟
            ReadInputMessages();
        }

        private void ReadNonInputMessages()
        {
            var queue = GetTempMessageQueue();
            _messageReader.ReadAll(queue);
            while (queue.TryDequeue(out var message))
            {
                ProcessMessage(message);
            }
        }

        private void ReadInputMessages()
        {
            var queue = GetTempMessageQueue();
            _messageReader.ReadAllInput(queue);
            while (queue.TryDequeue(out var message))
            {
                ProcessMessage(message);
            }
        }

        private Queue<CloudGameMessageBase> GetTempMessageQueue()
        {
            _tempMessageQueue.Clear();
            return _tempMessageQueue;
        }

        private void ProcessMessage(CloudGameMessageBase message)
        {
            CloudSeat seat = null;
            if (message is PlayerConnectingMessage connectingMessage)
            {
                seat = CreateClient(message.index);
                seat?.Join(connectingMessage);
                return;
            }

            var index = message.index;
            var userInfo = message.UserInfo;
            var userId = userInfo.RtcUserId;

            var log = message.ToString();
            {
                if (message is PlayerOperateMessage)
                    log = null;
                // 优先信任 rtcUserId、及其 message.index 。 避免上游其他用户信息bug时带来严重错误
                bool found = false, foundUser = false, foundIndex = false;
                if (!string.IsNullOrEmpty(userId))
                    found = foundUser = TryGetSeatBy(item => item.RtcUserId == userId, out seat);
                if (!found && index >= 0)
                    found = foundIndex = TryGetSeat(index, out seat);

                if (!string.IsNullOrEmpty(log))
                    CGLogger.Log($"ClientManager {message.GetType()}: {log} found {found}, by user {foundUser}, by index {foundIndex}");

                if (!found || seat == null)
                {
                    CGLogger.LogError($"ERROR: ClientManager seat not found! message: {message}");
                    CloudSyncSdk.NotifyFatalError("游戏错误, 请重新打开");
                    return;
                }

                if (message.index != seat.Index)
                {
                    CGLogger.LogError($"ERROR: Client #{seat.Index} index conflicts! msg.Index: {message.index} {message}");
                    CloudSyncSdk.NotifyFatalError("游戏错误, 请重新打开");
                    return;
                }

                var client = seat.Client;
                if (client == null)
                    return;

                switch (message)
                {
                    case PlayerOperateMessage operateMessage:
                        VirtualDeviceSystem.CurrentOperateFrame = operateMessage.writeFrame; // debug 用
                        client.Operate(operateMessage.operateData);
                        break;
                    case PlayerDisconnectedMessage disconnectedMessage:
                        client.DisconnectSuccess(disconnectedMessage);
                        seat.Leave(client);
                        break;
                    case PlayerCustomMessage customMessage:
                        var data = new CustomMessageData()
                        {
                            index = customMessage.index,
                            message = customMessage.message
                        };
                        client.OnCustomMessage(data);
                        break;
                }
            }
        }

        private bool TryGetSeat(SeatIndex index, out CloudSeat result)
        {
            result = GetSeat(index);
            return result != null;
        }

        private bool TryGetSeatBy(Predicate<CloudSeat> match, out CloudSeat result)
        {
            foreach (var seat in _seats)
            {
                if (seat == null || !match.Invoke(seat))
                    continue;
                result = seat;
                return true;
            }
            result = null;
            return false;
        }

        public void GetClients(List<ICloudClient> clients)
        {
            foreach (var seat in _seats)
            {
                if (seat.Client != null)
                    clients.Add(seat.Client);
            }
        }

        internal List<CloudClient> GetClientsInternal()
        {
            var clients = new List<CloudClient>();
            foreach (var seat in _seats)
            {
                if (seat.Client != null)
                    clients.Add(seat.Client);
            }
            return clients;
        }

        public ICloudClient GetClient(SeatIndex index) => GetClientInternal(index);

        private CloudClient GetClientInternal(SeatIndex index)
        {
            var seat = GetSeat(index);
            return seat?.Client;
        }

        /// <summary>
        /// 创建用户座位
        /// </summary>
        private CloudSeat CreateClient(SeatIndex index)
        {
            var seat = GetSeat(index);
            if (seat.Client != null)
            {
                CGLogger.LogError($"ERROR: Client {index} is already exist! override it! user info = {seat.PlayerInfo?.ToJson()}");
            }

            CGLogger.Log($"CreateClient, index: {index}");

            var client = new CloudClient();
            try
            {
                client.Initialize(seat, _deviceFactory);
                Debug.Assert(client.RtcUserInfo.Index == index);
                seat.Bind(client);
                return seat;
            }
            catch (DeviceCreateErrorException e)
            {
                CGLogger.LogError($"Create CloudClient {index} error: {e.Message}");
            }

            return null;
        }

        public async Task<ICloudClient> WaitConnected(SeatIndex index, CancellationToken cancellationToken)
        {
            CGLogger.Log($"Wait connecting: {index}");
            var seat = GetSeat(index);
            await seat.WaitJoin(cancellationToken);
            return seat.Client;
        }

        public bool IsConnected(SeatIndex index)
        {
            var client = GetClientInternal(index);
            return client is { IsConnected: true };
        }

        public void Dispose()
        {
            foreach (var seat in _seats)
            {
                seat.Dispose();
            }
        }

        ICloudSeat ICloudSeatManager.HostSeat => GetSeat(SeatIndex.Index0);

        ICloudSeat ICloudSeatManager.GetSeat(SeatIndex index) => GetSeat(index);

        public CloudSeat GetSeat(SeatIndex index)
        {
            var i = (int)index;
            if (index < 0 || i >= _seats.Length)
            {
                CGLogger.LogError($"ERROR: GetSeat: Index = {index} is out of range.");
                return null;
            }
            return _seats[i];
        }

        IEnumerable<ICloudSeat> ICloudSeatManager.AllSeats => _seats;

        public IEnumerable<CloudSeat> AllSeats => _seats;

        public void GetAllSeats(List<ICloudSeat> clients)
        {
            clients.AddRange(_seats);
        }

        bool ICloudSeatManager.IsJoined(SeatIndex index) => IsConnected(index);
    }
}
