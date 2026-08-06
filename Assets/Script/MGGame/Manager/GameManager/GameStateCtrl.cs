// ******************************************************************
// @file       GameStateCtrl.cs
// @brief      游戏状态控制
// @author     SamuelZon, zonsamuel@gmail.com
//             
// @Modified   2023-09-01
// @Copyright  Copyright (c) 2023, BarrageKnight
// ******************************************************************

namespace XN
{
    public enum MGGameState
    {
        未进入游戏 = 0,
        进入房间 = 10,

        加载完毕 = 20,

        /// <summary>
        /// 等待阶段
        /// </summary>
        等待玩家 = 30,
        第一个玩家进入游戏 = 31,

        游戏开始 = 40,
        起步阶段 = 41,
        游戏中 = 50,
        开始显示终点 = 60,
        游戏倒计时 = 70,
        到达终点 = 80,
        游戏结束 = 90,
    }

    public class GameStateCtrl
    {
        /// <summary>
        /// 游戏状态
        /// </summary>
        public static MGGameState State { get; private set; } = MGGameState.未进入游戏;

        /// <summary>
        /// 所有游戏中的状态
        /// </summary>
        public static bool IsGameAllState { get; private set; }
        
        /// <summary>
        /// 等待阶段
        /// </summary>
        public static bool IsGameWait { get; private set; }

        public static bool IsGameStart { get; private set; }
        
        /// <summary>
        /// 游戏开始后准备阶段
        /// </summary>
        public static bool IsGamePrepare { get; private set; }

        /// <summary>
        /// 从加速开始的游戏中阶段
        /// </summary>
        public static bool IsGaming { get; private set; }

        public static bool IsGameEnd { get; private set; }

        public static void UpdateStateJudge(MGGameState state)
        {
            if (state == State) return;
            UpdateState(state);
        }

        /// <summary>
        /// 更新游戏状态
        /// </summary>
        /// <param name="state"></param>
        /// <param name="forcedEntry">强制切换 一般用于重来/换模式</param>
        public static void UpdateState(MGGameState state, bool forcedEntry = false)
        {
            if (state < State && !forcedEntry)
            {
                //大部分情况不允许回退状态
                if (state != MGGameState.等待玩家)
                {
                    return;
                }
            }

            State = state;
            Debug.Log("切换状态为 : " + state);
            IsGameAllState = State is >= MGGameState.进入房间 and <= MGGameState.游戏倒计时;
            IsGameWait = State is >= MGGameState.等待玩家 and < MGGameState.游戏开始;
            IsGameStart = State >= MGGameState.游戏开始;
            IsGamePrepare = State is >= MGGameState.游戏开始 and <= MGGameState.起步阶段;
            IsGaming = State is >= MGGameState.游戏开始 and < MGGameState.到达终点;
            IsGameEnd = State is MGGameState.游戏结束;

            // SoundManager.Instance.PlayMusic(State);
            EventsManager.BroadCast(GameEnum.UpdateGameState);
        }
    }
}