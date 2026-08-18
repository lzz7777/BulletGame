using System.Collections.Generic;

namespace XN
{
    public enum SexType
    {
        Male,
        Female,
    }

    /// <summary>
    /// ByteDance.LiveOpenSdk.Room.IUserInfo
    /// </summary>
    public class UserInfo
    {
        // 用户的 OpenID。
        public string OpenId;

        /// <summary>用户的头像 URL。</summary>
        public string AvatarUrl;

        /// <summary>用户的昵称。</summary>
        public string Nickname;

        /// <summary>
        /// 是不是Unity下的 编辑器数据
        /// </summary>
        public bool IsEditor = false;
    }

    public class PlayerInfoComponent : IComponent
    {
        public string PlayerId;
        public long CarId { get; set; }
        public string Name { get; set; }
        public string AvatarUrl { get; set; }

        /// <summary>
        /// 是否完成落座
        /// </summary>
        public bool IsTakeSeat { get; set; }

        /// <summary>
        /// SDK个人玩家信息，看这里
        /// </summary>
        public UserInfo UserInfo;

        /// <summary>
        /// 皮肤
        /// </summary>
        public int SkinId { get; set; }

        /// <summary>
        /// 定制入场视频
        /// </summary>
        public string CustomVideoId { get; set; }
        
        /// <summary>
        /// 特效皮肤
        /// </summary>
        public Dictionary<int, int> Effects { get; set; } = new();

        /// <summary>
        /// 原始积分
        /// </summary>
        public double OrigScore { get; set; }

        /// <summary>
        /// 原始粉丝
        /// </summary>
        public double OrigFans { get; set; }

        /// <summary>
        /// 贡献的积分
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// 触发增加积分的时间
        /// </summary>
        public long ScoreTime { get; set; }

        /// <summary>
        /// 贡献的里程
        /// </summary>
        public double Mileage { get; set; }

        /// <summary>
        /// 赢得粉丝
        /// </summary>
        public double WinFans { get; set; }

        /// <summary>
        /// 赢得积分
        /// </summary>
        public double WinScore { get; set; }

        /// <summary>
        /// 记录扣掉进池子的粉丝
        /// </summary>
        public double LoseFans { get; set; }
        
        /// <summary>
        /// 指令总数
        /// </summary>
        public Dictionary<int, int> InputSumDic { get; set; } = new();

        /// <summary>
        /// 指令数量触发
        /// </summary>
        public Dictionary<int, int> InputQuantity { get; set; } = new();

        /// <summary>
        /// 玩家标签
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 性别
        /// </summary>
        public SexType Sex { get; set; }
        
        /// <summary>
        /// 是否保底粉丝
        /// </summary>
        public bool IsBaoDiFans { get; set; }
        
        public override void OnCreate()
        {
        }

        public override void OnDestroy()
        {
            PlayerId = default;
            CarId = default;
            Name = default;
            AvatarUrl = default;
            IsTakeSeat = default;
            SkinId = default;
            CustomVideoId = default;
            Effects.Clear();
            OrigScore = default;
            OrigFans = default;
            Score = default;
            ScoreTime = default;
            Mileage = default;
            WinFans = default;
            WinScore = default;
            LoseFans = default;
            InputSumDic.Clear();
            InputQuantity.Clear();
            Title = default;
            Sex = default;
            IsBaoDiFans = false;
        }
    }
}