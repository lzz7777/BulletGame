using System.Collections.Generic;

namespace ByteDance.LiveOpenSdk.Perf
{
    internal static class ItemPool
    {
        private static readonly List<FrameInfo> FramePool = new();
        private static readonly List<EventInfo> EventPool = new();

        public static FrameInfo GetFrameInfo()
        {
            lock (FramePool)
            {
                if (FramePool.Count > 0)
                {
                    var frameInfo = FramePool[0];
                    FramePool.RemoveAt(0);
                    return frameInfo;
                }
            }

            return new FrameInfo();
        }

        public static EventInfo GetEventInfo()
        {
            lock (EventPool)
            {
                if (EventPool.Count > 0)
                {
                    var eventInfo = EventPool[0];
                    EventPool.RemoveAt(0);
                    return eventInfo;
                }
            }
            return new EventInfo();
        }

        public static void Release(FrameInfo frameInfo)
        {
            lock (FramePool)
            {
                FramePool.Add(frameInfo);
            }
        }

        public static void Release(EventInfo eventInfo)
        {
            lock (EventPool)
            {
                EventPool.Add(eventInfo);
            }
        }
    }
}