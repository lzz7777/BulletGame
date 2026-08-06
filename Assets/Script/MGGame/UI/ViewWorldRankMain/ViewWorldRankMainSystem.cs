using System;
using System.Collections.Generic;
using System.Linq;
using cfg;
using cfg.Rank;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace XN
{
public static class ViewWorldRankMainSystem
{
	#region CircleLife
    public static void OnOpenSystem(this ViewWorldRankMain self, UIWindowData uIWindowData)
    {
	    self.OpenType = uIWindowData.StringArgs1;
	    // self.currToggle = RankType.WeekRank;
	    // TODO  WorldRank or GameOver 刷新不同
        self.RefreshAll(self.OpenType);
    }
    
    public static void OnCloseSystem(this ViewWorldRankMain self)
    {
        
    }
	#endregion

    #region UIEvents

    public static void UIBtnBackMainButtonOnClick(this ViewWorldRankMain self)
    {
	    self.Close();
	    // UIManager.Instance.OpenWindow<ViewMain>().ToCoroutine();
    }
    
    public static void UIButtonSelectModeButtonOnClick(this ViewWorldRankMain self)
    {
	    // UI 
	    self.Close();
	    // Game
	    RoomManager.Instance.CloseGame();
    }

    public static void UIButtonOneMoreAgainButtonOnClick(this ViewWorldRankMain self)
    {
	    // UI
	    self.Close();
	    // 再来
	    RoomManager.Instance.OneMoreAgain().ToCoroutine();
    }
    
    public static void UIRankFamousTopButtonOnClick(this ViewWorldRankMain self)
    {
	    UIManager.Instance.OpenWindow<ViewFamousRankMain>();
    }
    
    public static void UIToggleDetailFansButtonOnClick(this ViewWorldRankMain self)
    {
	    UIManager.Instance.OpenWindow<ViewRankFanBadgePop>().ToCoroutine();
    }
    #endregion
    
    #region GlobalEvents
    #endregion
    
    #region Logics

    private static async UniTask RefreshAll(this ViewWorldRankMain self, string openType)
    {
	    var currChannel = TotalConfigManager.ConfigManager.ConstConfigCategory.CurrChannel;
	    var constCc = TotalConfigManager.ConfigManager.LoginInfoConfigCategory.GetOrDefault(currChannel);
	    self.UIWorldRankTitleTextMeshProUGUI.SetText(constCc.Name);
	    // Top3 名人堂
		self.RefreshTop3();
	    
	    // 局内Rank ; 7天/粉丝/月榜单
	    self.UIBtnBackMainButton.gameObject.SetActive(openType == "World2Rank");
	    self.UIButtonOneMoreAgainButton.gameObject.SetActive(openType == "Room2Rank");
	    self.UIButtonSelectModeButton.gameObject.SetActive(openType == "Room2Rank");
	    
	    // 等待一帧，确保 Toggle 的 SetActive 完成初始化
	    await UniTask.NextFrame();
	    self.RefreshScollToggle();
    }

    public static async UniTask RefreshTop3(this ViewWorldRankMain self)
    {
	    List<RankDataRet> datRankList = await DataManager.GetRankIndexInfo(RankType.HallOfFame, 0, 2);

	    Transform top3Parent = self.UIFamousTop3NodeHorizontalLayoutGroup.transform;
	    ObjectPoolManager.Instance.ReturnToPool(self.Top3Items);
	    self.Top3Items.Clear();
	    for (int i = 0; i < datRankList.Count; i++)
	    {
		    var onePlayerData = datRankList[i];
		    var obj = await ObjectPoolManager.Instance.GetFromPool<ItemRankTopPlayer>(top3Parent);
		    self.Top3Items.Add(obj);
		    var itemData = new ItemRankTopPlayerData()
		    {
			    Scale = 0.95f,
			    RankIndex = onePlayerData.Rank,
			    Name = onePlayerData.Nickname,
			    AvatarUrl = onePlayerData.AvatarUrl,
			    StarNum = (int)onePlayerData.Score,
		    };
		    obj.GetComponent<ItemRankTopPlayer>().OnRefresh(itemData);
	    }
    }
    
    /// <summary>
	/// 刷新世界排行里面各分页签内容
	/// </summary>
	/// <param name="self"></param>
    public static void RefreshScollToggle(this ViewWorldRankMain self)
    {
	    // 兼容处理当前分页签 按钮栏
	    self.RefreshToggleBar();
	    
	    // 模拟切页签 Toggle 显示选中 ToggleDetail
	    // self.UIWorldRankToggleOnChanged(self.currToggle, true);
    }

    // 确保开启Toggle , 触发后续刷新
    static void EnsureToggleOnWithEvent(Toggle t)
    {
	    if (t.isOn) t.onValueChanged.Invoke(true);
	    else t.isOn = true;
    }
    
    /// <summary>
    /// 获取当前分页签显示，用于兼容不同模式下应该显示的当前正确页签
    /// </summary>
    /// <param name="self"></param>
    public static void RefreshToggleBar(this ViewWorldRankMain self)
    {
	    // 本局里程碑没有表头
	    // 依次数据为：名次、昵称、总里程/本局增加里程、里程碑排名上升名次
	    RankInfoConfigCategory rankInfoCc = TotalConfigManager.ConfigManager.RankInfoConfigCategory;
	    // string rankName = "本局榜";
	    // int itemIdSortKey = 4;	// 里程
	    
	    // OpenType 决定显隐  本局跳入排行榜，本局榜显示
	    self.UIWorldCurRoomToggle.gameObject.SetActive(self.OpenType == "Room2Rank");
	    self.UIMaximumRangeToggle.gameObject.SetActive(self.OpenType == "World2Rank");
	    
	    // 矫正Toggle
	    switch (self.currToggle)
	    {
		    case RankType.None:	// 本局榜单 - 虚无缥缈占位一下
			    if (self.OpenType == "World2Rank")
			    {
				    self.currToggle = RankType.WeekRank;	// 世界排行点开，直接默认显示周排行
				    EnsureToggleOnWithEvent(self.UIWorldWeekToggle);
				    // self.UIWorldWeekToggle.isOn = true;
			    }
			    else if (self.OpenType == "Room2Rank")
			    {
				    self.UIWorldCurRoomToggle.GetComponentInChildren<Text>().text = "本局榜";
				    EnsureToggleOnWithEvent(self.UIWorldCurRoomToggle);
				    // self.UIWorldCurRoomToggle.isOn = true;
			    }
			    break;
		    default:
			    if (self.OpenType == "Room2Rank")
			    {
				    // self.UIWorldCurRoomToggle.isOn = true;
				    self.UIWorldCurRoomToggle.GetComponentInChildren<Text>().text = "本局榜";
				    EnsureToggleOnWithEvent(self.UIWorldCurRoomToggle);
			    }
			    break;
	    }

	    self.UIWorldWeekToggle.GetComponentInChildren<Text>().text = rankInfoCc.Get(RankType.WeekRank).RankName;
	    self.UIWorldFansToggle.GetComponentInChildren<Text>().text = rankInfoCc.Get(RankType.FansRank).RankName;
	    self.UIWorldMonthToggle.GetComponentInChildren<Text>().text = rankInfoCc.Get(RankType.MonthRank).RankName;

	    // self.RefreshToggleSubTitle(self.currToggle);	// isOn 应该去调整
    }
    
    // 1.排名，2.昵称，3.积分/公里/粉丝，4.按钮/粉丝/空/皮肤，皮肤/
    public static void ToggleDetail(this ViewWorldRankMain self, string t1, string t2, string t3, string t4 = "", string t5 = "")
    {
	    self.UIToggleDetailTitle1TextMeshProUGUI.SetText(t1);
	    self.UIToggleDetailTitle2TextMeshProUGUI.SetText(t2);
	    self.UIToggleDetailTitle3TextMeshProUGUI.SetText(t3);
	    self.UIToggleDetailTitle4TextMeshProUGUI.SetText(t4);
	    self.UIToggleDetailTitle5TextMeshProUGUI.SetText(t5);
    }

    public static void RefreshToggleSubTitle(this ViewWorldRankMain self, RankType rankType)
    {
	    RankInfoConfigCategory rankInfoCC = TotalConfigManager.ConfigManager.RankInfoConfigCategory;

	    // 粉丝勋章
	    self.UIToggleDetailFansButton.gameObject.SetActive(rankType == RankType.FansRank);
	    RankInfoConfig oneRankInfo;
	    switch (rankType)
	    {
		    case RankType.None:
			    // 积分排名
			    self.ToggleDetail("排名","昵称","积分","粉丝","周榜");
			    break;
		    case RankType.WeekRank:
			    // 积分
			    oneRankInfo = rankInfoCC.Get(RankType.WeekRank);
			    // 排名，昵称，积分，粉丝，皮肤Img
			    // self.ToggleDetail(oneRankInfo.GradeName[0],oneRankInfo.GradeName[1],oneRankInfo.GradeName[2],oneRankInfo.GradeName[3],oneRankInfo.GradeName[4]);
			    self.ToggleDetail(oneRankInfo.GradeName[0],oneRankInfo.GradeName[1],oneRankInfo.GradeName[2],string.Empty,oneRankInfo.GradeName[4]);
			    break;
		    case RankType.FansRank:
			    oneRankInfo = rankInfoCC.Get(RankType.FansRank);
			    // 
			    self.ToggleDetail(oneRankInfo.GradeName[0],oneRankInfo.GradeName[1],oneRankInfo.GradeName[2],"勋章",oneRankInfo.GradeName[3]);
			    break;
		    case RankType.MonthRank:
			    oneRankInfo = rankInfoCC.Get(RankType.MonthRank);
			    // 排名 昵称 积分
			    self.ToggleDetail(oneRankInfo.GradeName[0],oneRankInfo.GradeName[1],oneRankInfo.GradeName[2]);
			    break;
		    case RankType.KillRank:
			    oneRankInfo = rankInfoCC.Get(RankType.KillRank);
			    self.ToggleDetail(oneRankInfo.GradeName[0],oneRankInfo.GradeName[1],oneRankInfo.GradeName[2]);
			    break;
		    default:
			    throw new ArgumentOutOfRangeException(nameof(rankType), rankType, null);
	    }
    }
    
    public static void UIWorldRankToggleOnChanged(this ViewWorldRankMain self, RankType rankType, bool isOn)
    {
	    Debug.Log($" toggle rankType: {rankType} / {isOn}  {self.OpenType}");
	    if(!isOn || string.IsNullOrEmpty(self.OpenType)) return;

	    self.RefreshToggleSubTitle(rankType);

	    // 兼容分页签下拉取Player List
	    self.currToggle = rankType;
	    self.RefreshScrollPlayerItems(rankType).ToCoroutine();
    }
    
    public static async UniTask RefreshScrollPlayerItems(this ViewWorldRankMain self, RankType currRankType)
    {
	    var RankRewardCc = TotalConfigManager.ConfigManager.RankRewardConfigCategory;
	    List<RankDataRet> DatRankList;	// 后续服务器拉取消息
	    string[] playerIds ;
	    
	    int num;
	    // 世界排名 N * Player
	    if (currRankType == RankType.None)
	    {
		    // 本局榜 按照积分排序
		    // string[] playerIdsOld = RoomHelper.GetPlayers()
			   //  .Select(kv => new {
				  //   PlayerId = kv.Key,
				  //   Info = EntityManager.Instance.GetEntityById(kv.Value)?.GetComponent<PlayerInfoComponent>(),
				  //   Item = EntityManager.Instance.GetEntityById(kv.Value)?.GetComponent<PlayerItemComponent>()
			   //  })
			   //  .OrderByDescending(x => ( x.Info?.WinScore) ?? 0)	// x.Item?.GetItemNum(GameConst.ScoreId) + x.Info?.WinScore
			   //  .Select(x => x.PlayerId)
			   //  .ToArray();

		    playerIds = RoomHelper.GetPlayerIdsInCar()
			    .Select(playerId => RoomHelper.GetRoomInfoComponent().GetPlayerInfoComponent(playerId))
			    .Where(playerInfoComp => playerInfoComp != null)  // 过滤 null
			    .OrderByDescending(playerInfoComp => playerInfoComp.WinScore)
			    .Select(comp => comp.PlayerId)
			    .ToArray();
		    
		    DatRankList = await DataManager.GetRankIndexInfo(RankType.WeekRank, playerIds);	// 服务器 周榜排名
	    }
	    else
	    {
		    DatRankList = await DataManager.GetRankIndexInfo(self.currToggle, 0,99);
		    playerIds = DatRankList.Select(i => i.PlayerId).ToArray();
	    }
	    num = DatRankList.Count;

	    Transform nPlayerParent = self.UIScrollContentVerticalLayoutGroup.transform;
	    ObjectPoolManager.Instance.ReturnToPool(self.RankListItems);
	    self.RankListItems.Clear();

	    for (int i = 0; i < num; i++)
	    {
		    int Index = i + 1; // 后续有异步await
		    var obj = await ObjectPoolManager.Instance.GetFromPool<ItemRankOnePlayer>(nPlayerParent);
		    self.RankListItems.Add(obj);
		    var onePlayerData = DatRankList.Find(x => x.PlayerId == playerIds[i]);
		    var itemData = new ItemRankOnePlayerData()
		    {
			    RankType = currRankType,
			    PlayerId = onePlayerData.PlayerId,
			    Name = onePlayerData.Nickname,
			    AvatarUrl = ResHelper.GetAvatarUrl(onePlayerData.AvatarUrl),
			    Index = Index,
		    };

		    switch (currRankType)
		    {
			    case RankType.None:
				    if (RoomHelper.GetPlayers().TryGetValue(onePlayerData.PlayerId, out long playerInstId))
				    {
					    var playerUnit = EntityManager.Instance.GetEntityById(playerInstId);
					    var playerInfoComp = playerUnit.GetComponent<PlayerInfoComponent>();
					    var playerItemComp = playerUnit.GetComponent<PlayerItemComponent>();
					    
					    // 本局榜单， 不依赖排行榜
					    itemData.Name = playerInfoComp.Name;
					    itemData.AvatarUrl = playerInfoComp.AvatarUrl;
					    // itemData.OwnScore = playerItemComp.GetItemNum(GameConst.ScoreId);
					    Debug.Log($"PlayerId: {onePlayerData.PlayerId} [score]  开始：{playerItemComp.GetItemNum(GameConst.ScoreId)} ===> 结束：{onePlayerData.Score}");
					    itemData.OwnScore = onePlayerData.Score;
					    itemData.WinScore = playerInfoComp.WinScore;
					    itemData.OwnFans = playerItemComp.GetItemNum(GameConst.FansId);
					    itemData.WinFans = playerInfoComp.WinFans;
					    itemData.FansIsMin = playerInfoComp.IsBaoDiFans;
					    itemData.WeekRankIndex = onePlayerData.Rank;
					    itemData.RankNode = RoomManager.Instance.GetPlayerRank(RankType.WeekRank, onePlayerData.PlayerId);
				    }
				    break;
			    case RankType.WeekRank:
				    itemData.OwnScore = Convert.ToSingle(onePlayerData.Score);
				    itemData.OwnFans = Convert.ToSingle(onePlayerData.Fans);
				    var oneRankRwd = RankRewardCc.GetOrDefault(onePlayerData.Rank);
				    itemData.Text5 = oneRankRwd?.WeekRankRewardShow;
				    break;
			    case RankType.FansRank:
				    var onePlayerDataFans = DatRankList.Find(x => x.PlayerId == playerIds[i]);
				    float? rankRewardFansAddPct = RankRewardCc.Get(Index)?.FansRankPointAdd;
				    itemData.OwnFans = Convert.ToSingle(onePlayerDataFans.Fans);
				    itemData.Text5 = $"+{rankRewardFansAddPct * 100}%";
				    break;
			    case RankType.MonthRank:
				    var onePlayerDataMonth = DatRankList.Find(x => x.PlayerId == playerIds[i]);
				    itemData.OwnScore = Convert.ToSingle(onePlayerDataMonth.Score);
				    break;
			    case RankType.KillRank:
				    var onePlayerDataKill = DatRankList.Find(x => x.PlayerId == playerIds[i]);
				    itemData.KillCount = Convert.ToSingle(onePlayerDataKill.Score);
				    break;
		    }
		    
		    obj.GetComponent<ItemRankOnePlayer>().OnRefresh(itemData);
	    }
	    
	    switch (currRankType)
	    {
		    case RankType.None:
			    self.UIRankDateTextMeshProUGUI.SetText("");
			    break;
		    case RankType.FansRank:
			    self.UIRankDateTextMeshProUGUI.SetText("粉丝月底百分百继承");
			    break;
		    case RankType.WeekRank:
		    case RankType.MonthRank:
			    var rankInfo = SceneHelper.GetRankUnit().GetComponent<RankInfoComponent>().RankInfo;
			    if (rankInfo.TryGetValue(currRankType, out var timeData))
			    {
				    long time = timeData.EndTime;
				    var dateTime = TimeHelper.Time2DateTimeMs(time);
				    string formattedTime = dateTime.ToString("yyyy/M/d/ HH:mm:ss");
				    self.UIRankDateTextMeshProUGUI.SetText($"本期榜单截止时间：{formattedTime}");
			    }
			    else
			    {
				    self.UIRankDateTextMeshProUGUI.SetText($"本期榜单截止时间:");
			    }
			    break;
	    }
    }
    
    #endregion
}
}
