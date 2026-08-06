using System.Collections.Generic;
using Apifox;
using cfg.Global;
using cfg.Net;
using Cysharp.Threading.Tasks;

namespace GameMain
{
    public static partial class DataManager
    {
        private const string CST_URL_LOGIN_OTHER = "ga/public/api/login";
        private const string CST_URL_REFRESH_LOGIN = "ga/refreshLogin1";
        private const string CST_URL_KS_COMBAT = "ga/public/api/combatId";

        private static LoginInfoConfigCategory loginInfoConfig = TotalConfigManager.ConfigManager.LoginInfoConfigCategory;
        private static ConstConfigCategory ConstConfig => TotalConfigManager.ConfigManager.ConstConfigCategory;

        public static string Token { get; private set; }

        public static void SetToken(string instanceLocalToken)
        {
            Log($"当前调试token: {instanceLocalToken}");
            Token = instanceLocalToken;
        }

        #region 开放接口

        /// <summary>
        /// 更新房间号
        /// </summary>
        /// <param name="r"></param>
        /// <param name="pwd"></param>
        public static void UpdateRoomId(string r, string pwd = "")
        {
            _roomId = r.Trim();
            _roomPwd = pwd.Trim();

            if (_roomId is "12345" or "11111")
                NoSend = true;
            else
                NoSend = false;

            Log($"当前房间号: {r}");
        }

        /// <summary>
        /// 固定登录流程
        /// </summary>
        /// <returns></returns>
        public static async UniTask<bool> DoLoginHelper()
        {
            var debug = false;

#if UNITY_EDITOR
            //调试模式
            if (!string.IsNullOrEmpty(Token)) debug = true;
#endif
            if (!debug)
            {
                var result = await GetCombatId();
                if (result.code != 0)
                {
                    Debug.LogError("获取CombatId失败");
                    return false;
                }

#if UNITY_EDITOR
                if (!string.IsNullOrEmpty(DebugManager.Instance.LocalToken))
                {
                    Token = DebugManager.Instance.LocalToken;
                    return true;
                }

#endif
                var ret = await SendLogin();
                if (ret is not { code: 0 })
                {
                    Debug.LogError("登录失败");
                    return false;
                }
            }

            //开始链接IGame
            // IOGameSocketManager.Instance.Connect();

            //开启定时
            // CountdownDisplayRanking();

            return true;
        }

        /// <summary>
        /// 重新登录
        /// </summary>
        /// <returns></returns>
        public static async UniTask<bool> RefreshLogin()
        {
            // if (Config.Channel == ChannelCmd.快手) {
            // 等待结果
            var result = await GetCombatId();
            if (result.code != 0)
            {
                Debug.LogError("获取CombatId失败");
                return false;
            }

            result = await RefreshToken();
            if (result.code != 0)
            {
                Debug.LogError("登录失败");
                return false;
            }

            return true;
        }

        #endregion

        #region 中间层

        /// <summary>
        /// 获取CombatId
        /// </summary>
        /// <param name="callback"></param>
        private static async UniTask<RespRetString> GetCombatId()
        {
            // if (!KsOrDy) return RespRetString.Success;

            if (NoSend) return RespRetString.Success;

            return await KsGetCombatId();
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="callback"></param>
        private static async UniTask<RespRetString> SendLogin()
        {
            if (NoSend) return RespRetString.Success;

            return await SendLoginOther();
        }

        /// <summary>
        /// 刷新token
        /// </summary>
        /// <param name="callback"></param>
        private static async UniTask<RespRetString> RefreshToken()
        {
            if (NoSend) return RespRetString.Success;

            return await SendRefreshToken();
        }

        #endregion

        #region 请求链接

        /// <summary>
        /// 请求token
        /// </summary>
        /// <returns></returns>
        private static async UniTask<RespRetString> SendLoginOther()
        {
            var loginInfo = loginInfoConfig.GetOrDefault(ConstConfig.CurrChannel);
            var param = new Dictionary<string, object>
            {
                //必填；H5--内部H5方式登录，SUD--忽然科技(需要重新调整，暂时忽略)，KS--快手平台
                { "channel", loginInfo.LoginChannel },
                //非必填；默认1000
                { "channelId", loginInfo.LoginChannelId },
                { "appId", loginInfo.AppID }
            };
            // if (Game560)
            // {
            //     param.Add("userName", _roomId.Trim());
            //     param.Add("password", _roomPwd.Trim());
            // }
            // else
            // {
                param.Add("code", _roomId.Trim());
            // }

            // if (KsOrDy)
            // {
            //     // combatId：非必填；暂时只支持 channel=KS渠道
            //     param.Add("combatId", CombatId);
            //     if (Dy)
            //     {
            //         param.Add("openStart", "0");
            //     }
            // }

            var resp = await AsyncSendPost<RespRetString>(CST_URL_LOGIN_OTHER, body: param);
            if (resp is not { code: 0 })
            {
                Debug.LogError("DataManager 登录失败");
                return resp;
            }

            Token = resp.data;
            // AsyncSendGet<RespRetLst<PlayerBuffInfo>>

            // if (Sud) GetStatus();

            // UpdatePlayerBuff();


            return resp;
        }

        /// <summary>
        /// 刷新token
        /// </summary>
        /// <param name="callback"></param>
        private static async UniTask<RespRetString> SendRefreshToken()
        {
            var loginInfo = loginInfoConfig.GetOrDefault(ConstConfig.CurrChannel);
            var param = new Dictionary<string, object>
            {
                //必填；H5--内部H5方式登录，SUD--忽然科技(需要重新调整，暂时忽略)，KS--快手平台
                { "channel", loginInfo.LoginChannel },
                //非必填；默认1000
                { "channelId", loginInfo.LoginChannelId }
                //必填；房间code码
                //{ "code", _roomId.Trim() },
            };

            // if (KsOrDy)
            // {
            //     if (Ks)
            //     {
            //         param.Add("code", _roomId.Trim());
            //     }
            //
            //     //非必填；暂时只支持 channel=KS渠道
            //     param.Add("appId", ChannelConfig.AppID);
            //     // combatId：非必填；暂时只支持 channel=KS渠道
            //     param.Add("combatId", CombatId);

                if (Dy)
                {
                    param.Add("openStart", "0");
                }
            // }

            var resp = await AsyncSendPost<RespRetString>(CST_URL_REFRESH_LOGIN, body: param);
            if (resp.code != 0)
            {
                Debug.LogError("DataManager 登录失败");
                return resp;
            }

            // Token = resp.data;
            return resp;
        }


        /// <summary>
        /// 获取CombatId(仅快手
        /// </summary>
        private static async UniTask<RespRetString> KsGetCombatId()
        {
            var resp = await AsyncSendPost<RespRetString>(CST_URL_KS_COMBAT);
            if (resp.code != 0)
            {
                Debug.LogError("DataManager 获取CombatId失败");
                return resp;
            }

            CombatId = resp.data;
            return resp;
        }

        #endregion
    }
}