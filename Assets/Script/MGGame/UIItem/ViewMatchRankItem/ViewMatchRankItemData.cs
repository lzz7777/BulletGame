using System.Collections.Generic;

namespace XN
{
    public class ViewMatchRankItemData : UIItemDataBase
    {
        public long CarId { get; set; }
        public int Rank { get; set; }
        public List<string> PlayerIds = new();
    }
}