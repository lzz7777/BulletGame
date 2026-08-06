using System.Collections.Generic;

namespace XN
{
    public class PlayerItemComponent : IComponent
    {
        public Dictionary<long, BagData> BagDataDict { get; set; } = new();
        
        public override void OnCreate()
        {
        }

        public override void OnDestroy()
        {
        }
    }
}