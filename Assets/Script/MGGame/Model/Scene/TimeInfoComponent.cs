using System;

namespace XN
{
    public class TimeInfoComponent : ComponentBase
    {
        public long ServerId;
        public long ServerTimeAndLocalOffset;
        public const int TimeZone = 8;
        public DateTime DateTime1970 = new(1970, 1, 1, TimeZone, 0, 0);
        public long ServerClientTime;
        
        public override void OnCreate()
        {
        }

        public override void OnDestroy()
        {
        }
    }
}