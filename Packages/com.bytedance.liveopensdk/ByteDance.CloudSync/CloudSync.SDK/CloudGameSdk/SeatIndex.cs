// Copyright (c) Bytedance. All rights reserved.
// Description:

namespace ByteDance.CloudSync
{
    /// <summary>
    /// 房间座位号枚举，支持最多 4 个座位，即 0 ~ 3。 0 号位为房主自己。<br/>
    /// </summary>
    public enum SeatIndex
    {
        /// <summary>
        /// 无效的座位号
        /// </summary>
        Invalid = -1,

        /// <summary>
        /// 座位号 0（房主座位号）
        /// </summary>
        Index0 = 0,

        /// <summary>
        /// 座位号 1
        /// </summary>
        Index1 = 1,

        /// <summary>
        /// 座位号 2
        /// </summary>
        Index2 = 2,

        /// <summary>
        /// 座位号 3
        /// </summary>
        Index3 = 3,

        MaxIndex3 = Index3
    }

    public static class ClientIndexUtils
    {
        public static int ToInt(this SeatIndex index)
        {
            return (int)index;
        }

        /// <summary>
        /// 是否有效的roomIndex (在单实例连屏玩法为0~3，在单实例匹配待定x~x)
        /// </summary>
        public static bool IsValid(this SeatIndex index)
        {
            return index is >= SeatIndex.Index0 and <= SeatIndex.MaxIndex3;
        }

        /// <summary>
        /// 是否是 Host 端，即 Index 为 0 的客户端
        /// </summary>
        public static bool IsHost(this SeatIndex index)
        {
            return index == SeatIndex.Index0;
        }

        internal static bool IsAnchor(this SeatIndex index)
        {
            return index.IsValid();
        }
    }
}