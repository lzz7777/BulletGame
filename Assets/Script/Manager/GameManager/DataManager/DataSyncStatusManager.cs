using System.Collections.Generic;
using Apifox;
using Cysharp.Threading.Tasks;

namespace GameMain
{
    public static partial class DataManager
    {
        private const string CST_URL_TIKTOK_SYNCSTATUS = "/ga/api/combat//tiktok/syncStatus";
        private static long startTime;

        /// <summary>
        /// 获取战斗队列配置
        /// </summary>
        public static async UniTask<RespRetBase> SyncStatus(int status, List<TiktokResult> obj = null)
        {
            if (NoSend)
            {
                return RespRetBase.Success;
            }

            if (!Dy) return RespRetBase.Success;

            var param = new Dictionary<string, object>
            {
                //战斗id
                { "combatId", CombatId },
                //当前房间的游戏对局状态（1=已开始、2=已结束）
                { "status", status }
            };
            if (obj != null)
            {
                //status=2的时候需要传
                param.Add("gaTiktokResultDtos", obj);
                //秒级时间戳
                //秒级时间戳 状态等于2的时候必传
                param.Add("endTime", DateTimeHelper.Timestamp);
            }
            else
            {
                //秒级时间戳
                startTime = DateTimeHelper.Timestamp;
            }

            param.Add("startTime", startTime);

            var attempts = 5;
            RespRetBase resp = default;
            while (--attempts > 0)
            {
                resp = await AsyncSendPost<RespRetBase>(CST_URL_TIKTOK_SYNCSTATUS, body: param);
                if (resp.code == 0) break;

                Debug.LogError("DataManager 推送同步对局状态失败");
            }

            return resp;
        }

        public class TiktokResult
        {
            /// <summary>
            /// 阵营id，取值来源来自开发者平台「进阶礼物配置」的group_id，如：red
            /// </summary>
            public string groupId;

            /// <summary>
            /// 对局结果（1=胜利、2=失败、3=平局）
            /// </summary>
            public int result;
        }
    }
}