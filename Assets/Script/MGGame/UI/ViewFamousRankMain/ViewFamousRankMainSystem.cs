using System.Collections.Generic;
using System.Linq;
using cfg;
using cfg.Rank;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XN
{
public static class ViewFamousRankMainSystem
{
	#region CircleLife
    public static void OnOpenSystem(this ViewFamousRankMain self, UIWindowData uIWindowData)
    {
	    // Title
	    
	    // 初始化 根据传参，只显示一个
	    self.UITgFamousToggle.isOn = true;
	    self.UITgFamousToggleOnValueChanged(true);
	    self.UITgRuleToggleOnValueChanged(false);
    }
    
    public static void OnCloseSystem(this ViewFamousRankMain self)
    {
        
    }
	#endregion

    #region UIEvents

    public static void UITgFamousToggleOnValueChanged(this ViewFamousRankMain self, bool value)
    {
	    Debug.Log("UITgFamousToggleOnValueChanged : " + value);
	    self.UIScrollViewScrollRect.gameObject.SetActive(value);

	    // 显示刷新
	    if (value)
	    {
		    // Top1 名人堂 + Grid 5*6 ...
		    self.RefreshFamousGrid().ToCoroutine();
	    }
    }
    
    public static void UITgRuleToggleOnValueChanged(this ViewFamousRankMain self, bool value)
    {
	    Debug.Log("UITgRuleToggleOnValueChanged : " + value);
	    self.UIRulePanelImage.gameObject.SetActive(value);
	    RankRewardConfigCategory rankRewardCc = TotalConfigManager.ConfigManager.RankRewardConfigCategory;
	    if (value)
	    {
		    // 七天榜 前三
		    self.UITop3TitleTextMeshProUGUI.SetText(
			    string.Format("<size=40>{0}</size>\n<size=26>{1}</size>", "七天榜", "每周一0点结算")
			    );
		    
		    List<List<ItemGroup>> weekRewards = rankRewardCc.DataList
			    .Where(x => x.RewardId <= 3)
			    .Select(x => x.WeekRankReward ).ToList();
		    self.RefreshContentStar(self.UITop3ContentVerticalLayoutGroup.transform, self.RuleTop3List, weekRewards).ToCoroutine();
		    
		    // 总里程榜 
		    self.UITop5TitleTextMeshProUGUI.SetText(
			    string.Format("<size=40>{0}</size>\n<size=26>{1}</size>", "里程榜", "每双周一0点结算")
			    );
		    
		    List<List<ItemGroup>> totalMileRwds = rankRewardCc.DataList
			    .Where(x => x.RewardId <= 5)
			    .Select(x => x.MilestoneReward ).ToList();
		    self.RefreshContentStar(self.UITop5ContentVerticalLayoutGroup.transform, self.RuleTop5List, totalMileRwds).ToCoroutine();

		    // 月榜单 前十
		    self.UITop10TitleTextMeshProUGUI.SetText(
			    string.Format("<size=40>{0}</size>\n<size=26>{1}</size>", "月度榜", "每月1号0点结算")
		    );
		    
		    List<List<ItemGroup>> monthRwds = rankRewardCc.DataList
			    .Where(x => x.RewardId <= 10)
			    .Select(x => x.MonthRankReward ).ToList();
		    Debug.Log(monthRwds.Count);
		    self.RefreshContentStar(self.UITop10ContentVerticalLayoutGroup.transform, self.RuleTop10List, monthRwds).ToCoroutine();
			
		    // Tips
		    self.UIStarRuleTipsTextMeshProUGUI.SetText("Tips:榜单结算时发放上一期<sprite=3>奖励");
	    }
    }

    public static void UIBgButtonOnClick(this ViewFamousRankMain self)
    {
	    self.Close();
    }

    public static async UniTask RefreshFamousGrid(this ViewFamousRankMain self)
    {
	    List<RankDataRet> DatRankList = await DataManager.GetRankIndexInfo(RankType.HallOfFame, 0,99);
	    int num = DatRankList.Count;
	    Transform parent = self.UISVContentGridLayoutGroup.transform;
	    ObjectPoolManager.Instance.ReturnToPool(self.RankListItems);
	    self.RankListItems.Clear();
	    Debug.Log($"Famous : " + num);
	    for (int i = 0; i < num; i++)
	    {
		    var onePlayerData = DatRankList[i];
		    int starNum = (int)onePlayerData.Score;

		    if (starNum > 0)
		    {
			    var obj = await ObjectPoolManager.Instance.GetFromPool("ItemRankTopPlayer", parent);
			    self.RankListItems.Add(obj);
			    var itemData = new ItemRankTopPlayerData()
			    {
				    RankIndex = onePlayerData.Rank,
				    Name = onePlayerData.Nickname,
				    // IsShowFrame = true,
				    Scale = 0.88f,
				    AvatarUrl = onePlayerData.AvatarUrl,
				    StarNum = starNum,
				    OnClick = (string str) =>
				    {
					    Debug.Log($"OnClick: " + onePlayerData.PlayerId);
				    }
			    };
			    obj.GetComponent<ItemRankTopPlayer>().OnRefresh(itemData);
		    }
	    }
	    
	    
	    // Top1
	    var Top1PlayerData = DatRankList[0];
	    self.UIOneTitleTextMeshProUGUI.SetText("荣耀车神");
		// self.UINameTextMeshProUGUI.SetText(Top1PlayerData.Nickname);
		self.UINameText.text = Top1PlayerData.Nickname;

		self.ItemStarList.SetStarNum((int)Top1PlayerData.Score);
		YooAssetManager.Instance.LoadSpriteAsync(ResHelper.GetAvatarUrl(Top1PlayerData.AvatarUrl), self.UIHeadIconImage).ToCoroutine();
		string frame = RankHelper.GetHallOfFameFrameResByIndex(1);
		YooAssetManager.Instance.LoadSpriteAsync(frame, self.UIHeadFrameImage).ToCoroutine();

    }
    
    #endregion
    
    #region GlobalEvents
    #endregion
    
    #region Logics

    public static async UniTask RefreshContentStar(this ViewFamousRankMain self, Transform parent, List<GameObject> dataList,List<List<ItemGroup>> rewardsList)
    {
	    ObjectPoolManager.Instance.ReturnToPool(dataList);
	    dataList.Clear();
	    Debug.Log(rewardsList.Count);
	    for (int i = 0; i < rewardsList.Count; i++)
	    {
		    string RankIndex = $"第{i + 1}名";
		    List<ItemGroup> oneIndexItems = rewardsList[i];
		    ItemGroup starItem= oneIndexItems.FirstOrDefault(x => x.ItemId == 5);
		    var obj = await ObjectPoolManager.Instance.GetFromPool<ItemRankRuleKV>(parent);
		    dataList.Add(obj);
		    obj.GetComponent<ItemRankRuleKV>().OnRefresh(new ItemRankRuleKVData()
		    {
				Key = RankIndex,
				Value = $"<size=36><sprite=3></size> {starItem.Number}",
		    });
	    }
    }
    #endregion
}
}
