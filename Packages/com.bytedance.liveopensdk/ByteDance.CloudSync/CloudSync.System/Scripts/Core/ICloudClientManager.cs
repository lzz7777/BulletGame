using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ByteDance.CloudSync
{
    /// <summary>
    /// CloudClient 管理器
    /// </summary>
    internal interface ICloudClientManager
    {
        /// <summary>
        /// 获取当前房间内所有 ICloudClient
        /// </summary>
        /// <param name="clients"></param>
        void GetClients(List<ICloudClient> clients);

        /// <summary>
        /// 通过指定座位号取得 ICloudClient
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        ICloudClient GetClient(SeatIndex index);

        /// <summary>
        /// 等待对应座位号连接。
        /// </summary>
        /// <param name="index">目标座位号</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ICloudClient> WaitConnected(SeatIndex index, CancellationToken cancellationToken);

        /// <summary>
        /// 检测对应座位号的 Client 是否已连接
        /// </summary>
        /// <param name="index">目标座位号</param>
        /// <returns></returns>
        bool IsConnected(SeatIndex index);
    }
}