using System;
using ByteDance.Live.Foundation.Logging;
using ByteDance.LiveOpenSdk.DebugUtils;
using ByteDance.LiveOpenSdk.Room;
using ByteDance.LiveOpenSdk.Runtime.Utilities;
using ByteDance.LiveOpenSdk.Utilities;
using UnityEngine;

namespace ByteDance.LiveOpenSdk.Runtime.Modules
{
    internal interface IDebugUtilsController
    {
        void StartLoop();
        void OnRoomInfoUpdate(IRoomInfo obj);
    }
    internal class DebugUtilsControllerImpl : MonoBehaviour, IDebugUtilsController
    {
        private static IDebugUtilsController _instance;

        public static IDebugUtilsController Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject($"[{nameof(IDebugUtilsController)}]");
                    _instance = go.AddComponent<DebugUtilsControllerImpl>();
                }

                return _instance;
            }
        }
        private bool _start;
        private bool _roomUpdate;
        private float _deltaTime;
        private const float REQUEST_TIMES_PER_SECONDS = 5.0f;
        private readonly float _requestRateSeconds = 1 / REQUEST_TIMES_PER_SECONDS;
        private LogWriter Log { get; } = new LogWriter(SdkUnityLogger.LogSink, "DebugUtilsControllerImpl");
        private DebugUtilsServiceImpl _debugUtilsService;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void OnRoomInfoUpdate(IRoomInfo obj)
        {
            _debugUtilsService = LiveOpenSdk.Instance?.GetDebugUtilsService() as DebugUtilsServiceImpl;
            _debugUtilsService!.GameVersion = Application.version;
            _roomUpdate = true;
        }

        private async void UploadLogAsync()
        {
            try
            {
                await _debugUtilsService?.UploadLogAsync()!;
            }
            catch (Exception e)
            {
                Log.Warning($"UploadLogAsync failed: {e}, will retry.");
            }
        }
        public void StartLoop()
        {
            if (_start)
                return;

            _start = true;
        }

        public void StopLoop()
        {
            if (!_start)
                return;

            _start = false;
        }

        public void Update()
        {
            _deltaTime += Time.deltaTime;
            if (_deltaTime >= _requestRateSeconds)
            {
                _deltaTime = 0;
                if (_start && _roomUpdate)
                {
                    UploadLogAsync();
                }
            }
        }
    }
}