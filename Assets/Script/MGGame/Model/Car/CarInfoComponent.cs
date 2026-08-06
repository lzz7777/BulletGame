using System;
using System.Collections.Generic;
using cfg;

namespace XN
{
    [Flags]
    public enum CarMoveType
    {
        Normal,
        MoveX = 1 << Normal,
        MoveY = 1 << MoveX,
    }

    public class CarInfoComponent : IComponent
    {
        /// <summary>
        /// 里程
        /// </summary>
        public float Mileage { get; set; }
        public string Name { get; set; }
        public float Speed { get; set; }
        public float ExtraSpeedVale { get; set; }
        public float ExtraSpeedPct { get; set; }

        /// <summary>
        /// 盾
        /// </summary>
        public float Shield { get; set; }
        
        /// <summary>
        /// 车辆默认载具
        /// </summary>
        public int DeviceId;
        
        /// <summary>
        /// 当前组
        /// </summary>
        public int Group { get; set; }
        
        /// <summary>
        /// 当前轨道
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// 切换轨道延时
        /// </summary>
        public int ChangeLineDelay { get; set; }
        
        /// <summary>
        /// 切换轨道计时
        /// </summary>
        public float ChangeLineTime { get; set; }

        /// <summary>
        /// 移动状态
        /// </summary>
        public CarMoveType CarMoveType { get; set; }

        /// <summary>
        /// 状态组
        /// </summary>
        public Dictionary<State, (float, float)> StateDic { get; set; } = new();

        /// <summary>
        /// 临时状态
        /// </summary>
        public State CarState { get; set; } = State.None;

        /// <summary>
        /// 状态机
        /// </summary>
        public CarStateMachine CarStateMachine { get; set; } = new();
        
        /// <summary>
        /// 成员
        /// </summary>
        public List<string> PlayerIds { get; set; } = new();
        
        /// <summary>
        /// 特效组
        /// </summary>
        public Dictionary<int, int> EffectGroup { get; set; } = new();
        
        /// <summary>
        /// 是否丢弃
        /// </summary>
        public bool IsDiscard { get; set; }
        
        public override void OnCreate()
        {
            for (int i = 0; i < Enum.GetValues(typeof(State)).Length; i++)
            {
                StateDic[(State)i] = (0,0);
            }
        }

        public override void OnDestroy()
        {
        }
    }
}