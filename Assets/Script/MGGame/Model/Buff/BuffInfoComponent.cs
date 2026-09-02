using System.Collections.Generic;
using cfg;

namespace XN
{
    public class FunctionData
    {
        /// <summary>
        /// 时间队列
        /// </summary>
        public Queue<float> TimeQueue = new();

        public int FunctionId;
        public int GroupId;
    }

    public class BuffInfoComponent : ComponentBase
    {
        public int BuffId { get; set; }
        public float EndTime { get; set; }
        public float Time { get; set; }
        public string PlayerId { get; set; }
        
        public bool IsDiscard { get; set; }
        
        /// <summary>
        /// 方法数据
        /// </summary>
        public List<FunctionData> Functions { get; set; } = new();

        /// <summary>
        /// 生效参数
        /// </summary>
        public List<BuffChange> Mutexes { get; set; } = new();
        
        /// <summary>
        /// 替换特效组 (DeviceId, (EffectId, EffectSkin))
        /// </summary>
        public Dictionary<int, Dictionary<int, int>> EffectDeviceGroup = new();
        
        public override void OnCreate()
        {
        }

        public override void OnDestroy()
        {
            this.OnDestroySystem();
            
            BuffId = default;
            EndTime = default;
            Time = default;
            PlayerId = default;
            IsDiscard = default;
            Functions.Clear();
            Mutexes.Clear();
            EffectDeviceGroup.Clear();
        }
    }
}