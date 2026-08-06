// Copyright@www.bytedance.com
// Author: DONEY Dong
// Date: 2024/05/08
// Description:

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace ByteDance.CloudSync
{
    /// <summary>
    /// 用户状态
    /// </summary>
    public enum CloudUserState
    {
        /// 不在 （未加入，或已退房）
        None,

        /// <summary>
        /// 查询中 （RTC加入，但open信息在查）
        /// </summary>
        Querying,

        /// <summary>
        /// 已加入 （RTC加入且open信息成功）
        /// </summary>
        Joined,
    }

    /// <summary>
    /// 用户信息数据库（本地缓存 RtcUserId, openId 的映射）
    /// </summary>
    internal interface ICloudUserInfoDB
    {
        /// <summary>
        /// 已加入的主播数量
        /// </summary>
        int JoinedAnchorCount { get; }

        Task WaitAnchors(int requireCount, CancellationToken token);

        /// 设置查询中
        CloudUserInfo SetQuery(SeatIndex index, string rtcUserId);

        /// 设置已加入
        CloudUserInfo SetJoined(SeatIndex index, JoinRoomParam param);

        /// 设置退出（不在）
        CloudUserInfo SetExit(SeatIndex index, string rtcUserId);

        /// <summary>
        /// 读取状态。
        /// </summary>
        CloudUserState GetState(string rtcUserId);

        /// <summary>
        /// 读取席位数据
        /// </summary>
        void GetSeatInfo(SeatIndex index, out CloudUserState seatState, out CloudUserInfo seatUser);
    }

    internal class CloudUserInfoHolder
    {
        public void Init(CloudUserInfo info)
        {
            _info = info;
        }

        public CloudUserInfo Info => _info;
        private CloudUserInfo _info;

        public CloudUserState State { get; set; }
    }

    /// <inheritdoc cref="ICloudUserInfoDB"/>
    internal class CloudUserInfoDB : ICloudUserInfoDB
    {
        private readonly Dictionary<string, CloudUserInfoHolder> _userIdMap = new(32);
        private readonly Dictionary<SeatIndex, CloudUserInfoHolder> _seatsMap = new(8);
        private static readonly SdkDebugLogger Debug = CloudGameSdkManager.Debug;

        public int JoinedAnchorCount
        {
            get
            {
                var count = 0;

                // no LINQ, avoid GC
                foreach (var it in _seatsMap.Values)
                {
                    if (it.State == CloudUserState.Joined && it.Info is { IsAnchor: true })
                        count++;
                }

                return count;
            }
        }

        public async Task WaitAnchors(int requireCount, CancellationToken token)
        {
            while (JoinedAnchorCount < requireCount && !token.IsCancellationRequested)
            {
                await Task.Yield();
            }
        }

        public CloudUserInfo SetQuery(SeatIndex index, string rtcUserId)
        {
            var newInfo = new CloudUserInfo(index, rtcUserId);
            var holder = GetHolder(rtcUserId, false);
            if (holder != null && holder.Info.IsValidInfo)
                newInfo = holder.Info;
            SetUserInfo(newInfo, false, CloudUserState.Querying);
            SetSeatInfo(index, rtcUserId, CloudUserState.Querying, newInfo);
            return newInfo;
        }

        public CloudUserInfo SetJoined(SeatIndex index, JoinRoomParam param)
        {
            var newInfo = new CloudUserInfo(index, param);
            var rtcUserId = newInfo.RtcUserId;
            SetUserInfo(newInfo, true, CloudUserState.Joined);
            SetSeatInfo(index, rtcUserId, CloudUserState.Joined, newInfo);
            return newInfo;
        }

        public CloudUserInfo SetExit(SeatIndex index, string rtcUserId)
        {
            var userInfo = new CloudUserInfo(index, rtcUserId);
            SetSeatInfo(index, rtcUserId, CloudUserState.None, userInfo);

            if (_userIdMap.Remove(rtcUserId, out var holder))
            {
                holder.State = CloudUserState.None;
                return holder.Info;
            }

            Debug.LogError($"ERROR: SetExit rtcUserId not found! #{index}, remove id: {rtcUserId}");
            return EmptyUserInfo(index);
        }

        public void GetSeatInfo(SeatIndex index, out CloudUserState seatState, out CloudUserInfo seatUser)
        {
            var holder = GetSeatInfoHolder(index, false);
            seatState = holder?.State ?? CloudUserState.None;
            seatUser = holder?.Info ?? EmptyUserInfo(index);
        }

        private void SetUserInfo(CloudUserInfo userInfo, bool isFullInfo, CloudUserState state)
        {
            var userId = userInfo.RtcUserId;
            Debug.Log($"Set userInfo, state: {state}, rtcUserId: {userId} ");
            if (string.IsNullOrEmpty(userId))
                Debug.LogError("ERROR: Set userInfo, rtcUserId is null!");

            var holder = GetHolder(userId, true);
            holder.Init(userInfo);
            holder.State = state;
        }

        private static CloudUserInfo EmptyUserInfo(SeatIndex index)
        {
            return new CloudUserInfo(index, string.Empty);
        }

        /// <inheritdoc cref="ICloudUserInfoDB.GetState"/>
        public CloudUserState GetState(string rtcUserId)
        {
            var holder = GetHolder(rtcUserId, false);
            return holder?.State ?? CloudUserState.None;
        }

        /// 设置席位信息
        /// <returns>是否设置成功。 true 成功； false 失败、异常。</returns>
        private bool SetSeatInfo(SeatIndex index, string rtcUserId, CloudUserState state, CloudUserInfo newInfo)
        {
            if (!ValidateSetState(index, rtcUserId, state))
                return false;

            Debug.Log($"Set seatInfo, seat: {index}, state: {state}, rtcUserId: {rtcUserId}");
            switch (state)
            {
                case CloudUserState.None: // 退房
                    _seatsMap.Remove(index);
                    return true;
                case CloudUserState.Querying: // 查询中
                case CloudUserState.Joined: // 已加入
                    var holder = GetSeatInfoHolder(index, true);
                    holder.Init(newInfo);
                    holder.State = state;
                    return true;
                default:
                    return false;
            }
        }

        private bool ValidateSetState(SeatIndex index, string rtcUserId, CloudUserState state)
        {
            CloudUserInfoHolder seatUser;
            switch (state)
            {
                case CloudUserState.None: // 退房
                    if (IsSeatUserConflict(index, rtcUserId, out seatUser))
                    {
                        // 检查边界case：Exit冲突，不信任
                        Debug.LogError($"ERROR: set seat: {index} {state}, rtcUserId not match! remove id: {rtcUserId}, saved id: {seatUser.Info.RtcUserId}");
                        return false;
                    }
                    break;
                case CloudUserState.Querying: // 查询中
                    if (IsSeatUserConflict(index, rtcUserId, out seatUser))
                    {
                        // 检查边界case：Join冲突，信任新数据
                        Debug.LogError($"Warning: set seat: {index} {state}, rtcUserId not match! new id: {rtcUserId}, saved id: {seatUser.Info.RtcUserId}");
                    }
                    break;
                case CloudUserState.Joined: // 已加入
                    if (IsSeatUserConflict(index, rtcUserId, out seatUser))
                    {
                        // 检查边界case：onQuery冲突，不信任
                        Debug.LogError($"ERROR: set seat: {index} {state}, rtcUserId not match! remove id: {rtcUserId}, saved id: {seatUser.Info.RtcUserId}");
                        return false;
                    }
                    break;
            }

            return true;
        }

        /// <summary>
        /// 是否席位上信息冲突，即存在用户、状态非None、且其userId与传入的不一致
        /// </summary>
        private bool IsSeatUserConflict(SeatIndex index, string rtcUserId, out CloudUserInfoHolder seatUser)
        {
            if (_seatsMap.TryGetValue(index, out seatUser))
            {
                var userInfo = seatUser.Info;
                if (seatUser.State != CloudUserState.None && userInfo.IsValidInfo && rtcUserId != userInfo.RtcUserId)
                    return true;
            }

            return false;
        }

        private CloudUserInfoHolder GetHolder(string rtcUserId, bool autoCreate)
        {
            if (_userIdMap.TryGetValue(rtcUserId, out var holder))
            {
                return holder;
            }

            if (autoCreate)
            {
                holder = new CloudUserInfoHolder();
                _userIdMap[rtcUserId] = holder;
                return holder;
            }

            return null;
        }

        private CloudUserInfoHolder GetSeatInfoHolder(SeatIndex index, bool autoCreate)
        {
            if (_seatsMap.TryGetValue(index, out var holder))
            {
                return holder;
            }

            if (autoCreate)
            {
                holder = new CloudUserInfoHolder();
                _seatsMap[index] = holder;
                return holder;
            }

            return null;
        }

        public bool TryGetByUserId(string rtcUserId, out CloudUserInfo value)
        {
            var hasInfo = _userIdMap.TryGetValue(rtcUserId, out var holder);
            value = holder?.Info ?? default;
            return hasInfo;
        }
    }
}