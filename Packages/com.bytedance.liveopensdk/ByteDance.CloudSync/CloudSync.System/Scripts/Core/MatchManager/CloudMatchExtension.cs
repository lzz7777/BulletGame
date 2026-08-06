using System;
using JetBrains.Annotations;
using ByteDance.CloudSync.Match;

namespace ByteDance.CloudSync.MatchManager
{
    internal static class CloudMatchExtension
    {
        public static string ExceptionToMessage([CanBeNull] Exception exception)
        {
            if (exception is AggregateException aggregateException && aggregateException.InnerExceptions.Count == 1)
                exception = aggregateException.InnerExceptions[0];
            if (exception != null)
                return $"{exception.GetType().Name}: {exception.Message}";
            return "exception: unknown error";
        }
    }

    internal static class MatchInfoExtention
    {
        internal static MatchPb.MatchInfo ToMatchInfo(this MatchConfig self, int olympusAppId)
        {
            var info = new MatchPb.MatchInfo
            {
                OlympusAppId = olympusAppId,
                StarkAppId = self.MatchAppId,
                ApiName = self.PoolName,
                MatchTag = self.MatchTag
            };
            return info;
        }

        internal static string ToStr(this MatchConfig self) =>
            $"{self.MatchAppId}, {self.PoolName}, {self.MatchTag}";
    }

    internal static class AnchorPlayerInfoExtension
    {
        internal static AnchorPlayerInfo Accept(this AnchorPlayerInfo self, WebCastInfo webCastInfo)
        {
            self.openId = webCastInfo.OpenID;
            self.nickName = webCastInfo.NickName;
            self.avatarUrl = webCastInfo.AvatarURL;
            self.liveRoomId = webCastInfo.LiveRoomID.ToString();
            return self;
        }
    }

    internal static class MatchResultUserExtension
    {
        internal static MatchResultUser Accept(this MatchResultUser self, MatchPb.MatchResultUser user, SeatIndex roomIndex)
        {
            self.RoomIndex = roomIndex;
            self.OpenId = user.OpenId;
            self.AvatarUrl = user.AvatarUrl;
            self.Nickname = user.NickName;
            self.LiveRoomId = user.LiveRoomId.ToString();
            self.ExtraInfo = user.ExtraInfo;
            return self;
        }

        public static string ToStr(this MatchResultUser self, bool withExtra = false) =>
            "{ " +
            $"#{self.RoomIndex}, openId: {self.OpenId}, nickname: {self.Nickname}, avatarUrl: {self.AvatarUrl}, liveRoomId: {self.LiveRoomId}" +
            (withExtra ? $", extra: {self.ExtraInfo}" : "") +
            " }";
    }
}