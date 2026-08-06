using Apifox;
using Cysharp.Threading.Tasks;

namespace GameMain
{
    public static partial class DataManager
    {
        private const string CST_URL_GET_TIME = "/ga/public/api/getServerTimeStamp";

        /// <summary>
        /// 获取服务器时间
        /// </summary>
        /// <param name="callback"></param>
        public static async UniTask<RespRetString> GetTime()
        {
            return await AsyncSendGet<RespRetString>(CST_URL_GET_TIME);
        }
    }
}