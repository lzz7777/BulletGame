using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace ByteDance.CloudSync.TeaSDK
{
    public interface ITeaDataProvider
    {
        string TestDeviceId { get; }

        Dictionary<string, object> CustomValues { get; }
    }

    public class TestTeamDataProvider : ITeaDataProvider
    {
        public TestTeamDataProvider(string testDeviceId)
        {
            TestDeviceId = testDeviceId;
        }

        public string TestDeviceId { get; }
        public Dictionary<string, object> CustomValues { get; } = new();
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class TeaSdk
    {
        private const string CN = "https://mcs.snssdk.com/v1/list";
        private const string CN_TEST = "https://data.bytedance.net/et_api/logview/web_verify/?from=node/v1/list";
        private Dictionary<string, object> _custom;

        private Header _header;
        private User _user;

        public void Init(int appId, string uuid, string appChannel, string appVersion)
        {
            _header = new Header
            {
                app_channel = appChannel,
                app_version = GetAppVersion(appVersion),
                os_name = GetOSName(),
                os_version = Environment.OSVersion.VersionString,
                app_id = appId,
                device_id = uuid
            };

            _user = new User
            {
                user_unique_id = uuid
            };
        }

        private string GetAppVersion(string appVersion)
        {
            return appVersion; // 1.0.0正确，1.0会为unknown
        }

        private string GetOSName()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                    return "Windows";
                case RuntimePlatform.OSXEditor:
                    return "Mac";
                case RuntimePlatform.Android:
                    return "Android";
                case RuntimePlatform.IPhonePlayer:
                    return "iOS";
            }

            return "unknown";
        }

        public async void Collect(string eventName, string eventParams, ITeaDataProvider provider)
        {
            var ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            var custom = new Dictionary<string, object>();
            if (provider?.CustomValues != null)
                foreach (var pair in provider.CustomValues)
                    custom.Add(pair.Key, pair.Value);

            if (_custom != null)
                foreach (var pair in _custom)
                    custom.Add(pair.Key, pair.Value);

            _header.custom = custom;
            var user = _user;
            user.user_unique_id = _user.user_unique_id;
            var data = new CollectData
            {
                events = new[]
                {
                    new TeaEvent
                    {
                        @event = eventName,
                        local_time_ss = ts.TotalMilliseconds,
                        @params = eventParams
                    }
                },
                header = _header,
                user = user
            };
            var json = JsonConvert.SerializeObject(data);
            Debug.Log("json = " + json);
            var arrJson = $"[{json}]";
            var url = provider?.TestDeviceId != null ? CN_TEST : CN;
            using var request = new UnityWebRequest(url, "POST");
            request.SetRequestHeader("Content-Type", "application/json");
            using var uploadHandlerRaw = new UploadHandlerRaw(Encoding.UTF8.GetBytes(arrJson));
            request.uploadHandler = uploadHandlerRaw;
            using var downloadHandlerBuffer = new DownloadHandlerBuffer();
            request.downloadHandler = downloadHandlerBuffer;
            await request.SendWebRequest();
        }

        public void AddCustom(string name, object value)
        {
            if (_custom == null)
            {
                _custom = new Dictionary<string, object>();
                _header.custom = _custom;
            }

            _custom?.Add(name, value);
        }

        [Serializable]
        [SuppressMessage("ReSharper", "NotAccessedField.Local")]
        private struct TeaEvent
        {
            public string @event;
            public double local_time_ss;
            public string @params;
        }

        [Serializable]
        [SuppressMessage("ReSharper", "NotAccessedField.Local")]
        private class Header
        {
            public string device_id;
            public string app_version;
            public string app_channel;
            public string os_version;
            public string os_name;
            public int app_id;
            public Dictionary<string, object> custom;
        }

        [Serializable]
        [SuppressMessage("ReSharper", "NotAccessedField.Local")]
        private class User
        {
            public string user_unique_id;
        }

        [Serializable]
        [SuppressMessage("ReSharper", "NotAccessedField.Local")]
        private class CollectData
        {
            public TeaEvent[] events;
            public Header header;
            public User user;
        }
    }
}