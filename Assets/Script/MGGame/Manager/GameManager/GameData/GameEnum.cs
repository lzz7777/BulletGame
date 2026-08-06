namespace XN
{
    public enum GameEnum
    {
        /// <summary>
        /// 游戏阶段更新
        /// </summary>
        UpdateGameState,

        /// <summary>
        /// 进入房间
        /// </summary>
        EnterRoom,

        /// <summary>
        /// 房间结束
        /// </summary>
        EndRoom,

        /// <summary>
        /// 玩家加入车队
        /// </summary>
        PlayerJoinCar,

        /// <summary>
        /// 车队更换排名
        /// </summary>
        CarChangeGroup,

        /// <summary>
        /// 局内界面刷新
        /// </summary>
        ViewBattleMainRefreshEvent,

        /// <summary>
        /// 局内界面道具动画
        /// </summary>
        ViewBattleMainItemShowEvent,

        /// <summary>
        /// 局内界面入场动画
        /// </summary>
        ViewBattleMainEntranceShowEvent,

        /// <summary>
        /// 局内结束，终止UI ing的展示
        /// </summary>
        ViewBattleMainGameOver,

        /// <summary>
        /// 排名节点刷新
        /// </summary>
        ViewMatchRankNodeRefreshEvent,

        /// <summary>
        /// 顶部功能按钮，部分界面的刷新事件
        /// </summary>
        TopSettingRefreshEvent,

        /// <summary>
        /// 车辆里程减少事件
        /// </summary>
        CarMileageDelEvent,
        
        /// <summary>
        /// 更新房间信息事件
        /// </summary>
        UpdateSceneInfo,
        
        /// <summary>
        /// 组刷礼物事件
        /// </summary>
        GroupBrushGifts,
        
        /// <summary>
        /// 玩家退出车队
        /// </summary>
        PlayerExitCar,
        
        /// <summary>
        /// 秒榜显示事件
        /// </summary>
        ViewMaximumRangeNodeShowEvent,
        
        /// <summary>
        /// 更新实体组件数据显示
        /// </summary>
        UpdateEntityViewerEvent,
    }
}