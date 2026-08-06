// ******************************************************************
// @file       CmdManager.cs
// @brief      游戏指令处理
// @author     SamuelZon, zonsamuel@gmail.com
//             
// @Modified   2023-10-12
// @Copyright  Copyright (c) 2023, BarrageKnight
// ******************************************************************

using System;
using System.Collections.Generic;
using cfg;
using Cysharp.Threading.Tasks;
using GameMain;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 道具分发控制器
/// </summary>
public class CmdManager : MonoSingleton<CmdManager>
{
    private static Queue<string> _botNameQueue = new();
    [LabelText("测试排名的名次"), SerializeField] private int testRank;
    private int testContinueWin;

    private void Update()
    {
        ReplaceUser();
    }

    public static string GetBotName()
    {
        return _botNameQueue.Dequeue();
    }

    //重新初始化
    public void Init()
    {
        _userQuantity.Clear();
        _asyncQueue.Clear();
        _firstRewardOnce.Clear();
        // _firstRewardNum = FirstRewardConfig.RewardNum;
    }

    protected override void OnInit()
    {
        Init();
        EventsManager.AddListener<int>(InputSystemEvent.EventUseProp, RandomUseProps);
        EventsManager.AddListener<int>(InputSystemEvent.ChangesUser, OnChangesUser);
    }

    protected override void OnRemove()
    {
        EventsManager.RemoveListener<int>(InputSystemEvent.EventUseProp, RandomUseProps);
        EventsManager.RemoveListener<int>(InputSystemEvent.ChangesUser, OnChangesUser);
    }

    //按键加入新用户
    private void OnChangesUser(int order)
    {
        var id = Guid.NewGuid().ToString();
        switch (order)
        {
            case 1:
                UpdateUserInfo(id, GetBotName(), TotalConfigManager.GetHand(), true);
                // TryAddUser(id, ECmd.加入队伍1 + Random.Range(0, GameConfig.GroupNum));
                break;
        }
        // TryAddUser(id);
    }

    //按键使用随机道具
    private void RandomUseProps(int jsonId)
    {
        if (getRandomPlayer(out var id)) UseProps(jsonId, id, 1);
    }

    /// <summary>
    /// 记录用户信息
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="userName"></param>
    /// <param name="handUrl"></param>
    /// <param name="isBot"></param>
    public int UpdateUserInfo(string uid, string userName, string handUrl, bool isBot = false)
    {
        // if (!GameData.HasUserInfo(uid))
        // {
        //     DataManager.GetUserProp(uid);
        // }
        //
        // return GameData.TryAddUserInfo(new UserInfo
        // {
        //     Uid = uid,
        //     UserName = userName,
        //     HeadUrl = handUrl,
        //     IsBot = isBot
        // });
        return 0;
    }

    /// <summary>
    /// 根据ID 使用道具
    /// </summary>
    /// <param name="eCmd">道具jsonID</param>
    /// <param name="id"></param>
    /// <param name="count">一次释放数量</param>
    /// <param name="nativeCount">真是数量</param>
    /// <param name="timestamp">时间戳</param>
    /// <param name="overrideValue">覆盖指定属性</param>
    public void UseProps(ECmd eCmd, int id, int count, int nativeCount, long timestamp, int overrideValue = 0)
    {
        Debug.LogWarning("使用道具 " + eCmd);
        // EventsManager.BroadCast(GameEnum.CmdAddBuff, new xxx);
    }

    /// <summary>
    /// 根据ID 使用道具
    /// </summary>
    /// <param name="jsonId">道具jsonID</param>
    /// <param name="id"></param>
    /// <param name="count"></param>
    public void UseProps(int jsonId, int id, int count)
    {
        var propsType = (ECmd)jsonId;
        UseProps(propsType, id, count, count, (long)DateTimeHelper.TimestampMs);
    }

    /// <summary>
    /// 获取随机玩家
    /// </summary>
    /// <returns></returns>
    private bool getRandomPlayer(out int index)
    {
        index = -1;
        // var arr = _airplanequery.ToEntityArray(Allocator.Temp);
        // if (arr.Length <= 0) {
        //     index = -1;
        //     return false;
        // }
        //
        // index = EntityManager.GetComponentData<DollyCart>(arr[Random.Range(0, arr.Length)]).ID;
        return true;
    }

    #region 玩家进出

    private delegate UniTask ReplaceUserAction();

    private readonly Queue<ReplaceUserAction> _asyncQueue = new();

    private bool _runReplace = true;

    private async void ReplaceUser()
    {
        if (!_runReplace || _asyncQueue.Count <= 0) return;
        _runReplace = false;
        await _asyncQueue.Dequeue().Invoke();
        _runReplace = true;
    }

    /// <summary>
    /// 添加用户
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="autoJoinRoom"></param>
    /// <param name="group">进入队伍</param>
    public bool TryAddUser(string uid, ECmd group = ECmd.None, bool autoJoinRoom = false)
    {
        return true;
    }

    // public bool TryChangeSkin(string uid, int skin = 0)
    // {
    //     if (GameData.HasUser(uid))
    //     {
    //         //更换皮肤
    //         if (GameData.TryGetAuthorityInfo(uid, out var info) && info.CreateHandOver)
    //         {
    //             UpdateAirplaneData.UpdateChangeSkin(uid, skin);
    //         }
    //     }
    //
    //     return false;
    // }

    // private static TbOtherConfig OtherConfig => TotalConfigManager.ConfigManager.TbOtherConfig;

    /// <summary>
    /// 实例化头像对象
    /// </summary>
    public bool TryCreateHand(string uid, ECmd cmd, bool autoJoinRoom)
    {
        // if (!GameData.TryGetAuthorityInfo(uid, out var info)) return false;
        // if (info.CreateHandOver) return false;
        // if (GameConfig.IsGroupGame) {
        var group = -1;

        // 加入队伍
        // if (cmd >= ECmd.加入队伍1)
        // {
        //     group = cmd - ECmd.加入队伍1;
        // }
        // else if (autoJoinRoom)
        // {
        //     //随机一个队伍
        group = Random.Range(0, GameConfig.GroupNum);
        // }
        //
        if (group < 0)
        {
            return false;
        }

        //已存在用户
        // GameData.AddInteractiveInfo(uid, (InteractiveID)((int)InteractiveID.加入队伍1 + group));
        // info.UpdateGroup(uid, group);
        return true;
    }

    #endregion

    #region 收到消息处理

    /// <summary>
    /// 根据名称寻找配置
    /// </summary>
    // public static List<InputCmdInfoConfig> ParseCmd(string content, ref int giftCount, bool isGift)
    // {
    //     if (string.IsNullOrEmpty(content))
    //         return null;
    //     var lst = new List<InputCmdInfoConfig>() { };
    //     return lst;
    // }

    /// <summary>
    /// 根据价格寻找配置
    /// </summary>
    /// <param name="price"></param>
    /// <param name="isGift"></param>
    /// <returns></returns>
    // public static (Dictionary<InputCmdInfoConfig, int>, InputCmdInfoConfig)? ParsePrice(float price, bool isGift)
    // {
    //     return null;
    // }

    /// <summary>
    /// 根据ID寻找配置
    /// </summary>
    // public static List<InputCmdInfoConfig> ParseCmdID(string content, string uid, ref int giftCount, bool isGift)
    // {
    //     if (string.IsNullOrEmpty(content))
    //         return null;
    //
    //     //排除掉使用超过限制的
    //
    //     return new List<InputCmdInfoConfig>(){};
    // }

    #region 处理服务器返回

    private readonly Dictionary<string, int> _userQuantity = new();

    /// <summary>
    /// 执行指令
    /// </summary>
    /// <param name="userID">用户ID</param>
    /// <param name="inputCmdInfo">指令</param>
    /// <param name="count">次数</param>
    /// <param name="content">原指令</param>
    /// <param name="isDriver"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    // public void ReceiveCmd(string userID, InputCmdInfoConfig inputCmdInfo, int count, long timestamp,
    //     string content = "", bool isDriver = true)
    // {
    //
    //     //取出用户信息
    //     var hasInfo = GameData.TryGetUserInfo(userID, out var userinfo);
    //     if (!hasInfo)
    //     {
    //         Debug.LogError($"用户{userID}未加入游戏 无法获取到信息");
    //     }
    //
    //     if (string.IsNullOrEmpty(userID))
    //         return;
    //
    //     var dic = new NativeHashMap<int, int>(Enum.GetValues(typeof(ECmd)).Length, Allocator.Temp);
    //     var nativeNumDic = new NativeHashMap<int, int>(Enum.GetValues(typeof(ECmd)).Length, Allocator.Temp);
    //
    //     for (var i = 0; i < count; i++)
    //     {
    //         if (inputCmdInfo is { Hit: true })
    //         {
    //             var (cmd, quantity) = inputCmdInfo.GetHitInfo();
    //             var intCmd = (int)cmd;
    //             if (!dic.TryAdd(intCmd, quantity))
    //             {
    //                 dic[intCmd] += quantity;
    //             }
    //
    //             if (!nativeNumDic.TryAdd(intCmd, quantity))
    //             {
    //                 nativeNumDic[intCmd] += quantity;
    //             }
    //         }
    //     }
    //
    //     var multiples = inputCmdInfo.GetMultiples(count);
    //     var cmdArr = dic.GetKeyArray(Allocator.Temp);
    //
    //     foreach (var key in cmdArr)
    //     {
    //         UseCmd(key);
    //     }
    //
    //     return;
    //
    //     async void UseCmd(int key)
    //     {
    //         var cmd = (ECmd)key;
    //         var quantity = Mathf.CeilToInt(dic[key] * multiples);
    //         quantity = inputCmdInfo.TryMaxNum(userID, quantity);
    //         var nativeCount = nativeNumDic[key];
    //
    //         int start;
    //         int end;
    //         string name;
    //         switch (cmd)
    //         {
    //             case ECmd.抽奖:
    //                 //处于抽奖模式
    //                 break;
    //             case ECmd.修复汽车:
    //                 break;
    //             case ECmd.复活汽车:
    //                 break;
    //             case ECmd.定位:
    //                 break;
    //             case ECmd.加入队伍1:
    //             case ECmd.加入队伍2:
    //             case ECmd.加入队伍3:
    //             case ECmd.加入队伍4:
    //             case ECmd.加入队伍5:
    //             case ECmd.加入队伍6:
    //             case ECmd.加入队伍7:
    //             case ECmd.加入队伍8:
    //             case ECmd.加入队伍9:
    //             case ECmd.加入队伍10:
    //             case ECmd.加入队伍11:
    //             case ECmd.加入队伍12:
    //                 TryAddUser(userID, cmd);
    //                 break;
    //             case ECmd.更换飞机:
    //                 break;
    //             case ECmd.更换翅膀:
    //                 break;
    //             case ECmd.更换拖尾:
    //                 break;
    //             case ECmd.邀请同乘:
    //                 break;
    //             case ECmd.申请同乘:
    //                 break;
    //             default:
    //                 if (hasInfo)
    //                 {
    //                     UseProps(cmd, userinfo.ID, quantity, nativeCount, timestamp);
    //                 }
    //                 else
    //                 {
    //                     // if (GameData.HasUser(userID)) {
    //                     UseProps(cmd, -1, quantity, nativeCount, timestamp);
    //                     // }
    //                     // else {
    //                     //     Debug.LogError($"用户{userID}连信息都没有 无法获取到信息");
    //                     // }
    //                 }
    //
    //                 break;
    //         }
    //     }
    // }

    //补给功能
    private bool UseSupply(int userInfoID)
    {
        return false;
    }

    /// <summary>
    /// 处理聊天
    /// </summary>
    /// <param name="content">聊天信息</param>
    /// <param name="uid">用户ID</param>
    public void ChatMessage(string content, string uid, long timestamp)
    {
        // TODO
        EventsManager.BroadCast(GameEnum.ChatMessage, uid, content);
    }

    /// <summary>
    /// 处理礼物
    /// </summary>
    /// <param name="giftName">礼品名</param>
    /// <param name="giftId">礼品ID</param>
    /// <param name="giftCount">礼品数量</param>
    /// <param name="uid">用户ID</param>
    /// <param name="timestamp"></param>
    /// <param name="price"></param>
    public void GiftMessage(string giftName, string giftId, int giftCount, string uid, long timestamp,
        float price = 0)
    {
        // if (string.IsNullOrEmpty(giftName) && string.IsNullOrEmpty(giftId))
        // {
        //     Debug.Log($"礼物获取错误 giftName：{giftName} giftId：{giftId}");
        //     return;
        // }
        //
        // IEnumerable<InputCmdInfoConfig> cmdInfo = Array.Empty<InputCmdInfoConfig>();
        // if (price > 0)
        // {
        //
        //     //根据价格判断
        //     var valueTuple = ParsePrice(price, true);
        //     if (valueTuple == null) return;
        //     var (info, fast) = valueTuple.Value;
        //     if (info.Count > 0)
        //     {
        //         //记录礼物
        //         // if (info.TryGetValue(fast, out var count))
        //         //     GameData.AddInteractiveInfo(uid, fast.ID, count);
        //         // else
        //         //     Debug.LogError($"礼物记录错误！！！ {fast.ID} 不存在数量");
        //
        //         foreach (var (input, value) in info) ReceiveCmd(uid, input, value, timestamp);
        //     }
        // }
        // else
        // {
        //     //根据名称和ID判断
        //     if (!string.IsNullOrEmpty(giftName))
        //     {
        //         var info = ParseCmd(giftName, ref giftCount, true);
        //         if (info != null) cmdInfo = cmdInfo.Concat(info);
        //     }
        //
        //     if (!string.IsNullOrEmpty(giftId))
        //     {
        //         var info = ParseCmdID(giftId, uid, ref giftCount, true);
        //         if (info != null) cmdInfo = cmdInfo.Concat(info);
        //     }
        //
        //     //排重
        //     cmdInfo = cmdInfo.Distinct();
        //     var inputCmdInfos = cmdInfo.ToArray();
        //     if (inputCmdInfos.Length > 0)
        //     {
        //         var fast = inputCmdInfos[0];
        //
        //         //记录礼物
        //         // GameData.AddInteractiveInfo(uid, fast.ID, giftCount);
        //         foreach (var input in inputCmdInfos)
        //         {
        //             // TryAddUser(uid, autoJoinRoom: input.AutoJoinRoom);
        //             ReceiveCmd(uid, input, giftCount, timestamp);
        //         }
        //     }
        // }
        //
        // AddFirstReward(uid, timestamp);
    }

    /// <summary>
    /// 处理点赞
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="count"></param>
    public void LikeMessage(string uid, int count, long timestamp)
    {
        //记录点赞
        // GameData.AddInteractiveInfo(uid, InteractiveID.点赞, count);
        //
        // var cmdInfo = ParseCmd("点赞", ref count, false);
        // foreach (var input in cmdInfo)
        // {
        //     // TryAddUser(uid, autoJoinRoom: input.AutoJoinRoom || GameConfig.LikeAutoJoinRoom);
        //     for (var i = 0; i < count; i++)
        //     {
        //         ReceiveCmd(uid, input, 1, timestamp);
        //     }
        // }
    }

    #endregion

    #endregion

    #region 首充奖励

    private int _firstRewardNum;

    private readonly HashSet<string> _firstRewardOnce = new();

    /// <summary>
    /// 触发首送奖励
    /// </summary>
    /// <param name="uid"></param>
    private void AddFirstReward(string uid, long timestamp)
    {
        // TODO
        // EventsManager.BroadCast(GameEnum.CmdUpdateRewardNumber, _firstRewardNum);
    }

    #endregion
}