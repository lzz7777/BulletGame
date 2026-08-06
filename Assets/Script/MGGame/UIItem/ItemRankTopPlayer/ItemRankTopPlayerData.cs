using System;

namespace XN
{
public class ItemRankTopPlayerData
{
    public int RankIndex;
    public string Name;
    public string AvatarUrl;
    public float Scale=1.0f;
    public bool IsShowFrame = false;
    public string HeadFrame;
    public int StarNum;
    public Action<string> OnClick;
}
}
