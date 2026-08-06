using System.Collections.Generic;

namespace XN
{
    /// <summary>
    /// 获取背包数据请求
    /// </summary>
    public class GetBagDataRequest
    {
        /// <summary> 玩家id </summary>
        public string PlayerId { get; set; }
    }

    /// <summary>
    /// 获取背包数据响应
    /// </summary>
    public class GetBagDataResponse
    {
        /// <summary> 玩家id </summary>
        public string PlayerId { get; set; }

        /// <summary> 背包数据 </summary>
        public List<BagData> BagDataList { get; set; } = new List<BagData>();
    }

    /// <summary>
    /// 背包数据
    /// </summary>
    public class BagData
    {
        /// <summary> 物品id </summary>
        public long ItemId { get; set; }

        /// <summary> 物品数量 </summary>
        public double ItemNum { get; set; }

        // /// <summary> 过期时间  0表示永不过期 </summary>
        public long ExpirationAt { get; set; }
    }
}