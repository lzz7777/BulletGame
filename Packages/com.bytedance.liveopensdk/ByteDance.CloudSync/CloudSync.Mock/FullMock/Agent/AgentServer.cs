using System;
using System.Collections.Generic;
using System.Linq;
using ByteDance.CloudSync.Mock.Agent;
using ByteDance.CloudSync.Mock;
using UnityEngine;
using UnityEngine.Serialization;
using WebSocketSharp.Server;
using Random = UnityEngine.Random;

namespace ByteDance.CloudSync.Mock.Agent
{
    [Serializable]
    class RtcServerConfig
    {
        public bool IsHost { get; set; }

        public int port;
        public bool started;
    }

    /// <summary>
    /// Mock直播云游戏的Agent服务器
    /// </summary>
    /// <remarks>
    /// Rtc链路关系，参考: <see cref="FullMock"/>
    /// </remarks>
    internal class AgentServer : IDisposable
    {
        private static readonly AgentServer Instance = new();
        private static readonly LockFile LockFile = new(".rtc_server_lock");
        private static IMockLogger _logger = IMockLogger.GetLogger(nameof(AgentServer));

        private static RtcServerConfig _config;
        private static RtcServerConfig _detectedConfig;

        internal static RtcServerConfig GetCurrentConfig() => _config;
        internal static RtcServerConfig GetDetectedConfig() => _detectedConfig;

        public static void StartServer()
        {
            Instance.Start();
        }

        public static void StopServer()
        {
            Instance.Stop();
        }

        [RuntimeInitializeOnLoadMethod]
        private static void Init()
        {
            if (Application.isEditor)
            {
                Application.quitting += () =>
                {
                    Instance.Stop();
                    LockFile.Unlock();
                };
            }
        }

        private WebSocketServer _server;
        private readonly Dictionary<string, PodRtcRoomService> _rooms = new();
        private bool _started;

        internal readonly MatchServer MatchServer;

        private AgentServer()
        {
            MatchServer = new MatchServer(this);
        }

        private void Start()
        {
            if (!Application.isPlaying)
                throw new NotSupportedException("Application is not playing");

            if (_started)
                return;

            Config();

            if (!_config.IsHost)
            {
                _logger.LogError("Failed to start as server. Please connect to existing server");
                return;
            }

            _logger.Log($"starting server, port: {Port}");
            _server = new WebSocketServer(Port);
            _server.AddWebSocketService("/client", () => new ClientRtcService(this)
            {
                Delayer = GetMessageDelayer()
            });
            _server.AddWebSocketService("/pod_room", () => new PodRtcRoomService(this)
            {
                Delayer = GetMessageDelayer()
            });
            _server.Start();
            _started = true;
            _config.started = true;
            UpdateLockContent();

            _logger.Log($"started server, port: {Port}");
        }

        private void Stop()
        {
            if (!_started)
                return;

            _started = false;
            _server.Stop();
            _rooms.Clear();
            _logger.Log("server stopped");
        }

        internal PodRtcRoomService GetRoomById(string roomId)
        {
            return _rooms.GetValueOrDefault(roomId);
        }

        /// <summary>
        /// 根据房主的 Token 查找房间
        /// </summary>
        /// <param name="hostToken">即房主的 Env.CloudGameToken</param>
        /// <returns></returns>
        internal PodRtcRoomService GetRoomByHostToken(string hostToken)
        {
            return _rooms.Values.FirstOrDefault(it => it.PodToken == hostToken);
        }

        /// <summary>
        /// 注册Pod房间
        /// </summary>
        /// <param name="id"></param>
        /// <param name="service"></param>
        internal void RegisterPodRoom(string id, PodRtcRoomService service)
        {
            _logger.Log($"Register Pod Room, roomId: {id}, pod token: {service.PodToken}");
            _rooms.Add(id, service);
        }

        public void Dispose()
        {
            _server?.Stop();
        }

        /// <summary>
        /// 全局记录一下当前用户设置的 Host 地址
        /// </summary>
        public static string Host;

        /// <summary>
        /// 当前 Agent 端口
        /// </summary>
        public static int Port
        {
            get
            {
                Config();
                return _config.port;
            }
            set
            {
                Config();
                _config.port = value;
                UpdateLockContent();
            }
        }

        public static bool IsHost => _config.IsHost;

        internal const int DefaultDelayMS = 100;
        internal static int NetDelayMs { get; set; } = DefaultDelayMS;

        private static Func<int, bool> GetMsgDelayChecker() => id => id > MessageId.Candidate;

        public static MessageDelayer GetMessageDelayer()
        {
            return new MessageDelayer
            {
                DelayMs = NetDelayMs,
                MsgDelayChecker = GetMsgDelayChecker()
            };
        }

        /// <summary>
        /// 尝试设置端口，第一个运行的 APP 获得主导权，能开 AgentServer。其它不能。
        /// </summary>
        internal static RtcServerConfig Config()
        {
            DetectExistingConfig();
            if (_config != null)
            {
                UpdateLockContent();
                return _config;
            }

            const string key = "CloudSync.Mock.RTC_SERVER_PORT";
            if (LockFile.TryLock())
            {
                _config = new RtcServerConfig();
                _config.IsHost = true;
                _config.port = Random.Range(8000, 9000);
                LockFile.WriteJson(_config);
                PlayerPrefs.SetInt(key, _config.port);
                _detectedConfig = _config;
                _logger.Log($"lock success, set port = {_config.port}");
            }
            else
            {
                _config = new RtcServerConfig();
                _config.port = PlayerPrefs.GetInt(key);
                _config.IsHost = false;
                _logger.Log($"lock failed, read port = {_config.port}");
            }

            return _config;
        }

        private static void UpdateLockContent()
        {
            if (!_config.IsHost || _detectedConfig == null)
                return;
            if (!LockFile.HasLockSuccess())
                return;
            LockFile.WriteJson(_config);
        }

        internal static RtcServerConfig DetectExistingConfig()
        {
            try
            {
                _detectedConfig = LockFile.ReadJson<RtcServerConfig>();
            }
            catch (Exception e)
            {
                _logger.Log($"try read lock got exception: {e}");
            }

            return _detectedConfig;
        }

        public List<IRtcRoomService> GetRooms()
        {
            return _rooms.Values.Cast<IRtcRoomService>().ToList();
        }
    }
}