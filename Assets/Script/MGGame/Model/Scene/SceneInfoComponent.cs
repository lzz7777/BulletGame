namespace XN
{
    public class SceneInfoComponent : IComponent
    {
        public string AnchorOpenId { get; set; }
        public string RoomId { get; set; }

        /// <summary>
        /// 场景组id
        /// </summary>
        public int SceneId { get; set; }

        /// <summary>
        /// 上吧积分池
        /// </summary>
        public double LastScorePool { get; set; }

        /// <summary>
        /// 上吧粉丝池
        /// </summary>
        public double LastFansPool { get; set; }

        public override void OnCreate()
        {
            SceneId = 1;
        }

        public override void OnDestroy()
        {
            AnchorOpenId = default;
            RoomId = default;
            SceneId = default;
            LastScorePool = default;
            LastFansPool = default;
        }
    }
}