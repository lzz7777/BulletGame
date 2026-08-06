using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ByteDance.CloudSync
{
    public static class Extensions
    {
        /// <summary>
        /// 获取当前房间内所有 ICloudClient
        /// </summary>
        /// <param name="system"></param>
        /// <returns></returns>
        internal static ICloudClient[] GetClients(this CloudSyncSdk system)
        {
            var all = new List<ICloudClient>();
            system.ClientManager.GetClients(all);
            return all.Where(c => c.State == ClientState.Connected).ToArray();
        }

        /// <summary>
        /// 获取所有非 Host 的客户端
        /// </summary>
        /// <param name="system"></param>
        /// <returns></returns>
        internal static ICloudClient[] GetNonHostClients(this CloudSyncSdk system)
        {
            var all = new List<ICloudClient>();
            system.ClientManager.GetClients(all);
            return all.Where(c => !c.IsHost() && c.State == ClientState.Connected).ToArray();
        }

        /// <summary>
        /// 获取 Host 主播，即房主自己的 ICloudClient 对象。（即 SeatIndex 为 SeatIndex.Index0 的 ICloudClient 对象。）
        /// </summary>
        /// <param name="manager"></param>
        /// <returns></returns>
        internal static ICloudClient GetHostClient(this ICloudClientManager manager)
        {
            return manager.GetClient(SeatIndex.Index0);
        }

        /// <summary>
        /// 是否是 Host 端，即 Index 为 0 的客户端
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        internal static bool IsHost(this ICloudClient client)
        {
            return client.Index.IsHost();
        }

        internal static ICloudSeat[] GetNonHostSeats(this ICloudSync system)
        {
            var all = system.SeatManager.AllSeats;
            return all.Where(c => !c.IsHost() && c.State == SeatState.InUse).ToArray();
        }

        /// <summary>
        /// 是否是 Host 座位，即 Index 为 0 的座位
        /// </summary>
        /// <param name="seat"></param>
        /// <returns></returns>
        public static bool IsHost(this ICloudSeat seat)
        {
            return seat.Index.IsHost();
        }

        /// <summary>
        /// 座位名称，Index 0 为 "房主"，其他为"玩家1"、"玩家2"、"玩家3"
        /// </summary>
        public static string GetSeatName(this ICloudSeat self)
        {
            return self.IsHost() ? "房主" : $"玩家{self.IntIndex + 1}";
        }

        internal static async Task WaitConnected(this ICloudClient client, CancellationToken token)
        {
            if (client.State == ClientState.Connected)
                return;

            while (true)
            {
                if (client.State == ClientState.Connected || token.IsCancellationRequested)
                {
                    break;
                }

                await Task.Yield();
            }
        }

        internal static InitResult Accept(this InitResult self, InitCloudGameResult result)
        {
            self.Code = InitResultCode.Failed;
            self.Message = result.Error;
            return self;
        }
    }
}