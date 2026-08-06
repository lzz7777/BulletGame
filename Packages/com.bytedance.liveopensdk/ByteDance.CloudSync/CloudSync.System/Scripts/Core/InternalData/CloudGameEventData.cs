namespace ByteDance.CloudSync
{
    public readonly struct PlayerEnterData
    {
        /// 对齐云游戏的`roomIndex`、单实例多路的`seat`. 参考 <see cref="CloudClient.Index"/>
        public readonly SeatIndex seat;

        /// 云游戏rtc的userid
        public readonly string rtcUserId;

        internal PlayerEnterData(SeatIndex seat, CloudUserInfo userInfo)
        {
            this.seat = seat;
            rtcUserId = userInfo.RtcUserId;
        }

        public override string ToString() => $"{{ seat: {seat}, rtcUserId: {rtcUserId} }}";
    }

    public struct PlayerLeaveData
    {
        public SeatIndex seat;
        public string rtcUserId;

        internal PlayerLeaveData(SeatIndex seat, CloudUserInfo userInfo)
        {
            this.seat = seat;
            rtcUserId = userInfo.RtcUserId;
        }

        public override string ToString() => $"{{ seat: {seat}, rtcUserId: {rtcUserId} }}";
    }

    public struct CustomMessageData
    {
        /// 对齐云游戏的`roomIndex`、单实例多路的`seat`. 参考 <see cref="CloudClient.Index"/>
        public SeatIndex index;
        /// 消息 json string
        public string message;
        public override string ToString() => $"{{ index: {index}, message: {message} }}";
    }
}