using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ByteDance.CloudSync
{
    /// <summary>
    /// 抖音开放平台主播用户信息
    /// </summary>
    [Serializable]
    public abstract class PlayerInfo : IPlayerInfo
    {
        public string openId;
        public string nickName;
        public string avatarUrl;

        public string OpenId => openId;

        public string NickName => nickName;

        public string AvatarUrl => avatarUrl;

        public string ToJson() => JsonUtility.ToJson(this);
    }

    [Serializable]
    public class AnchorPlayerInfo : PlayerInfo, IAnchorPlayerInfo
    {
        /// <summary>
        /// 直播间 room id
        /// </summary>
        public string liveRoomId;

        /// <summary>
        /// 直播间 room token
        /// </summary>
        public string liveRoomToken;

        public string LiveRoomId => liveRoomId;

        public string ToStr() => $"{{ liveRoomId: {liveRoomId}, liveRoomToken: {liveRoomToken} openId: {openId}, nickName: {nickName}, avatarUrl: {avatarUrl} }}";
    }

    /// <summary>
    /// 抖音开放平台主播用户信息
    /// </summary>
    public interface IPlayerInfo
    {
        /// <summary>
        /// 抖音开放平台中用户的 openid
        /// </summary>
        string OpenId { get; }
        /// <summary>
        /// 抖音开放平台中用户的昵称
        /// </summary>
        string NickName { get; }
        /// <summary>
        /// 抖音开放平台中用户的头像URL
        /// </summary>
        string AvatarUrl { get; }

        string ToJson();
    }

    /// <summary>
    /// 抖音开放平台主播用户信息
    /// </summary>
    public interface IAnchorPlayerInfo : IPlayerInfo
    {
        /// <summary>
        /// 直播间 room id
        /// </summary>
        string LiveRoomId { get; }
    }

    /// <summary>
    /// 玩家（主播）信息提供器。
    /// </summary>
    public interface IAnchorPlayerInfoProvider
    {
        /// <summary>
        /// 获取玩家（主播）的用户信息。
        /// </summary>
        /// <seealso cref="AnchorPlayerInfo"/>
        /// <param name="token">取消令牌</param>
        /// <returns></returns>
        Task<AnchorPlayerInfo> FetchPlayerInfo(CancellationToken token);
    }

    /// <summary>
    /// 为非 Host Client 提供主播用户信息获取能力
    /// </summary>
    public interface IMultiAnchorPlayerInfoProvider
    {
        Task<AnchorPlayerInfo> FetchOnJoinPlayerInfo(ICloudSeat seat, CancellationToken token);
    }

    internal class SelfAnchorPlayerInfoProvider : IAnchorPlayerInfoProvider, IMultiAnchorPlayerInfoProvider
    {
        private readonly IAnchorPlayerInfoProvider _inner;

        public SelfAnchorPlayerInfoProvider(IAnchorPlayerInfoProvider inner)
        {
            _inner = inner;
        }

        public Task<AnchorPlayerInfo> FetchPlayerInfo(CancellationToken token)
        {
            return _inner.FetchPlayerInfo(token);
        }

        public Task<AnchorPlayerInfo> FetchOnJoinPlayerInfo(ICloudSeat seat, CancellationToken token)
        {
            return _inner.FetchPlayerInfo(token);
        }
    }
}