using System.Linq;
using cfg;
using Cysharp.Threading.Tasks;

namespace XN
{
    public partial class RoomManager
    {
        private void UpdateGameState()
        {
            switch (GameStateCtrl.State)
            {
                case MGGameState.进入房间:
                    InitSceneEvent();
                    break;
                case MGGameState.游戏开始:
                    GameStart();

                    var scenInfoComp = SceneHelper.GetSceneInfoComponent();
                    SampleMessagePushManager.UploadLog(new[] { "Battle", "Start" },
                        $"AnchorOpenId:{scenInfoComp.AnchorOpenId} | RoomId:{scenInfoComp.RoomId}");
                    
                    CmdManager.Instance.UpdateInputData();
                    break;
                case MGGameState.游戏结束:
                    //关闭跑马灯
                    UIManager.Instance.CloseWindow<ViewTicker>();
                    CmdManager.Instance.ClearData();

                    // 关闭局内界面 进行中UI元素
                    EventsManager.BroadCast(GameEnum.ViewBattleMainGameOver);

                    // 结算
                    RoomHelper.GetRoomInfoComponent().SettleData();
                    var sceneInfoComp = SceneHelper.GetSceneInfoComponent();
                    sceneInfoComp.SavePoolData();

                    //上报池子数据
                    // PlayerMessage.SendSetPrizePool(sceneInfoComp.AnchorOpenId, sceneInfoComp.LastScorePool, sceneInfoComp.LastFansPool);
                    
                    // 播放视频
                    SoundManager.Instance.PauseMusic();
                    VideoManager.Instance.PlayAsync(VideoType.End, () =>
                    {
                        // 这里特殊处理， 视频结束后，才开始下一段音乐。
                        SoundManager.Instance.PlayMusic(MGGameState.到达终点);
                    }).ToCoroutine();

                    // 上报数据
                    // DataManager.SendRoomData((cbStr) =>
                    // {
                    //     Debug.Log("上报完毕...回调回来...打开结算界面");
                    //     SampleMessagePushManager.UploadLog(new[] { "Battle", "End" }, cbStr);
                    //     // ver1.打开局内结算
                    //     UIManager.Instance.OpenWindow<ViewBattleMVP>().ToCoroutine();
                    // }).ToCoroutine();
                    
                    Debug.Log("上报完毕...回调回来...打开结算界面");
                    // ver1.打开局内结算
                    UIManager.Instance.OpenWindow<ViewBattleMVP>().ToCoroutine();
                    break;
                default:
                    Debug.LogWarning($" TODO ..... {GameStateCtrl.State}");
                    break;
            }
        }

        private async UniTask GameStart()
        {
            foreach (var id in RoomHelper.GetCars())
            {
                var carEntity = EntityManager.Instance.GetEntityById(id);

                if (carEntity.GetComponent(out CarViewComponent carViewComp))
                {
                    //刷新特效
                    carViewComp.RefreshEffect();
                    //刷新灯带
                    carViewComp.RefreshTrackLight();
                    carViewComp.ViewCarInfoItem.RefreshMembers();
                    carViewComp.UpdateDeviceScale();
                }
            }

            SwithGameing();
        }

        private async UniTask SwithGameing()
        {
            await UniTask.Delay(500);
            GameStateCtrl.UpdateState(MGGameState.游戏中);
        }

        private void EnterRoom(int roomId)
        {
            InitRoomData(roomId);
            GameStateCtrl.UpdateState(MGGameState.进入房间);
        }

        private void EndRoom()
        {
            //ClearRoomData();
        }

        /// <summary>
        /// 玩家加入事件
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="carId"></param>
        private void PlayerJoinCar(string playerId, int inputId, long carId = 0) => PlayerJoinCarAsync(playerId, inputId, carId);

        private async UniTask PlayerJoinCarAsync(string playerId, int inputId, long carId = 0)
        {
            EventsManager.BroadCast(GameEnum.ViewBattleMainRefreshEvent);
            EventsManager.BroadCast(GameEnum.ViewMatchRankNodeRefreshEvent);

            var inputConf = TotalConfigManager.ConfigManager.InputIndexConfigCategory.GetOrDefault(inputId);
            if (inputConf.IsGift)
            {
                await UniTask.Delay(1000);
            }

            EventsManager.BroadCast(GameEnum.ViewBattleMainEntranceShowEvent, playerId);

            //落座表现
            await RoomHelper.DoTakeSeatView(playerId);
        }

        /// <summary>
        /// 车队更换排名
        /// </summary>
        private void CarChangeGroup(long carId)
        {
            EventsManager.BroadCast(GameEnum.ViewBattleMainRefreshEvent);
            EventsManager.BroadCast(GameEnum.ViewMatchRankNodeRefreshEvent);
            
            var carViewComponent = EntityManager.Instance.GetEntityById(carId).GetComponent<CarViewComponent>();
            carViewComponent?.ViewCarInfoItem.RefreshInfo();

            //更换灯带
            carViewComponent?.RefreshTrackLight();
        }

        /// <summary>
        /// 车辆里程减少事件
        /// </summary>
        private void CarMileageDelEvent(long carId)
        {
            var carUnit = EntityManager.Instance.GetEntityById(carId);
            var carViewComp = carUnit.GetComponent<CarViewComponent>();
            
            carViewComp?.ViewCarInfoItem.DoPlayViewCarAnimation("fx_ui_ViewCarInfoItem_Hit");
            carViewComp?.DoCarHitAnimation();
            CameraHelper.DoCameraShake();
        }

        /// <summary>
        /// 更新房间事件
        /// </summary>
        /// <param name="index"></param>
        private void UpdateSceneInfo(int index)
        {
            var roomInfoComp = RoomHelper.GetRoomInfoComponent();
            var timeSceneInfo = RoomHelper.GetFightRoomConfig().TimeSceneInfo;
            
            if (index >= timeSceneInfo.Count) 
            {
                return;
            }
            
            roomInfoComp.SetTimeSceneInfo(index);
            
            //更新车辆速度
            float baseSpeed = roomInfoComp.GetScenePlanning().BaseSpeed;
            foreach (var carId in RoomHelper.GetCars())
            {
                var carUnit = EntityManager.Instance.GetEntityById(carId);
                var carInfoComp = carUnit.GetComponent<CarInfoComponent>();
                carInfoComp.SetSpeed(baseSpeed);
            }

            //更新场景数据事件
            UpdateSceneDataEvent();
        }

        /// <summary>
        /// 组刷礼物
        /// </summary>
        /// <param name="giftId"></param>
        /// <param name="num"></param>
        private void GroupBrushGifts(string giftId, int num)
        {
            Debug.Log($"giftId:{giftId} num:{num}");
            
            if (!GameStateCtrl.IsGameAllState)
                return;
           
            var signinfoCc = TotalConfigManager.ConfigManager.SignInfoConfigCategory;
            var constCc = TotalConfigManager.ConfigManager.ConstConfigCategory;
            signinfoCc.SignInfoConfigDic.TryGetValue((ChannelCmd.DouYin, giftId), out var oneGift);
            if (oneGift == null)
            {
                Debug.LogError($"{ChannelCmd.DouYin} + {giftId} 找不到商品");
                return;
            }

            var itemFansValue = constCc.ItemFansValue;
            var itemFansAddRatio = constCc.ItemFansAddRatio;
            var price = oneGift.Price * num;
            if (price < itemFansValue)
                return;
            
            var roomInfoComp = RoomHelper.GetRoomInfoComponent();
            var addFans = price * itemFansAddRatio;
            roomInfoComp.AddFans(addFans);
            
            EventsManager.BroadCast(GameEnum.ViewBattleMainRefreshEvent);
        }

        private void PlayerExitCar(string playerId) => PlayerExitCarAsync(playerId);
        
        /// <summary>
        /// 玩家退出车队事件
        /// </summary>
        /// <param name="playerId"></param>
        private async UniTask PlayerExitCarAsync(string playerId)
        {
            if (!GameStateCtrl.IsGameAllState)
                return;
            
            var playerInfoComp = RoomHelper.GetRoomInfoComponent().GetPlayerInfoComponent(playerId);
            if (playerInfoComp == null)
            {
                Debug.LogError($"playerId:{playerId} is no exist");
                return;
            }

            if (!playerInfoComp.CheckJoinCar())
                return;

            var carUnit = EntityManager.Instance.GetEntityById(playerInfoComp.CarId);
            var carInfoComp = carUnit.GetComponent<CarInfoComponent>();
            carInfoComp.PlayerExitCar(playerId);
            
            playerInfoComp.ExitCar();
            
            //显示层
            var carViewComp = carUnit.GetComponent<CarViewComponent>();
            carViewComp.RefreshCarScaleX();
            carViewComp.ViewCarInfoItem.RefreshInfo();
            await carViewComp.SwitchSkin();
            carViewComp.RefreshCarTitle();
            carViewComp.ViewCarInfoItem?.RefreshMembers();
            
            EventsManager.BroadCast(GameEnum.ViewBattleMainRefreshEvent);
            EventsManager.BroadCast(GameEnum.ViewMatchRankNodeRefreshEvent);
        }

        #region UI 结算 按钮触发

        public async UniTask CloseGame()
        {
            await ClearRoomData();

            UIManager.Instance.OpenWindow<ViewMain>().ToCoroutine();
        }

        public async UniTask OneMoreAgain()
        {
            var curRoomId = RoomHelper.GetRoomInfoComponent().RoomID;
            await ClearRoomData();
            EnterRoom(curRoomId);
            // TODO ? UI 未显示
            UIManager.Instance.OpenWindow<ViewBattleMain>().ToCoroutine();
        }

        #endregion
    }
}