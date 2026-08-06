using UnityEngine;

namespace XN
{
    public class ViewHeadItemData
    {
        public string PlayerId; // 通过只穿这个获取
        public string NickName;
        public string AvatarUrl;
        public string Frame;
        public int SortingOrder;

        public Vector2 SizeData = Vector2.zero;
    }
}