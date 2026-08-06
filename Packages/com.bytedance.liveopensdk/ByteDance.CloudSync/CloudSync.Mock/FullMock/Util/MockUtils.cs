using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.WebRTC;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ByteDance.CloudSync.Mock
{
    public static class MockUtils
    {
        public static RTCConfiguration GetSelectedSdpSemantics()
        {
            RTCConfiguration config = default;
            config.iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } } };
            return config;
        }

        public static async Task Wait(this AsyncOperationBase operationBase, CancellationToken token)
        {
            while (operationBase.MoveNext())
            {
                token.ThrowIfCancellationRequested();
                await Task.Yield();
            }
            if (operationBase.IsError)
                Debug.LogError(operationBase.Error.message);
        }

        private static readonly HashSet<int> UsedIds = new();

        public static int RandomIntId()
        {
            var count = 0;
            while (true)
            {
                const int maxExclusive = 1000000;
                var id = Random.Range(maxExclusive / 10, maxExclusive);
                if (UsedIds.Add(id))
                    return id;
                if (count++ > 9999)
                    throw new Exception("RandomIntId failed");
            }
        }

        public static string MockRandomRoomId() => MockRoomId(RandomIntId());

        public static string MockRoomId(int intRoomId) => $"{intRoomId:00000}";

        public static string MockRandomUserId() => MockUserId(RandomIntId());

        public static string MockUserId(int intId) => $"U{intId:00000}";

        private static readonly string[] Avatars = new[]
        {
            "https://p11.douyinpic.com/aweme/720x720/c16000003f97583dac4.jpeg",
            "https://p11.douyinpic.com/aweme/720x720/aweme-avatar/mosaic-legacy_3797_2889309425.jpeg?from=",
            "https://p11.douyinpic.com/aweme/720x720/aweme-avatar/mosaic-legacy_3795_3044413937.jpeg?from=",
            "https://p11.douyinpic.com/aweme/720x720/aweme-avatar/mosaic-legacy_3795_3033762272.jpeg?from=",
        };

        internal static string RandomAvatarUrl()
        {
            return Avatars[Random.Range(0, Avatars.Length)];
        }

        public static AnchorPlayerInfo MockRandomPlayerInfo() => MockPlayerInfo(RandomIntId());

        public static AnchorPlayerInfo MockPlayerInfo(int intRoomId)
        {
            return new AnchorPlayerInfo
            {
                liveRoomId = $"{intRoomId}", // roomId 内容为纯数字
                liveRoomToken = "mock_live_room_token",
                openId = $"mock_open_id_{intRoomId:00000}",
                nickName = $"主播{intRoomId:00000}",
                avatarUrl = RandomAvatarUrl()
            };
        }
    }

    public class LockFile : IDisposable
    {
        private readonly string _path;
        private FileStream _lockerFileStream;

        public LockFile(string filename)
        {
            _path = Path.Combine(Application.persistentDataPath, filename);
        }

        public T ReadJson<T>()
        {
            var path = _path;
            if (!File.Exists(path))
                return default;
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs, Encoding.Default);
            var text = sr.ReadToEnd();
            return JsonUtility.FromJson<T>(text);
        }

        public void WriteJson<T>(T data)
        {
            var text = JsonUtility.ToJson(data);
            _lockerFileStream?.SetLength(0);
            _lockerFileStream?.Write(Encoding.UTF8.GetBytes(text));
            _lockerFileStream?.Flush();
        }

        public bool HasLockSuccess() => _lockerFileStream != null;

        public bool TryLock()
        {
            _lockerFileStream?.Dispose();
            _lockerFileStream = null;

            var lockSuccess = true;
            if (File.Exists(_path))
            {
                try
                {
                    File.Delete(_path);
                }
                catch (Exception)
                {
                    lockSuccess = false;
                }
            }

            if (lockSuccess)
            {
                _lockerFileStream = File.Open(_path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            }

            Debug.Log($"Lock result: {lockSuccess}, path = {_path}");
            return lockSuccess;
        }

        public void Unlock()
        {
            _lockerFileStream?.Flush();
            _lockerFileStream?.Dispose();
            _lockerFileStream = null;
        }

        public void Dispose()
        {
            Unlock();
        }
    }
}