// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ByteDance.Live.Foundation.Logging;
using ByteDance.LiveOpenSdk.Push;
using ByteDance.LiveOpenSdk.Report;
using ByteDance.LiveOpenSdk.Room;
using ByteDance.LiveOpenSdk.Round;
using ByteDance.LiveOpenSdk.Runtime.Utilities;
using ByteDance.LiveOpenSdk.Utilities;

namespace Douyin.LiveOpenSDK.Samples
{
    public record RoundStatusInfo : IRoundStatusInfo
    {
        public long RoundId { get; set; }
        public long StartTime { get; set; }
        public long EndTime { get; set; }
        public int Status { get; set; }
        public List<IGroupResultInfo>? GroupResultList { get; set; }
    }
    public record RoundUserGroupInfo : IRoundUserGroupInfo
    {
        public long RoundId { get; set; }
        public string OpenId { get; set; } = "";
        public string GroupId { get; set; } = "";
    }
    public record GroupResultInfo : IGroupResultInfo
    {
        public string GroupId { get; set; } = "";
        public int Result { get; set; }
    }
    /// <summary>
    /// 直播开放 SDK Round能力的接入示例代码。
    /// </summary>
    public class SampleRoundManager
    {
        private static SampleRoundManager _instance;

        public static SampleRoundManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SampleRoundManager();
                }

                return _instance;
            }
        }
        private IRoundApi RoundApi => SampleLiveOpenSdkManager.Sdk.GetRoundApi();
        private readonly LogWriter Log = new LogWriter(SdkUnityLogger.LogSink, "SampleRoundManager");

        // 对局结束时，阵型的结果数据
        private List<IGroupResultInfo> _groupResultInfos = new List<IGroupResultInfo>();

        // 对局状态参数
        private RoundStatusInfo _roundStatusInfo = new RoundStatusInfo();

        // 当前局的RoundId
        private long RoundId { get; set; }

        // 获取当前时间戳
        private long GetTimestamp()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        // 生成 RoundId
        private long GenerateRoundId()
        {
            // 注意一局开始结束后，下一局使用的RoundId不能和上一局相同，本Demo使用当前时间戳秒数
            return GetTimestamp();
        }

        /// <summary>
        /// 开始一局
        /// </summary>
        /// <returns></returns>
        public async Task<IRoundDataRes> StartRoundAsync()
        {
            // 生成RoundId
            // 同一局的RoundId要统一，Demo这里仅在开启一局（Round）时进行RoundId生成
            RoundId = GenerateRoundId();
            // 开始对局状态
            _roundStatusInfo = new RoundStatusInfo()
            {
                RoundId = RoundId,
                StartTime = GetTimestamp(),
                Status = 1,
            };
            try
            {
                var result =  await RoundApi.UpdateRoundStatusInfoAsync(_roundStatusInfo);
                Log.Info($"开始对局 roundId: {RoundId}, result: {result}");
                return result;
            }
            catch (Exception e)
            {
                Log.Error($"开始对局出错 roundId: {RoundId}, e: {e}");
            }

            return null;
        }

        /// <summary>
        /// 加入队伍
        /// </summary>
        /// <param name="openId">要加入某队伍的用户OpenId</param>
        /// <param name="groupId">要加入队伍的名称</param>
        public async void JoinGroup(string openId, string groupId)
        {
            try
            {
                var result = await RoundApi.UpdateUserGroupInfoAsync(new RoundUserGroupInfo()
                {
                    RoundId = RoundId,
                    OpenId = openId,
                    GroupId = groupId
                });
                _groupResultInfos.Add(new GroupResultInfo()
                {
                    GroupId = groupId,
                    Result = 2 // 需要开发者使用自己的胜负方式，这里作为Demo，初始化时默认所有队伍的结果都为失败
                });
                Log.Info($"上报用户阵营 groupId: {RoundId}, OpenId: {openId}, groupId: {groupId}, result: {result}");
            }
            catch (Exception e)
            {
                Log.Error($"上报用户阵营出错 roundId: {RoundId}, OpenId: {openId}, groupId: {groupId}, e: {e}");
            }
        }

        /// <summary>
        /// 结束当前局
        /// </summary>
        /// <returns></returns>
        public async Task<IRoundDataRes> EndRoundAsync()
        {
            try
            {
                if (_groupResultInfos.Count > 0)
                {
                    _groupResultInfos[0].Result = 1; // 需要开发者使用自己的胜负方式，这里作为Demo，算第一个队伍胜利
                }

                _roundStatusInfo.EndTime = GetTimestamp();
                _roundStatusInfo.GroupResultList = _groupResultInfos;
                _roundStatusInfo.Status = 2;
                var result = await RoundApi.UpdateRoundStatusInfoAsync(_roundStatusInfo);
                Log.Info($"结束对局 roundId: {RoundId}, result: {result}");
                return result;
            }
            catch (Exception e)
            {
                Log.Error($"结束对局出错 roundId: {RoundId}, e: {e}");
            }

            return null;
        }

    }
}