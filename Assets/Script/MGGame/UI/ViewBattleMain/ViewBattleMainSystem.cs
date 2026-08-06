using System;
using cfg;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DG.Tweening;

namespace XN
{
    public static class ViewBattleMainSystem
    {
        #region CircleLife

        public static void OnOpenSystem(this ViewBattleMain self, UIWindowData uIWindowData)
        {
            if (UIManager.Instance.GameModel == GameModel.Debug)
            {
                self.UIGMVerticalLayoutGroup.gameObject.SetActive(true);
            }
            else
            {
                self.UIGMVerticalLayoutGroup.gameObject.SetActive(false);
            }

            self.UIButtonOverButton.gameObject.SetActive(false);
            self.UIVideoRawImageRawImage.gameObject.SetActive(false);
            self.UIStartButton.gameObject.SetActive(true);
            self.UITakeCrownNodeRectTransform.gameObject.SetActive(false);
            self.UIEndMaskImage.gameObject.SetActive(false);
            // 初始化
            if (self.originalScoreLicensePos == Vector3.zero)
            {
                self.originalScoreLicensePos = self.UIVideoScoreImage.rectTransform.anchoredPosition;
            }

            self.InitTop100Hide();

            self.OpenSubWindow<ViewMatchRankNode>(self.UISubPageNodeRectTransform, new() { Pos = new(0, 450) });
            self.OpenSubWindow<ViewSlideShowNode>(self.UISubPageNodeRectTransform, new() { Pos = new(352, -434) });
            self.OpenSubWindow<ViewBattleRankNode>(self.UISubPageNodeRectTransform, new() { Pos = new(-248, -687) });
            self.OpenSubWindow<ViewMaximumRangeNode>(self.UISubPageNodeRectTransform, new() { Pos = new(440, -272) });
            self.OpenSubWindow<ViewItemShowNode>(self.UISubPageNodeRectTransform, new() { Pos = new(246, 35) });

            self.OnRefresh();

            EventsManager.BroadCast(GameEnum.TopSettingRefreshEvent, "ViewBattleMain");
            EventsManager.AddListener(GameEnum.ViewBattleMainRefreshEvent, self.RefreshEvent);
            EventsManager.AddListener<ViewItemShowNodeData>(GameEnum.ViewBattleMainItemShowEvent,
                self.ItemShowRefreshEvent);
            EventsManager.AddListener<string>(GameEnum.ViewBattleMainEntranceShowEvent, self.EntranceShowEvent);
            EventsManager.AddListener(GameEnum.ViewBattleMainGameOver, self.GameOver);
        }

        public static void OnCloseSystem(this ViewBattleMain self)
        {
            self.UIVideoRawImageRawImage.gameObject.SetActive(false);

            EventsManager.RemoveListener(GameEnum.ViewBattleMainRefreshEvent, self.RefreshEvent);
            EventsManager.RemoveListener<ViewItemShowNodeData>(GameEnum.ViewBattleMainItemShowEvent,
                self.ItemShowRefreshEvent);
            EventsManager.RemoveListener<string>(GameEnum.ViewBattleMainEntranceShowEvent, self.EntranceShowEvent);
            EventsManager.RemoveListener(GameEnum.ViewBattleMainGameOver, self.GameOver);
        }

        public static void OnUpdateSystem(this ViewBattleMain self)
        {
            self.OnUpdateRoom();
        }

        #endregion

        #region UIEvents

        public static void UIGMSendButtonOnClick(this ViewBattleMain self)
        {
            CmdManager.Instance.GMCmd(self.UIGMPlayerIdTMP_InputField.text, self.UIGMCmdTMP_InputField.text);
        }

        public static void GMLogoButtonOnClick(this ViewBattleMain self)
        {
            Debug.Log("GMLogoButtonOnClick");
            SensitiveManager.Refresh();
        }

        public static void UIButtonOverOnClick(this ViewBattleMain self)
        {
            GameStateCtrl.UpdateState(MGGameState.到达终点);
            GameStateCtrl.UpdateState(MGGameState.游戏结束);
            self.UIStartButton.gameObject.SetActive(false);
        }

        public static void UIStartButtonOnClick(this ViewBattleMain self)
        {
            SoundManager.Instance.PauseMusic();
            self.isVideoStart = true;
            VideoManager.Instance.PlayAsync(VideoType.Start, () =>
            {
                self.isVideoStart = false;
                GameStateCtrl.UpdateState(MGGameState.游戏开始);
                SoundManager.Instance.PlayMusic(MGGameState.游戏中);

                Debug.Log(
                    $"EntranceShowData {self.isFuncsRuning} {self.Funcs.Count} | {self.EntranceShowData.Count} {!self.isFuncsRuning}");
                if (self.isFuncsRuning && self.Funcs.Count > 0)
                {
                    self.DoFuncs();
                }
                else if (self.EntranceShowData.Count > 0 && !self.isFuncsRuning)
                {
                    self.DoEntranceShow(self.EntranceShowData.Dequeue());
                }
            }).ToCoroutine();

            CheckStartGame();

            self.UIStartButton.gameObject.SetActive(false);

            if (UIManager.Instance.GameModel == GameModel.Debug)
            {
                self.UIButtonOverButton.gameObject.SetActive(true);
            }
        }

        public static void UIRankButtonOnClick(this ViewBattleMain self)
        {
            UIManager.Instance.OpenWindow<ViewWorldRankMain>(new UIWindowData() { StringArgs1 = "World2Rank", })
                .ToCoroutine();
        }

        /// <summary>
        /// 定榜皮肤
        /// </summary>
        /// <param name="self"></param>
        public static void UIDtaDSkinsButtonOnClick(this ViewBattleMain self)
        {
            UIManager.Instance.OpenWindow<ViewRankLastSeason>().ToCoroutine();
        }

        public static void UIDataFamousButtonOnClick(this ViewBattleMain self)
        {
            UIManager.Instance.OpenWindow<ViewFamousRankMain>().ToCoroutine();
        }

        public static void UIDataMilesButtonOnClick(this ViewBattleMain self)
        {
            UIManager.Instance.OpenWindow<ViewRankMilesFuelPop>().ToCoroutine();
        }

        public static void UIDataWeekSkinsButtonOnClick(this ViewBattleMain self)
        {
            UIManager.Instance.OpenWindow<ViewRankWeekPop>().ToCoroutine();
        }

        public static void UIRankListButtonOnClick(this ViewBattleMain self)
        {
            self.OpenWindow<ViewRankListPop>(new UIWindowData()
            {
                StringArgs1 = "Rank",
            });
        }

        public static void UISkinListButtonOnClick(this ViewBattleMain self)
        {
            self.OpenWindow<ViewRankListPop>(new UIWindowData()
            {
                StringArgs1 = "Skin",
            });
        }

        #endregion

        #region GlobalEvents

        #endregion

        #region Logics

        private static async UniTask OnRefresh(this ViewBattleMain self)
        {
            var roomConf = RoomHelper.GetFightRoomConfig();
            string name = "";
            switch (roomConf.RoomType)
            {
                case FightRoomType.TextRoom:
                    name = "姓氏";
                    break;
                case FightRoomType.ZodiacRoom:
                    name = "生肖";
                    break;
                case FightRoomType.FreeRoom:
                    name = "词语";
                    break;
            }

            self.UIHelpJionTextMeshProUGUI.text = $"<color=#ff9a0d>加+{name}</color>参与玩法";

            self.RefreshEvent();
        }

        private static void OnUpdateRoom(this ViewBattleMain self)
        {
            var roomUnit = RoomHelper.GetRoomUnit();
            if (roomUnit == null)
            {
                return;
            }

            var roomInfoComp = roomUnit.GetComponent<RoomInfoComponent>();
            int lastTime = roomInfoComp.GetGameLastTime();
            float rts = Math.Max(0, roomInfoComp.EndTime - roomInfoComp.Time);

            if (rts <= lastTime)
            {
                self.UITakeCrownNodeRectTransform.gameObject.SetActive(true);
                self.UITimeTextMeshProUGUI.text = "";

                int sec = (int)rts;
                int ms = (int)(rts * 100) % 100;
                self.UITakeCrownText.text = $"{sec}.{ms:D2}";
                return;
            }

            var dateTime = TimeHelper.Time2DateTime((int)rts);
            self.UITimeTextMeshProUGUI.text = $"{dateTime.Minute:D2}:{dateTime.Second:D2}";
        }

        private static void RefreshEvent(this ViewBattleMain self)
        {
            var roomInfoComp = RoomHelper.GetRoomInfoComponent();

            self.UIScoreTextMeshProUGUI.text = UIManagerHelper.UIMathCeil(roomInfoComp?.ScorePool ?? 0);
            self.UIFansTextMeshProUGUI.text = UIManagerHelper.UIMathCeil(roomInfoComp?.FansPool ?? 0);
        }

        private static void ItemShowRefreshEvent(this ViewBattleMain self, ViewItemShowNodeData viewItemShowNodeData)
        {
            if (!self.SubViews.TryGetValue(nameof(ViewItemShowNode), out var uISubViewBase))
                return;

            var subView = uISubViewBase as ViewItemShowNode;
            if (subView.JudgeShowVideo(viewItemShowNodeData))
            {
                //加载动画
                self.DoShowLBYM(viewItemShowNodeData.InputId);
            }

            subView.OnRefresh(viewItemShowNodeData);
        }

        /// <summary>
        /// 半屏 礼物视频播放
        /// </summary>
        /// <param name="self"></param>
        /// <param name="inputId"></param>
        private static async UniTask DoShowLBYM(this ViewBattleMain self, int inputId)
        {
            var inputConf = TotalConfigManager.ConfigManager.InputIndexConfigCategory.Get(inputId);
            if (string.IsNullOrEmpty(inputConf.InputAnimation))
            {
                return;
            }

            VideoManager.Instance.PlayHalfScreenAsync(inputConf.InputAnimation,
                () => { Debug.Log($"礼物 {inputConf.InputAnimation} 视频完了.........."); }).ToCoroutine();
        }

        private static void EntranceShowEvent(this ViewBattleMain self, string playerId)
        {
            self.EntranceShowData.Enqueue(playerId);
            Debug.Log(
                $"EntranceShowData ++ {playerId} {self.EntranceShowData.Count} {!self.isFuncsRuning} {!self.isVideoStart}");
            if (self.EntranceShowData.Count > 0 && !self.isFuncsRuning && !self.isVideoStart)
            {
                self.DoEntranceShow(self.EntranceShowData.Dequeue());
            }
        }

        static void DoFuncs(this ViewBattleMain self)
        {
            if (GameStateCtrl.IsGameEnd)
            {
                Debug.Log($"EntranceShowData Clear top100+join ");
                self.InitTop100Hide();
                self.EntranceShowData.Clear();
                return;
            }

            if (self.isVideoStart)
            {
                return;
            }

            if (self.Funcs.Count > 0)
            {
                self.isFuncsRuning = true;
                Func<UniTask> func = self.Funcs[0];
                self.Funcs.RemoveAt(0);
                func();
            }
            else
            {
                self.isFuncsRuning = false;
            }
        }

        private static async UniTask DoEntranceShow(this ViewBattleMain self, string playerId)
        {
            if (GameConst.IsOptimized)
                return;

            var playerInfoComp = RoomHelper.GetRoomInfoComponent().GetPlayerInfoComponent(playerId);
            var carInfoComp = EntityManager.Instance.GetEntityById(playerInfoComp.CarId)
                .GetComponent<CarInfoComponent>();

            // TODO 主播直播间今日首次（周榜排行-粉丝排行-粉丝徽章）-加入车队；

            var onePlayerFamousRank = RoomManager.Instance.GetPlayerRank(RankType.HallOfFame, playerId);
            int famousRankIndex = onePlayerFamousRank.rankIndex;
            Debug.Log($"famousRankIndex: {famousRankIndex} | RankType.HallOfFame ---> playerId:{playerId}");
            //1.周榜排行 检测
            var onePlayerWeekRank = RoomManager.Instance.GetPlayerRank(RankType.WeekRank, playerId);

            string weekVideoRes = playerInfoComp.CustomVideoId;
            if (string.IsNullOrEmpty(weekVideoRes))
            {
                weekVideoRes =
                    SceneHelper.GetVideoRes(RankType.WeekRank, onePlayerWeekRank.rankIndex, playerInfoComp.Sex);
            }

            Debug.Log(
                $"Index: {onePlayerWeekRank.rankIndex} | RankType.WeekRank ---> playerId:{playerId}| {weekVideoRes}");

            if (!string.IsNullOrEmpty(weekVideoRes) &&
                SceneHelper.GetPlayerFirstJion(RankType.WeekRank.ToString(), playerId))
            {
                SceneHelper.SetPlayerHadJoin(RankType.WeekRank.ToString(), playerId);
                self.Funcs.Add(async () =>
                {
                    var onePlayerMonthRank = RoomManager.Instance.GetPlayerRank(RankType.MonthRank, playerId);
                    Debug.Log(
                        $"{playerId} --- {RankType.WeekRank} --- week: {onePlayerWeekRank.rankIndex} / month: {onePlayerMonthRank.rankIndex}");
                    int monthIndex = onePlayerMonthRank.rankIndex;
                    if (0 < monthIndex)
                    {
                        self.UIVideoWeekRankTextMeshProUGUI.SetText(
                            $"<size=36>月榜</size>\n<size=48><color=#13e3f0>{monthIndex}</color></size>");
                        self.UIVideoWeekRankTextMeshProUGUI.transform.parent.gameObject.SetActive(true);
                    }
                    else
                    {
                        self.UIVideoWeekRankTextMeshProUGUI.SetText("");
                        self.UIVideoWeekRankTextMeshProUGUI.transform.parent.gameObject.SetActive(false);
                    }

                    self.UIVideoScoreImage.GetComponentInChildren<ViewHeadItem>()?.OnRefresh(new ViewHeadItemData()
                    {
                        PlayerId = playerInfoComp.PlayerId,
                        // NickName = playerInfoComp.Name,
                        AvatarUrl = playerInfoComp.AvatarUrl,
                        Frame = RankHelper.GetHallOfFameFrameResByIndex(famousRankIndex),
                        SortingOrder = 100
                    });
                    // self.UIVideoScoreNameTextMeshProUGUI.SetText(playerInfoComp.Name);
                    self.UIVideoScoreNameText.text = playerInfoComp.Name;
                    // 周榜
                    // self.UIVideoScoreTextMeshProUGUI.SetText($"世界排名：<color=#fff830><size=100>{onePlayerWeekRank.rankIndex}</size></color>");
                    self.UIVideoScoreTextMeshProUGUI.SetText($"{onePlayerWeekRank.rankIndex}");
                    self.UIVideoScoreImage.gameObject.SetActive(true);

                    var rect = self.UIVideoScoreImage.rectTransform;
                    rect.DOKill(true);
                    rect.anchoredPosition =
                        new Vector2(self.originalScoreLicensePos.x, self.originalScoreLicensePos.y + 600);
                    rect.localRotation = Quaternion.identity;
                    Sequence seq = DOTween.Sequence();
                    seq.Join(rect.DOAnchorPosY(self.originalScoreLicensePos.y, 0.5f).SetEase(Ease.OutBack));
                    seq.Join(rect.DOPunchRotation(new Vector3(0, 0, 15), 3.0f, 2, 1f));

                    SoundManager.Instance.PauseMusic();
                    VideoManager.Instance.PlayPlayerDetailAsync(weekVideoRes, (() =>
                    {
                        self.UIVideoScoreImage.gameObject.SetActive(false);
                        self.DoFuncs();
                    }));


                    seq.AppendInterval(2.5f);
                    seq.Append(rect.DOAnchorPosY(self.originalScoreLicensePos.y + 600, 0.5f));
                });
            }

            //2.粉丝排行 检测
            var onePlayerFansRank = RoomManager.Instance.GetPlayerRank(RankType.FansRank, playerId);
            string fansVideoRes = SceneHelper.GetVideoRes(RankType.FansRank, onePlayerFansRank.rankIndex);
            Debug.Log($"Index: {onePlayerFansRank.rankIndex} | RankType.FansRank ---> playerId:{playerId}");
            if (!string.IsNullOrEmpty(fansVideoRes) &&
                SceneHelper.GetPlayerFirstJion(RankType.FansRank.ToString(), playerId))
            {
                SceneHelper.SetPlayerHadJoin(RankType.FansRank.ToString(), playerId);
                self.Funcs.Add(async () =>
                {
                    Debug.Log($"{playerId} --- {RankType.FansRank} --- {onePlayerFansRank?.rankIndex}");
                    SoundManager.Instance.PauseMusic();
                    VideoManager.Instance.PlayPlayerDetailAsync(fansVideoRes, (() => { self.DoFuncs(); }));
                });
            }

            // 百强 / 粉丝勋章，两个概念
            //2.2 粉丝勋章
            string badgeImage = SceneHelper.GetFansBadgeRes((int)onePlayerFansRank.score);
            if (!string.IsNullOrEmpty(badgeImage) && SceneHelper.GetPlayerFirstJion("FansBadge", playerId))
            {
                SceneHelper.SetPlayerHadJoin("FansBadge", playerId);
                self.Funcs.Add(async () =>
                {
                    Debug.Log($"{playerId} --- (粉丝勋章)FansBadge --- fans:{onePlayerFansRank.score}");
                    self.UIVideoFansVerticalLayoutGroup.enabled = false;
                    var rect2 = self.UIFansIconImage.rectTransform;
                    rect2.DOKill(true);
                    rect2.anchoredPosition = new Vector2(0, 1200);
                    await YooAssetManager.Instance.LoadSpriteAsync(badgeImage, self.UIFansIconImage, true);
                    var spriteOriginal = self.UIFansIconImage.sprite.rect;
                    float offsetScale = 1200f / self.UIFansIconImage.sprite.rect.height;
                    var finalsizeDelta = new Vector2(spriteOriginal.width * offsetScale,
                        spriteOriginal.height * offsetScale);
                    rect2.anchoredPosition = new Vector2(0, self.UIFansIconImage.sprite.rect.height / 2);
                    // rect2.sizeDelta = new Vector2(spriteOriginal.width * offsetScale, spriteOriginal.height * offsetScale);

                    Sequence seq2 = DOTween.Sequence();
                    seq2.Join(rect2.DOSizeDelta(finalsizeDelta, 0.5f).SetEase(Ease.OutQuad));
                    seq2.Join(rect2.DOAnchorPosY(-spriteOriginal.height * offsetScale / 2, 0.5f).SetEase(Ease.OutQuad));
                    ViewHeadItem headItem = self.UIVideoFansVerticalLayoutGroup.GetComponentInChildren<ViewHeadItem>();
                    headItem?.OnRefresh(new ViewHeadItemData()
                    {
                        PlayerId = playerInfoComp.PlayerId,
                        // NickName = playerInfoComp.Name,
                        AvatarUrl = playerInfoComp.AvatarUrl,
                        Frame = RankHelper.GetHallOfFameFrameResByIndex(famousRankIndex),
                    });

                    var headRect = headItem?.GetComponent<RectTransform>();
                    if (headRect != null)
                    {
                        var targetY = -finalsizeDelta.y - 95 - 30f;
                        headRect.anchoredPosition = new Vector2(0, targetY + 50);
                        seq2.Append(headRect.DOAnchorPosY(targetY, 0.3f).SetEase(Ease.OutQuad));
                    }

                    var nameBgRect = self.UIVideoFansNameText.transform.parent.GetComponent<RectTransform>();
                    if (nameBgRect != null)
                    {
                        var targetY = -finalsizeDelta.y - 190 - 30 * 2 - 47.5f;
                        nameBgRect.anchoredPosition = new Vector2(0, targetY + 50);
                        seq2.Append(nameBgRect.DOAnchorPosY(targetY, 0.2f).SetEase(Ease.OutQuad));
                    }

                    self.UIVideoFansNameText.text = playerInfoComp.Name;
                    self.UIVideoFansVerticalLayoutGroup.gameObject.SetActive(true);
                    SoundManager.Instance.ContinueMusic();
                    await UniTask.Delay(3000);
                    self.UIVideoFansVerticalLayoutGroup.gameObject.SetActive(false);
                    self.DoFuncs();
                });
            }

            //3. 必播 ---- 加入车队
            self.Funcs.Add(async () =>
            {
                Debug.Log($"{playerId} --- 加入车队 --- ");
                self.UIShowEntranceNodeRectTransform.gameObject.SetActive(true);
                // self.UIPlayerNameTextMeshProUGUI.text = playerInfoComp.Name;
                self.UIPlayerJoinCarNameText.text = playerInfoComp.Name;
                var roomConf = RoomHelper.GetFightRoomConfig();
                switch (roomConf.RoomType)
                {
                    case FightRoomType.TextRoom:
                        self.UICarNameText.text = $"{carInfoComp.Name}氏车队";
                        break;
                    case FightRoomType.ZodiacRoom:
                        self.UICarNameText.text = $"{carInfoComp.Name}车队";
                        break;
                    case FightRoomType.FreeRoom:
                        self.UICarNameText.text = $"{carInfoComp.Name}车队";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                self.viewHeadItem.OnRefresh(new ViewHeadItemData()
                {
                    PlayerId = playerId
                });

                // SoundManager.Instance.SetMusicVideoStart();
                SoundManager.Instance.ContinueMusic();
                VideoManager.Instance.PlayPlayerDetailAsync("join", () =>
                {
                    // SoundManager.Instance.SetMusicVideoClose();
                    self.UIShowEntranceNodeRectTransform.gameObject.SetActive(false);

                    if (GameStateCtrl.IsGameEnd)
                    {
                        self.EntranceShowData.Clear();
                    }
                    else
                    {
                        if (self.EntranceShowData.Count > 0)
                        {
                            self.DoEntranceShow(self.EntranceShowData.Dequeue());
                        }
                    }

                    self.DoFuncs();
                });
            });

            if (!self.isFuncsRuning)
            {
                self.DoFuncs();
            }
        }

        private static async UniTask CheckStartGame()
        {
            await UniTask.Delay(6000);

            if (GameStateCtrl.State < MGGameState.游戏开始)
            {
                GameStateCtrl.UpdateState(MGGameState.游戏开始);
                SoundManager.Instance.PlayMusic(MGGameState.游戏中);
            }
        }

        private static void GameOver(this ViewBattleMain self)
        {
            // self.lastRankNodePos = self.viewBattleRankNode.transform.position;
            self.InitTop100Hide();
            self.UIEndMaskImage.gameObject.SetActive(true);
        }

        public static void InitTop100Hide(this ViewBattleMain self)
        {
            Debug.Log("InitTop100Hide top100+join ");
            self.isFuncsRuning = false;
            self.Funcs.Clear();
            self.UIVideoScoreImage.gameObject.SetActive(false);
            self.UIVideoFansVerticalLayoutGroup.gameObject.SetActive(false);
            self.UIShowEntranceNodeRectTransform.gameObject.SetActive(false);
        }

        #endregion
    }
}