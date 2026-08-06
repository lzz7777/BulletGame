using System;
using System.Collections.Generic;
using Apifox;
using cfg;
using Cysharp.Threading.Tasks;
using UnityEngine.Events;

namespace GameMain
{
    public static partial class DataManager
    {
        private const string CST_URL_WS_CONFIG = "/ga/api/getWsConfig";
        private const string CST_URL_KS_CONFIG = "ga/api/combat/mq/config";
        private const string CST_URL_KS_GIFT = "ga/api/combat/ks/gift/top";
        private const string CST_URL_TIKTOK_GIFT = "ga/api/combat/tiktok/gift/top";
        private const string CST_URL_KS_START = "ga/api/combat/ks/start";
        private const string CST_URL_TIKTOK_START = "ga/api/combat/tiktok/start";
        private const string CST_URL_GET_DISCONNECT = "/ga/sud/disconnect";
        private const string CST_URL_ADDLOG = "ga/api/combat/addCombatLog";
        private const string CST_URL_GET_DISCONNECT_560 = "/ga/common/close";
        private const string CST_URL_KS_DISCONNECT = "ga/api/combat/ks/force/end";
        private const string CST_URL_TIKTOK_DISCONNECT = "ga/api/combat/tiktok/force/end";
        private static bool _fastConsumer = true;


        /// <summary>
        /// 开始游戏
        /// </summary>
        /// <param name="currIndex"></param>
        /// <param name="callback"></param>
        public static async UniTask<RespRetBase> StartGame(int currIndex)
        {
            if (NoSend) return RespRetBase.Success;

            RespRetBase ret;
            // if (KsOrDy)
            // {
            //     ret = await PubGameStart(currIndex);
            // }
            // else
            // {
                var resp = await SendAddLog(currIndex);
                CombatId = resp.data;
                ret = resp;
            // }

            if (ret.code == 0)
            {
                ret = await SyncStatus(1);
                Consumer();

                var arg0 = await UpdateGameGift();
                if (arg0.code != 0)
                    Debug.LogError("礼物确认失败");
            }

            return ret;

            void Consumer()
            {
                if (_fastConsumer)
                {
                    // if (Game560)
                    // {
                    //     GetWsConfig(ret =>
                    //     {
                    //         if (ret == default)
                    //         {
                    //             Log("获取Socket配置失败");
                    //             Debug.LogError("获取配置失败");
                    //             return;
                    //         }
                    //
                    //         if (ret.code != 0)
                    //         {
                    //             Log("获取Socket配置失败");
                    //             Debug.LogError("获取配置失败");
                    //             return;
                    //         }
                    //
                    //         _fastConsumer = !_fastConsumer;
                    //         // Socket560Manager.Instance.Consumer(ret.data);
                    //     });
                    // }
                }
            }
        }

        public static async void QuitGame(UnityAction<RespRetBase> callback)
        {
            if (NoSend)
            {
                callback.Invoke(RespRetBase.Success);
                return;
            }

            RespRetBase b;
            // if (Sud)
            //     b = await SudDisconnect();
            // else
                b = await PubDisconnect();

            callback?.Invoke(b);
        }

        private static async UniTask<RespRetBase> SudDisconnect()
        {
            var param = new Dictionary<string, string>
            {
                { "roomCode", _roomId }
            };

            return await AsyncSendGet<RespRetBase>(CST_URL_GET_DISCONNECT, param: param);
        }

        private static async UniTask<RespRetBase> PubDisconnect()
        {
            var param = new Dictionary<string, string>
            {
                { "combatId", CombatId }
            };

            return await AsyncSendGet<RespRetBase>(Channel switch
            {
                // ChannelCmd.快手 => CST_URL_KS_DISCONNECT,
                ChannelCmd.DouYin => CST_URL_TIKTOK_DISCONNECT,
                // ChannelCmd.Game560 => CST_URL_GET_DISCONNECT_560,
                _ => throw new ArgumentOutOfRangeException()
            }, param: param);
        }

        /// <summary>
        /// 开始游戏难度(仅快手
        /// </summary>
        private static async UniTask<RespRetBase> PubGameStart(int level)
        {
            var param = new Dictionary<string, object>
            {
                { "combatId", CombatId },
                { "level", level.ToString() }
            };

            var resp = await AsyncSendPost<RespRetBase>(Channel switch
            {
                // ChannelCmd.快手 => CST_URL_KS_START,
                ChannelCmd.DouYin => CST_URL_TIKTOK_START,
                _ => throw new ArgumentOutOfRangeException()
            }, body: param);

            if (resp.code != 0) Debug.LogError("DataManager 开始本局游戏失败");

            return resp;
        }

        private static async UniTask<RespRetString> SendAddLog(int diff)
        {
            var param = new Dictionary<string, object>
            {
                { "rank", diff.ToString() },
                { "roomId", _roomId }
            };

            var resp = await AsyncSendPost<RespRetString>(CST_URL_ADDLOG, body: param);
            if (resp.code != 0)
            {
                Debug.LogError("DataManager 添加记录失败");
                return resp;
            }

            _recordId = resp.data;
            return resp;
        }

        /// <summary>
        /// 更新礼物显示
        /// </summary>
        /// <param name="attempts">尝试次数</param>
        private static async UniTask<RespRetBase> UpdateGameGift(int attempts = 5)
        {
            // if (!KsOrDy) return RespRetBase.Success;

            await UniTask.Delay(500);
            while (--attempts > 0)
            {
                var resp = await PubSendGameGift();
                if (resp.code == 0) return resp;
            }

            return RespRetBase.Error;
        }

        private static async UniTask<RespRetBase> PubSendGameGift()
        {
            var param = new Dictionary<string, object>
            {
                { "combatId", CombatId }
            };
            // if (Ks)
            // {
            //
            //     var quickInfos = new List<Dictionary<string, string>>();
            //     foreach (var topButtonConfig in buttonInfo)
            //     {
            //         var dic = new Dictionary<string, string>
            //         {
            //             { "buttonText", topButtonConfig.ButtonText },
            //             { "buttonColor", topButtonConfig.ButtonColor },
            //             { "commentText", topButtonConfig.CommentText }
            //         };
            //         quickInfos.Add(dic);
            //     }
            //
            //     param.Add("quickInfos", quickInfos);
            // }
            //
            // var gifts = GiftPriceLst.Where(t => t.Channel == Channel).Select(t => t.ChannelSignID).ToList();
            // param.Add("gifts", gifts);

            var resp = await AsyncSendPost<RespRetBase>(Channel switch
            {
                // ChannelCmd.快手 => CST_URL_KS_GIFT,
                ChannelCmd.DouYin => CST_URL_TIKTOK_GIFT,
                _ => throw new ArgumentOutOfRangeException()
            }, body: param);

            if (resp.code != 0) Debug.LogError("DataManager 绑定礼物失败");

            return resp;
        }

        /// <summary>
        /// 获取websocket链接
        /// </summary>
        /// <param name="callback"></param>
        private static void WsGetConfig(UnityAction<RespRet<WsCombatConfigVo>> callback)
        {
            AsyncSendGet<RespRet<WsCombatConfigVo>>(CST_URL_WS_CONFIG, callback: resp =>
            {
                if (resp.code != 0)
                {
                    Debug.LogError("DataManager 获取websocket战斗队列配置失败");
                    callback?.Invoke(resp);
                    return;
                }

                callback?.Invoke(resp);
            });
        }

        /// <summary>
        /// 获取战斗队列配置
        /// </summary>
        private static void KsGetConfig(UnityAction<RespRet<GaCombatConfigVo>> callback)
        {
            var param = new Dictionary<string, object>
            {
                { "combatId", CombatId }
            };

            AsyncSendPost<RespRet<GaCombatConfigVo>>(CST_URL_KS_CONFIG, callback: resp =>
            {
                if (resp.code != 0)
                {
                    Debug.LogError("DataManager 获取战斗队列配置失败");
                    callback?.Invoke(resp);
                    return;
                }

                callback?.Invoke(resp);
            }, body: param);
        }

        #region 中间

        /// <summary>
        /// 获取Websocket配置
        /// </summary>
        /// <param name="callback"></param>
        private static void GetWsConfig(UnityAction<RespRet<WsCombatConfigVo>> callback)
        {
            if (NoSend)
            {
                callback.Invoke(default);
                return;
            }

            WsGetConfig(callback);
        }

        /// <summary>
        /// 获取MQ配置
        /// </summary>
        /// <param name="callback"></param>
        private static void GetConfig(UnityAction<RespRet<GaCombatConfigVo>> callback)
        {
            if (NoSend)
            {
                callback.Invoke(default);
                return;
            }

            // if (Wx)
            // {
            //     callback.Invoke(default);
            //     return;
            // }

            KsGetConfig(callback);
        }

        #endregion
    }
}