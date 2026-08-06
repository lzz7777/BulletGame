namespace GameMain
{
    public enum GameEnum
    {
        /// <summary>
        /// 游戏阶段更新
        /// </summary>
        UpdateGameState,

        /// <summary>
        /// 用户进入成功后
        /// 创建飞机
        /// </summary>
        CmdCreateAirplane,

        /// <summary>
        /// 更换组
        /// </summary>
        CmdUpdateAirplaneGroup,

        /// <summary>
        /// 更换飞机模型
        /// </summary>
        CmdChangeSkin,
        CmdChangeWing,
        CmdAddBuff,
        CmdAddBuffOver,
        CmdAddAttack,
        CmdAddAttackOver,

        /// <summary>
        /// 修复汽车
        /// </summary>
        CmdRestoringCar,

        /// <summary>
        /// buff通知
        /// </summary>
        AddBuffEvent,
        ReviseBuffEvent,
        LevelUpBuffEvent,
        RemoveBuffEvent,

        /// <summary>
        /// 添加道具
        /// </summary>
        AddPropEvent,
        RemovePropEvent,

        /// <summary>
        /// 定位飞机
        /// </summary>
        CmdLocation,

        /// <summary>
        /// 点赞
        /// </summary>
        Like,

        /// <summary>
        /// 礼物更新了
        /// </summary>
        UpdateInteractiveInfo,

        /// <summary>
        /// 更新首充
        /// </summary>
        CmdUpdateRewardNumber,

        /// <summary>
        /// 选中玩家
        /// </summary>
        SelectPlayer,

        /// <summary>
        /// 更换了圈数
        /// </summary>
        UpdateThrustIndex,

        /// <summary>
        /// 通知同乘成功
        /// </summary>
        CmdUpdateDriver,

        /// <summary>
        /// 修改了路径长度
        /// </summary>
        UpdatePath,

        /// <summary>
        /// 开始抽奖
        /// </summary>
        CallLottery,
        CallLotteryPrice,

        /// <summary>
        /// 发送聊天信息
        /// </summary>
        ChatMessage,

        /// <summary>
        /// 更新组头排的信息
        /// </summary>
        UpdateGroupTop,
        UpdateGroupUsers,
        AddStreak,
        AddScore,
    }
}