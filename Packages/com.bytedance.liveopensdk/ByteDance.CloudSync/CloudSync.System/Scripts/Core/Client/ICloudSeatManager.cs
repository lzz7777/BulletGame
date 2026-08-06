// Copyright (c) Bytedance. All rights reserved.
// Description:

using System.Collections.Generic;

namespace ByteDance.CloudSync
{
    public interface ICloudSeatManager
    {
        /// <summary>
        /// 房主座位
        /// </summary>
        ICloudSeat HostSeat { get; }

        /// <summary>
        /// 通过座位号获取对应的座位
        /// </summary>
        /// <param name="index">座位号</param>
        /// <returns></returns>
        /// <remarks>
        /// 在云同步初始化成功后，座位Seat总是保持有对象、非空。注意，Seat非空并不表示座位上就有用户。
        /// 若要判断Seat上用户是否加入、离开，请监听事件 <see cref="OnPlayerJoined"/>、<see cref="OnPlayerLeaving"/>、或判断状态枚举 <see cref="ICloudSeat.State"/>
        /// </remarks>
        ICloudSeat GetSeat(SeatIndex index);

        /// <summary>
        /// 获取所有座位
        /// </summary>
        IEnumerable<ICloudSeat> AllSeats { get; }

        /// <summary>
        /// 目标座位号上是否有玩家
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        bool IsJoined(SeatIndex index);
    }
}