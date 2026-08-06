using System;
using ByteDance.CloudSync.TeaSDK;

// ReSharper disable once CheckNamespace
namespace ByteDance.CloudSync
{
    internal partial class CloudGameSdkManager
    {
        private readonly ICloudUserInfoDB _userInfoDB = new CloudUserInfoDB(); // 注意线程安全

        // MARK: - ## 1. Joining ##
        #region Joining

        /// <summary>
        /// Sdk回调：RTC 进房事件, 保序.
        /// </summary>
        /// <param name="roomIndex"></param>
        /// <param name="param">加房参数，在 xplay init 接口的 extra.init_param 字段透传</param>
        /// <remarks></remarks>
        void IMultiplayerListener.OnPlayerJoin(int roomIndex, JoinRoomParam param)
        {
            TeaReport.Report_sdk_on_player_join((int)param.Code,param.LinkRoomId, roomIndex, param.RTCUserId);
            CGLogger.Log($"{LogTag}OnPlayerJoin gamesdk-rtc进房 room: {roomIndex}, rtcUserId: {param.RTCUserId}");
            // note: 注意！！ OnPlayerJoin 时 只信任 `int roomIndex` 和 `param.RTCUserId`，总是去做 Query
            if (!ValidateJoinEvent(roomIndex, param))
                return;
            if (!ProcessJoinNewConflict(roomIndex, param))
                return;

            var index = (SeatIndex)roomIndex;
            var newInfo = _userInfoDB.SetJoined(index, param);
            var msg = new PlayerConnectingMessage
            {
                UserInfo = newInfo,
                index = index
            };
            EnqueueMessage(roomIndex, msg);
        }

        /// 校验Join事件的进房信息
        private bool ValidateJoinEvent(int roomIndex, JoinRoomParam param)
        {
            var index = (SeatIndex)roomIndex;
            if (index.IsValid() && param != null)
                return true;

            // 边界case：进房信息、Index -1异常
            var code = param != null ? (int)param.Code : 0;
            var message = param?.Message ?? "";
            var rtcUserId = param?.RTCUserId ?? "";
            var errorLog = $"{LogTag}OnPlayerJoin room: {roomIndex}, index异常：rtcUserId: {rtcUserId}, code: {code}, {message}";
            CGLogger.LogError(errorLog);
            var text = UIInfoProvider.DlgText_RoomIndexError.Replace("{index}", $"{roomIndex}");
            CloudSyncSdk.NotifyFatalError(text, code);
            return false;
        }

        #endregion

        // MARK: - ## 2. Query ##
        #region Query

        // 进房处理#3. 回调 OnQueryRoomInfo
        [Obsolete]
        void IMultiplayerListener.OnQueryRoomInfo(int roomIndex, JoinRoomParam param)
        {
            throw new NotSupportedException("Never reached here");
        }

        #endregion

        // MARK: - Data Conflict
        #region Data Conflict

        /// <summary>
        /// 处理case: Join新用户信息顶替
        /// </summary>
        /// <returns>bool是否通过，并允许后续继续处理</returns>
        private bool ProcessJoinNewConflict(int roomIndex, JoinRoomParam param)
        {
            var index = (SeatIndex)roomIndex;
            _userInfoDB.GetSeatInfo(index, out var seatState, out var seatUser);
            if (seatState != CloudUserState.None && seatUser.IsValidInfo)
            {
                // 边界case：Join新用户信息顶替 (state #i 存在用户) 信任新进房事件，保序
                var newId = param.RTCUserId;
                var oldId = seatUser.RtcUserId;
                CGLogger.LogWarning($"{LogTag}room: {roomIndex}, 边界case：Join新用户信息顶替 Join id: {newId}, old info: {seatState} id: {oldId}");

                // 新用户信息顶替：设置 #i state: none
                _userInfoDB.SetExit(index, oldId);

                // 存在已有数据，需要抛出离开，由CloudClient继续处理
                var msg = new PlayerDisconnectedMessage
                {
                    index = index,
                    UserInfo = seatUser
                };
                EnqueueMessage(roomIndex, msg);
            }

            return true;
        }

        /// <summary>
        /// 处理case: 用户信息是旧的（已被顶替）
        /// </summary>
        /// <returns>bool是否通过，允许后续继续处理</returns>
        private bool ProcessOldConflict(int roomIndex, string eventUserId, string eventType)
        {
            var index = (SeatIndex)roomIndex;
            _userInfoDB.GetSeatInfo(index, out var seatState, out var seatUser);
            if (seatState != CloudUserState.None && seatUser.IsValidInfo)
            {
                var currentId = seatUser.RtcUserId;
                if (currentId != eventUserId)
                {
                    // 边界case: 用户信息是旧的（已被顶替） (state #i 存在用户 且 id不同)
                    var warnLog = $"{LogTag}room: {roomIndex}, 边界case：用户信息是旧的（已被顶替） {eventType} id: {eventUserId}, saved info: {seatState} id: {currentId}";
                    CGLogger.LogWarning(warnLog);
                    return false;
                }
            }

            return true;
        }

        #endregion

        // MARK: - ## 4. Exit ##
        #region Exit

        /// <summary>
        /// RTC 退房事件, 保序.
        /// </summary>
        /// <param name="roomIndex"></param>
        /// <param name="param">退房参数</param>
        void IMultiplayerListener.OnPlayerExit(int roomIndex, ExitRoomParam param)
        {
            var index = (SeatIndex)roomIndex;
            // note: 边界case： 如果主播1 把 嘉宾A（在`roomIndex` 2），切换成 嘉宾B（他加入时`roomIndex`也会是 2），端上无法100%保序为 OnPlayerExit A，再 OnPlayerJoin B.
            // 信任数据：`int roomIndex` `param.RTCUserId`
            var rtcUserId = param.RTCUserId;
            if (!ValidateExitEvent(roomIndex, param))
                return;
            if (!ProcessOldConflict(roomIndex, rtcUserId, "Exit"))
                return;

            var oldState = _userInfoDB.GetState(rtcUserId);
            var userInfo = _userInfoDB.SetExit(index, rtcUserId);
            var reason = param.Reason;
            CGLogger.Log($"{LogTag}OnPlayerExit gamesdk-rtc退房 room: {roomIndex}, reason: {reason}, rtcUserId: {rtcUserId}, oldState : {oldState}, {userInfo}");
            var msg = new PlayerDisconnectedMessage
            {
                index = index,
                UserInfo = userInfo
            };
            EnqueueMessage(roomIndex, msg);
        }

        /// 校验Exit事件的退房信息
        private bool ValidateExitEvent(int roomIndex, ExitRoomParam param)
        {
            var index = (SeatIndex)roomIndex;
            // 信任数据：`int roomIndex` `param.RTCUserId`
            var rtcUserId = param.RTCUserId;
            var state = _userInfoDB.GetState(rtcUserId);
            var userInfo = new CloudUserInfo(index, rtcUserId);

            // 边界case: 异常进退房信息，忽略
            if (!userInfo.IsValidInfo)
            {
                CGLogger.LogError($"{LogTag}OnPlayerExit room: {roomIndex}, 边界case 用户信息异常: {userInfo}");
                return false;
            }

            // 边界case: 主播退了，游戏结束，兜底弹窗
            if (userInfo.IsAnchor)
            {
                // OnPlayerExit_AnchorExit(userInfo);
                // 不强行阻断，继续走完离开事件
            }

            if (state == CloudUserState.None)
            {
                // 边界case: 退房用户不在，错误，不抛出离开事件
                CGLogger.LogError($"{LogTag}OnPlayerExit room: {roomIndex}, 边界case：退房用户不在！ rtcUserId: {rtcUserId}, oldState: {state}");
                return false;
            }

            if (state == CloudUserState.Querying)
            {
                // 边界case: 退房用户查询中、还未返回OnQuery
                CGLogger.Log($"{LogTag}OnPlayerExit room: {roomIndex}, 边界case：退房用户仍查询中 rtcUserId: {rtcUserId}, oldState: {state}");
                // 需要继续处理CloudClient内部逻辑和状态，继续执行
            }

            return true;
        }

        #endregion
    }
}