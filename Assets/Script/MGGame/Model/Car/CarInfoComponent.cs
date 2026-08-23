using System;
using System.Collections.Generic;
using cfg;
using UnityEngine;

namespace XN
{
    [Flags]
    public enum CarMoveType
    {
        Normal,
        MoveX = 1 << Normal,
        MoveY = 1 << MoveX,
    }
    
    // 新增：0 GC 的 State 枚举比较器
    public class StateEqualityComparer : IEqualityComparer<State>
    {
        public bool Equals(State x, State y)
        {
            return x == y;// 直接按底层 int 值比较
        }

        public int GetHashCode(State obj)
        {
            return (int)obj;// 直接返回底层 int 作为 HashCode
        }
    }

    public class CarInfoComponent : IComponent
    {
        /// <summary>
        /// 里程
        /// </summary>
        [SerializeField]
        public float Mileage { get; set; }
        
        [SerializeField]
        public string Name { get; set; }
        
        [SerializeField]
        public float Speed { get; set; }
        
        [SerializeField]
        public float ExtraSpeedVale { get; set; }
        
        [SerializeField]
        public float ExtraSpeedPct { get; set; }

        /// <summary>
        /// 盾
        /// </summary>
        [SerializeField]
        public float Shield { get; set; }
        
        /// <summary>
        /// 车辆默认载具
        /// </summary>
        [SerializeField]
        public int DeviceId { get; set; }
        
        /// <summary>
        /// 当前组
        /// </summary>
        [SerializeField]
        public int Group { get; set; }
        
        /// <summary>
        /// 当前轨道
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// 切换轨道延时
        /// </summary>
        [SerializeField]
        public int ChangeLineDelay { get; set; }
        
        /// <summary>
        /// 切换轨道计时
        /// </summary>
        [SerializeField]
        public float ChangeLineTime { get; set; }

        /// <summary>
        /// 切换组别计时
        /// </summary>
        [SerializeField]
        public float ChangeGroupTime { get; set; }

        /// <summary>
        /// 移动状态
        /// </summary>
        [SerializeField]
        public CarMoveType CarMoveType { get; set; }

        /// <summary>
        /// 状态组
        /// </summary>
        // 传入自定义比较器，避免底层默认比较器产生的装箱 GC
        [SerializeField]
        public Dictionary<State, (float, float)> StateDic { get; set; } = new(new StateEqualityComparer());

        /// <summary>
        /// 临时状态
        /// </summary>
        [SerializeField]
        public State CarState { get; set; } = State.None;

        /// <summary>
        /// 状态机
        /// </summary>
        [SerializeField]
        public CarStateMachine CarStateMachine { get; set; } = new();
        
        /// <summary>
        /// 成员
        /// </summary>
        [SerializeField]
        public List<string> PlayerIds { get; set; } = new();
        
        /// <summary>
        /// 特效组
        /// </summary>
        [SerializeField]
        public Dictionary<int, int> EffectGroup { get; set; } = new();
        
        /// <summary>
        /// 是否丢弃
        /// </summary>
        [SerializeField]
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
            Mileage = default;
            Name = default;
            Speed = default;
            ExtraSpeedVale = default;
            ExtraSpeedPct = default;
            Shield = default;
            DeviceId = default;
            Group = default;
            Line = default;
            ChangeLineDelay = default;
            ChangeLineTime = default;
            ChangeGroupTime = default;
            CarMoveType = default;
            StateDic.Clear();
            CarState = default;
            CarStateMachine = new();
            PlayerIds.Clear();
            EffectGroup.Clear();
            IsDiscard = default;
        }
    }
}