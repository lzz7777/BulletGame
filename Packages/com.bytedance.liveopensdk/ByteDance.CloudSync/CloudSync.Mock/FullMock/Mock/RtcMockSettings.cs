namespace ByteDance.CloudSync.Mock
{
    internal class RtcMockSettings
    {
        internal bool IsInitialized { get; set; }

        internal string RtcUserId { get; set; } = MockUtils.MockRandomUserId();

        internal string RoomId { get; set; } = MockUtils.MockRandomRoomId();

        /// 我方的云端Pod的Token，同时也是我方做房主时的HostToken
        internal string PodToken => $"pod-token-room{RoomId}";

        /// 我方做房主时的HostToken
        internal string HostToken => PodToken;

        internal AnchorPlayerInfo PlayerInfo { get; set; }
    }
}