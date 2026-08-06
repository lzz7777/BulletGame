using System;
using System.Collections.Generic;

namespace ByteDance.CloudSync
{
    /// <summary>
    /// 云游戏事件转发器
    /// </summary>
    public class CouldGameEventEmitter
    {
        internal static CouldGameEventEmitter Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new CouldGameEventEmitter();
                return _instance;
            }
        }


        private static CouldGameEventEmitter _instance;
        private static long _globalRecordId;
        private static long GenerateRecordId() => ++_globalRecordId;
        private bool _isVerboseLog = true;

        // ReSharper disable once MemberCanBePrivate.Global
        public interface IEventRecord : IDisposable
        {
            string Name { get; }
            long RecordId { get; }
            IEventContainer Container { get; }
        }

        // ReSharper disable once RedundantExtendsListEntry
        public class EventRecord : IEventRecord, IDisposable
        {
            public bool IsVerboseLog;
            public string Name { get; set; }
            public long RecordId { get; set; }
            public IEventContainer Container { get; set; }
            public void Dispose()
            {
                var remove = Container.Remove(RecordId);
                if (IsVerboseLog)
                    CGLogger.Log($"CloudGameEvent Dispose id: {RecordId}, name: \"{Name}\", remove: {remove}");
            }
        }

        public class EventRecord<T> : EventRecord
        {
            public Action<T> Action;
        }

        public interface IEventContainer
        {
            string Name { get; }

            bool Remove(long recordId);
            bool TryGet(long recordId, out IEventRecord record);
            bool TryRemove(long recordId, out IEventRecord record);
            void Clear();
        }

        private class EventContainer<T> : IEventContainer
        {
            private readonly Dictionary<long, EventRecord<T>> _events = new();
            private static long GenRecordId() => GenerateRecordId();
            public string Name { get; }

            public EventContainer(string name)
            {
                Name = name;
            }

            public EventRecord<T> Add(Action<T> action)
            {
                var recordId = GenRecordId();
                var record = new EventRecord<T>
                {
                    Name = Name,
                    Container = this,
                    RecordId = recordId,
                    Action = action,
                };
                _events[recordId] = record;
                return record;
            }

            public bool Remove(long recordId)
            {
                return _events.Remove(recordId);
            }

            public bool TryGet(long recordId, out IEventRecord record)
            {
                var ret = _events.TryGetValue(recordId, out var recordT);
                record = recordT;
                return ret;
            }

            public bool TryRemove(long recordId, out IEventRecord record)
            {
                _events.TryGetValue(recordId, out var recordT);
                record = recordT;
                return _events.Remove(recordId);
            }

            public void Emit(T data)
            {
                foreach (var pair in _events)
                {
                    try
                    {
                        var record = pair.Value;
                        record.Action?.Invoke(data);
                    }
                    catch (Exception e)
                    {
                        CGLogger.LogError($"CloudGameEvent Emit, action invoke got exception: {e}");
                    }
                }
            }

            public void Clear()
            {
                _events.Clear();
            }
        }

        private readonly EventContainer<PlayerEnterData> _playerEnterEvent = new("PlayerEnter");
        private readonly EventContainer<PlayerLeaveData> _playerLeaveEvent = new("PlayerLeave");
        private readonly EventContainer<CustomMessageData> _customMessageEvent = new("CustomMessage");

        public EventRecord<PlayerEnterData> RegisterPlayerEnter(Action<PlayerEnterData> enterAction)
        {
            var record = _playerEnterEvent.Add(enterAction);
            record.IsVerboseLog = _isVerboseLog;
            OnRegAdd(record);
            return record;
        }

        public bool UnregisterPlayerEnter(long recordId)
        {
            var remove = _playerEnterEvent.TryRemove(recordId, out var record);
            OnRegRemove(record, remove);
            return remove;
        }

        public void EmitPlayerEnter(PlayerEnterData data)
        {
            CGLogger.Log($"Emit PlayerEnterData: {data}");
            _playerEnterEvent.Emit(data);
        }

        public EventRecord<PlayerLeaveData> RegisterPlayerLeave(Action<PlayerLeaveData> leaveAction)
        {
            var record = _playerLeaveEvent.Add(leaveAction);
            record.IsVerboseLog = _isVerboseLog;
            OnRegAdd(record);
            return record;
        }

        public bool UnregisterPlayerLeave(long recordId)
        {
            var remove = _playerLeaveEvent.TryRemove(recordId, out var record);
            OnRegRemove(record, remove);
            return remove;
        }

        public void EmitPlayerLeave(PlayerLeaveData data)
        {
            CGLogger.Log($"Emit PlayerLeaveData: {data}");
            _playerLeaveEvent.Emit(data);
        }

        public EventRecord<CustomMessageData> RegisterCustomMessage(Action<CustomMessageData> action)
        {
            var record = _customMessageEvent.Add(action);
            record.IsVerboseLog = _isVerboseLog;
            OnRegAdd(record);
            return record;
        }

        public bool UnregisterCustomMessage(long recordId)
        {
            var remove = _customMessageEvent.TryRemove(recordId, out var record);
            OnRegRemove(record, remove);
            return remove;
        }

        public void EmitCustomMessage(CustomMessageData data)
        {
            _customMessageEvent.Emit(data);
        }

        public void ClearAll()
        {
            _customMessageEvent.Clear();
            _playerLeaveEvent.Clear();
            _playerEnterEvent.Clear();
        }

        private void OnRegAdd(IEventRecord record)
        {
            if (_isVerboseLog)
                CGLogger.Log($"CloudGameEvent Register id: {record.RecordId}, name: \"{record.Name}\"");
        }

        private void OnRegRemove(IEventRecord record, bool removed)
        {
            if (_isVerboseLog)
                CGLogger.Log($"CloudGameEvent Unregister id: {record.RecordId}, name: \"{record.Name}\", remove: {removed}");
        }
    }
}