// ******************************************************************
// @file       GameStateCtrl.cs
// @brief      游戏状态控制
// @author     SamuelZon, zonsamuel@gmail.com
//             
// @Modified   2023-09-01
// @Copyright  Copyright (c) 2023, BarrageKnight
// ******************************************************************

using GameMain;

public enum GameState
{
    未进入游戏 = 0,
    加载完毕 = 10,

    /// <summary>
    /// 等待玩家进入等阶段
    /// </summary>
    等待玩家 = 20,
    第一个玩家进入游戏 = 21,

    /// <summary>
    /// 加速阶段
    /// 此阶段加速
    /// </summary>
    起步阶段 = 30,

    /// <summary>
    /// 特殊阶段,
    /// 镜头切换成独立角色前
    /// </summary>
    加速阶段 = 40,
    游戏中 = 50,
    开始显示终点 = 60,
    游戏倒计时 = 81,
    游戏结束 = 90
}

public class GameStateCtrl
{
    /// <summary>
    /// 游戏状态
    /// </summary>
    public static GameState State { get; private set; } = GameState.未进入游戏;

    /// <summary>
    /// 所有游戏中的状态
    /// </summary>
    public static bool IsGameAllState { get; private set; }

    /// <summary>
    /// 等待阶段
    /// </summary>
    public static bool IsGameWait { get; private set; }

    /// <summary>
    /// 游戏开始前的状态
    /// </summary>
    public static bool IsGameFast { get; private set; }

    /// <summary>
    /// 从加速开始的游戏中阶段
    /// </summary>
    public static bool IsGaming { get; private set; }


    public static bool IsOverTime { get; private set; }

    public static void UpdateStateJudge(GameState state)
    {
        if (state == State) return;
        UpdateState(state);
    }

    /// <summary>
    /// 更新游戏状态
    /// </summary>
    /// <param name="state"></param>
    public static void UpdateState(GameState state)
    {
        if (state < State)
        {
            //大部分情况不允许回退状态
            if (state != GameState.等待玩家)
            {
                return;
            }
        }

        State = state;
        Debug.Log("切换状态为 : " + state);
        IsGameAllState = State is >= GameState.等待玩家 and <= GameState.游戏倒计时;
        IsGameWait = State is >= GameState.等待玩家 and < GameState.起步阶段;
        IsGameFast = State is >= GameState.等待玩家 and <= GameState.加速阶段;
        IsGaming = State is >= GameState.加速阶段 and <= GameState.游戏倒计时;
        IsOverTime = State is GameState.游戏倒计时;


        // SoundManager.Instance.PlayMusic(State);
        EventsManager.BroadCast(GameEnum.UpdateGameState);
    }
}