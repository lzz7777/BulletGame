using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ByteDance.CloudSync
{
    /// <summary>
    /// 启动界面
    /// </summary>
    public interface ISplashScreen
    {
        event Action CloseHandler;

        IVirtualDeviceFactory Factory { get; }

        void OnOpen();

        void OnClose();
    }

    /// <summary>
    /// 启动界面处理
    /// </summary>
    internal class SplashScreenWorker : IInitWorker, ISafeActionsUpdatable
    {
        private IVirtualDevice _virtualDevice;
        private readonly ISplashScreen _screen;
        private readonly TaskCompletionSource<bool> _tcs = new();
        private SplashInputProcessor _inputProcessor;

        public static SplashScreenWorker Create(ISplashScreen splash)
        {
            if (splash == null) return null;
            return new SplashScreenWorker(splash);
        }

        private SplashScreenWorker(ISplashScreen screen)
        {
            _screen = screen;
            _screen.CloseHandler += HandleClose;
        }

        private void HandleClose()
        {
            _tcs.SetResult(true);
        }

        public bool IsWorkFor(InitPhase phase)
        {
            return phase == InitPhase.AfterSdk;
        }

        public async Task WorkOnInit(InitPhase phase)
        {
            if (IsWorkFor(phase))
                await RunAfterSdk();
        }

        private async Task RunAfterSdk()
        {
            var updatable = this as ISafeActionsUpdatable;
            var system = CloudSyncSdk.InternalCurrent;
            Start(_screen.Factory);
            _screen.OnOpen();
            system.AddUpdatable(updatable);
            await _tcs.Task;
            _screen.OnClose();
            system.RemoveUpdatable(updatable);
            VirtualDeviceSystem.Destroy(_virtualDevice);
            _virtualDevice = null;
            _inputProcessor = null;
        }

        private void Start(IVirtualDeviceFactory splashDeviceFactory)
        {
            _virtualDevice = VirtualDeviceSystem.Instance.CreateDevice(0, splashDeviceFactory, new RenderSettings());
            _virtualDevice.Init();
            _virtualDevice.Screen.Enable = true;
            _virtualDevice.Input.Enable = true;
            var messageHandler = CloudSyncSdk.SdkManager.MessageHandler;
            _inputProcessor = new SplashInputProcessor(_virtualDevice, messageHandler);
        }

        void ISafeActionsUpdatable.Update()
        {
            _inputProcessor?.ReadInputMessages();
        }

        class SplashInputProcessor
        {
            private IVirtualDevice _device;
            private ICloudGameMessageReader _messageReader;
            private readonly Queue<CloudGameMessageBase> _tempMessageQueue = new();

            internal SplashInputProcessor(IVirtualDevice device, ICloudGameMessageReader messageReader)
            {
                _device = device;
                _messageReader = messageReader;
            }

            internal void ReadInputMessages()
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
                switch (message)
                {
                    case PlayerOperateMessage operateMessage:
                        Operate(operateMessage.operateData);
                        break;
                }
            }

            private bool IsInputEnable => _device != null && _device.Input.Enable;

            private void Operate(PlayerOperate operate)
            {
                if (!IsInputEnable)
                    return;
                _device.Input.ProcessInput(operate);
            }
        }
    }
}