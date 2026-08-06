using cfg;

namespace XN
{
public class ItemRankOnePlayerData
{
    public RankType RankType;
    // 文本前两段就是  排名，头像，昵称
    public int Index;
    public string PlayerId;
    public string Name;
    public string AvatarUrl;
    public bool IsShowFrame = false;

    // 内容差异
    public double OwnScore;
    public double WinScore;
    public double OwnFans;
    public double WinFans;
    public double Mileage;
    public bool FansIsMin = false;
    public double KillCount;
    
    // 周排名
    public int WeekRankIndex;
    public RankNode RankNode;
    // 五号位 积分加成/皮肤路径
    public string Text5;
    
    // 上期榜单 （皮肤，粉丝）
    public string RewardsShow;
    public ItemGroup FansItemGroup;
}
}
