using BestHTTP.JSON.LitJson;
using cfg;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XN
{
    public static class PlayerMessage
    {
        public static async UniTask<LoginRequest> SendLoginRequest(string playerId)
        {
            var userInfo = RoomHelper.GetUserInfo(playerId);
            var request = new LoginGameRequest();
            // ================== 兼容Unity / SDK / =================
            request.PlayerId = playerId;
            request.Nickname = userInfo?.Nickname ?? playerId;
            request.AvatarUrl = ResHelper.GetAvatarUrl(userInfo?.AvatarUrl);
            request.GameName = TotalConfigManager.ConfigManager.ConstConfigCategory.GameName;
            
            var response = await DataManager.AsyncSendPost<LoginRequest>(GameConst.Url.Post_GetPlayerInfo,
                bodyStr: JsonMapper.ToJson(request));

            return response;
        }

        public static async UniTask<GetBagDataResponse> SendBagRequest(string playerId)
        {
            var request = new GetBagDataRequest();
            request.PlayerId = playerId;
            
            var response = await DataManager.AsyncSendPost<GetBagDataResponse>(GameConst.Url.Post_GetBagDataRequest,
                bodyStr: JsonMapper.ToJson(request));
            
            return response;
        }

        public static async UniTask<CmdOperationResponse> SendCmdOperationRequest(string playerId, ECmd cmd, int value = 0)
        {
            var request = new CmdOperationRequest();
            request.PlayerId = playerId;
            request.Data = new CmdOperationData()
            {
                Cmd = (int)cmd,
                Value = value
            };
                
            var response = await DataManager.AsyncSendPost<CmdOperationResponse>(GameConst.Url.Post_CmdOperationRequest,
                bodyStr: JsonMapper.ToJson(request));
            
            return response;
        }
        
        /// <summary>
        /// 获取奖池
        /// </summary>
        public static async UniTask<PrizePoolResponse> SendGetPrizePool(string playerId)
        {
            var request = new PrizePoolRequest();
            request.PlayerId = playerId;
                
            var response = await DataManager.AsyncSendPost<PrizePoolResponse>(GameConst.Url.Post_GetPrizePool,
                bodyStr: JsonMapper.ToJson(request));
            
            return response;
        }
        
        /// <summary>
        /// 设置奖池
        /// </summary>
        public static async UniTask<PrizePoolResponse> SendSetPrizePool(string playerId, double goldPool, double fortunePool)
        {
            var request = new PrizePoolRequest();
            request.PlayerId = playerId;
            request.GoldPool = goldPool;
            request.FortunePool = fortunePool;
                
            var response = await DataManager.AsyncSendPost<PrizePoolResponse>(GameConst.Url.Post_SetPrizePool,
                bodyStr: JsonMapper.ToJson(request));
            
            return response;
        }
    }
}