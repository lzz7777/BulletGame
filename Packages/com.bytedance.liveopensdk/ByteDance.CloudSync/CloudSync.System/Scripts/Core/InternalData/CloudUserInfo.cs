// Copyright@www.bytedance.com
// Author: zhouxu.ken
// Date: 2024/06/11
// Description:

namespace ByteDance.CloudSync
{
    /// <summary>
    /// 一个云游戏用户的信息（对应端上用户信息） rtcUserId（由于云游戏关闭时获取不到openid，所以增加rtc的userid做唯一标识）, openId
    /// <para> !! 注意！信息读写可能是多线程！ 需要 lock () !! </para>
    /// </summary>
    internal struct CloudUserInfo
    {
        /// 对齐云游戏的`roomIndex`、单实例多路的`seat`. 参考 <see cref="CloudClient.Index"/>
        public SeatIndex Index { get; private set; }

        /// 云游戏rtc的userid. 云游戏链路可信任的id。
        /// <remarks>注意：一般来说不等于抖音uid！</remarks>
        public string RtcUserId { get; }

        public bool IsValidInfo => Index.IsValid() && !string.IsNullOrEmpty(RtcUserId);

        /// <summary>
        /// 是否是主播
        /// </summary>
        public bool IsAnchor => Index.IsAnchor();

        internal void SetIndex(SeatIndex newIndex)
        {
            Index = newIndex;
        }

        internal CloudUserInfo(SeatIndex index, JoinRoomParam param)
        {
            Index = index;
            RtcUserId = param.RTCUserId;
        }

        public CloudUserInfo(SeatIndex index, string rtcUserId)
        {
            Index = index;
            RtcUserId = rtcUserId;
        }

        public override string ToString() => $"index: {Index} rtcUserId: {RtcUserId}";
    }
}