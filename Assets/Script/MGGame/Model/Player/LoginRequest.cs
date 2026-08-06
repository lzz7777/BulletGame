using System.Collections.Generic;

namespace XN
{
    public class LoginGameRequest
    {
        public string PlayerId { get; set; }
        public string Nickname { get; set; }
        public string AvatarUrl { get; set; }
        public string GameName { get; set; }
    }
    
    public class LoginRequest
    {
        public string PlayerId { get; set; }
        public string Nickname { get; set; }
        public string AvatarUrl { get; set; }
        // 性别  0: 男 1: 女
        public int Sex { get; set; }
        public string SkinName { get; set; }
        public long SkinId { get; set; }
        public string VideoId { get; set; }
        //定制皮肤 20260227：前端不管
        // public long CookieSkinId { get; set; }
        public List<long> EffectsId { get; set; }
        public List<RankNode> Ranks { get; set; }
    }

    public class RankNode
    {
        /// <summary>
        /// 自己强转枚举 - 与配置保持一致
        /// </summary>
        public int RankType { get; set; }
        public int rankIndex {get; set;}
        public double score { get; set; }
    }
}