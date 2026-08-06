// Copyright (c) Bytedance. All rights reserved.
// Description: CloudSeat 中关于 IMessagePushService 部分

using System;
using System.Threading.Tasks;
using ByteDance.CloudSync.External;
using ByteDance.LiveOpenSdk;
using ByteDance.LiveOpenSdk.CloudSync;
using ByteDance.LiveOpenSdk.Push;

namespace ByteDance.CloudSync
{
    internal partial class CloudSeat
    {
        private IGuestLiveOpenSdk _guestLiveOpenSdk;
        private WrappedMessagePushService _pushService;
        private WrappedMessagePushService _pushServiceTasksStarted;

        /// <inheritdoc cref="ICloudSeat.StartPushService"/>
        public async Task<IMessagePushService> StartPushService()
        {
            if (State != SeatState.InUse)
                throw new InvalidOperationException($"Wrong state ({State})! Please call `StartPushService` after the `OnSeatPlayerJoined` event");

            if (_pushServiceTasksStarted != null)
                return _pushServiceTasksStarted;

            try
            {
                InitPushService();
            }
            catch (Exception e)
            {
                Debug.LogError($"InitPushService error: {e.Message}");
                throw;
            }

            // note: 错误处理：目前如果其中某一个或几个任务失败了，没有自动重试。 开发者可以 try catch 后重新调用`StartPushService`。
            // note: 注：`StartPushTaskAsync` 按接口设计、对外文档：成功启动过的任务，再次调用启动，无副作用。
            // note: 注：`StartPushTaskAsync` 在任务失败时，会抛异常 Exception，不会走到await下一行
            await Task.WhenAll(
                _pushService.StartPushTaskAsync(PushMessageTypes.LiveGift),
                _pushService.StartPushTaskAsync(PushMessageTypes.LiveComment),
                _pushService.StartPushTaskAsync(PushMessageTypes.LiveLike)
            );

            // note: Here we use current `_pushService`, which may be possibly disposed or set null after awaits.
            _pushServiceTasksStarted = _pushService;
            return _pushServiceTasksStarted;
        }

        /// <summary>
        /// 初始化直播间指令直推服务
        /// </summary>
        /// <exception cref="System.Exception">初始化直推失败</exception>
        private WrappedMessagePushService InitPushService()
        {
            // 只创建一次
            if (_pushService != null)
                return _pushService;
            var isRealEnv = ICloudSync.Env.IsRealEnv(); // 需要真实环境的直播间
            Debug.Log($"InitPushService isRealEnv: {isRealEnv}, index: {Index}, isHost: {this.IsHost()}");
            var sdkProvide = isRealEnv ? CloudSyncExternals.LiveOpenSdkProvide : null;
            if (sdkProvide == null)
            {
                Debug.Log("InitPushService done with dummy service.");
                return _pushService = new WrappedMessagePushService();
            }

            var hostLiveSdk = sdkProvide();

            // 此座位对应的 LiveOpenSdk
            ILiveOpenSdk myLiveOpenSdk;
            if (this.IsHost())
            {
                myLiveOpenSdk = hostLiveSdk;
            }
            else
            {
                // note: 错误处理：目前如果玩家进房时获取用户信息失败，那么这里PushService也失败，且没有重试。
                //       错误处理：考虑a：如果异常进房的玩家，会踢掉，那么此处无需处理。 考虑b：如果有重新获取其用户信息的机制，那么获取成功后，应自动重新处理PushService。
                if (PlayerInfo == null)
                    throw new Exception("PlayerInfo is null 获取用户信息失败");

                var guestAnchor = (AnchorPlayerInfo)PlayerInfo;
                if (guestAnchor.liveRoomToken == null)
                    throw new NullReferenceException("anchorInfo.liveRoomToken is null");

                Debug.Log($"Guest anchor: {guestAnchor.ToStr()}");
                var api = hostLiveSdk.GetService<ICloudSyncApi>();
                myLiveOpenSdk = _guestLiveOpenSdk = api.Create(ICloudSync.Env.AppId, guestAnchor.liveRoomToken);
            }

            myLiveOpenSdk.Initialize(ICloudSync.Env.AppId);
            Debug.Log("InitPushService done");
            return _pushService = new WrappedMessagePushService(myLiveOpenSdk);
        }

        private void CleanPushService()
        {
            _guestLiveOpenSdk?.Dispose();
            _guestLiveOpenSdk = null;
            _pushService?.Dispose();
            _pushService = null;
            _pushServiceTasksStarted = null;
        }
    }
}