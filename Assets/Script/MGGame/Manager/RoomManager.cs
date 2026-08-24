using System;
using System.Collections.Generic;
using cfg;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = System.Random;

namespace XN
{
    public partial class RoomManager : MonoSingleton<RoomManager>
    {
        public GameObject UnitRoot;
        public GameObject CanvasRoleUI;

        public List<List<Vector2>> GroupLinePos = new();
        private List<List<float>> _groupData = new();
        private List<BackgroundCtrlBase> _backgroundCtrls = new();

        public long RoomUnitId;

        // 全局换组冷却锁
        public float GlobalChangeGroupNextTime = 0;
        
        protected override async void OnInit()
        {
            await UniTask.WaitUntil(() => YooAssetManager.Instance.IsInitialized);
            await UniTask.WaitUntil(() => TotalConfigManager.Instance.IsLoadOver);

            InitLinePos();
            InitScene();

            EventsManager.AddListener(GameEnum.UpdateGameState, UpdateGameState);
            EventsManager.AddListener<int>(GameEnum.EnterRoom, EnterRoom);
            EventsManager.AddListener(GameEnum.EndRoom, EndRoom);
            EventsManager.AddListener<string, int, long>(GameEnum.PlayerJoinCar, PlayerJoinCar);
            EventsManager.AddListener<long>(GameEnum.CarChangeGroup, CarChangeGroup);
            EventsManager.AddListener<long>(GameEnum.CarMileageDelEvent, CarMileageDelEvent);
            EventsManager.AddListener<int>(GameEnum.UpdateSceneInfo, UpdateSceneInfo);
            EventsManager.AddListener<string, int>(GameEnum.GroupBrushGifts, GroupBrushGifts);
            EventsManager.AddListener<string>(GameEnum.PlayerExitCar, PlayerExitCar);
        }

        protected override void OnRemove()
        {
            EventsManager.RemoveListener(GameEnum.UpdateGameState, UpdateGameState);
            EventsManager.RemoveListener<int>(GameEnum.EnterRoom, EnterRoom);
            EventsManager.RemoveListener(GameEnum.EndRoom, EndRoom);
            EventsManager.RemoveListener<string, int, long>(GameEnum.PlayerJoinCar, PlayerJoinCar);
            EventsManager.RemoveListener<long>(GameEnum.CarChangeGroup, CarChangeGroup);
            EventsManager.RemoveListener<long>(GameEnum.CarMileageDelEvent, CarMileageDelEvent);
            EventsManager.RemoveListener<int>(GameEnum.UpdateSceneInfo, UpdateSceneInfo);
            EventsManager.RemoveListener<string, int>(GameEnum.GroupBrushGifts, GroupBrushGifts);
            EventsManager.RemoveListener<string>(GameEnum.PlayerExitCar, PlayerExitCar);
        }

        private void Update()
        {
            if (!GameStateCtrl.IsGaming)
            {
                return;
            }

            UpdateCarsMileage();
            
            GlobalChangeGroupNextTime += Time.deltaTime;
        }

        /// <summary>
        /// 更新车辆位置
        /// </summary>
        private void UpdateCarsMileage()
        {
            RoomHelper.CarsSort();

            var roomInfoComp = RoomHelper.GetRoomInfoComponent();
            var carIds = roomInfoComp.CarIds;

            _groupData.Clear();

            var firstTarget = TotalConfigManager.ConfigManager.ConstConfigCategory.FirstTarget;
            var lastTarget = TotalConfigManager.ConfigManager.ConstConfigCategory.LastTarget;
            float max = CarHelper.GetXByPct(1 - firstTarget);
            float min = CarHelper.GetXByPct(lastTarget);
            var maxMileage = EntityManager.Instance.GetEntityById(carIds[0]).GetComponent<CarInfoComponent>().Mileage;
            var minMileage = EntityManager.Instance.GetEntityById(carIds[6]).GetComponent<CarInfoComponent>().Mileage;
            float teamDiff = 0.2f; //队列内错开距离
            for (int i = 0; i < carIds.Count; i++)
            {
                if (Mathf.Approximately(minMileage, maxMileage))
                {
                    EntityManager.Instance.GetEntityById(carIds[i]).GetComponent<CarPositionComponent>()
                        .SetPosX(max - (i * teamDiff));
                }
                else
                {
                    float mileage = EntityManager.Instance.GetEntityById(carIds[i]).GetComponent<CarInfoComponent>()
                        .Mileage;
                    float pro = (mileage - minMileage) / (maxMileage - minMileage);
                    float targetX = pro * (max - min) + min - (i * teamDiff);
                    EntityManager.Instance.GetEntityById(carIds[i]).GetComponent<CarPositionComponent>()
                        .SetPosX(targetX);
                }
            }
        }

        private void InitLinePos()
        {
            float startPosX = -1.8f;
            float startPosY = 1.75f; // 4.25f;
            float intervalY = 0.75f;

            //从第八名开始，放屏幕下面，不入屏幕
            float tempIntervalY = 4.5f;

            int maxCarNum = TotalConfigManager.ConfigManager.ConstConfigCategory.MaxPlayer;
            for (int i = 0; i < maxCarNum; i++)
            {
                if (i == 7)
                {
                    startPosY -= tempIntervalY;
                }

                List<Vector2> linePos = new();
                for (int j = 0; j < 2; j++)
                {
                    var pos = new Vector2(startPosX, startPosY);
                    startPosY -= intervalY;
                    linePos.Add(pos);
                }

                GroupLinePos.Add(linePos);
            }
        }

        private void InitScene()
        {
            _backgroundCtrls.AddRange(GameObject.Find("World").GetComponentsInChildren<BackgroundCtrlBase>());
        }

        /// <summary>
        /// 初始化场景事件
        /// </summary>
        private void InitSceneEvent()
        {
            foreach (var backgroundCtrl in _backgroundCtrls)
            {
                backgroundCtrl.Init();
            }
        }

        /// <summary>
        /// 更新场景数据事件
        /// </summary>
        private void UpdateSceneDataEvent()
        {
            foreach (var backgroundCtrl in _backgroundCtrls)
            {
                backgroundCtrl.UpdateScene();
            }
        }

        /// <summary>
        /// 初始化房间数据
        /// </summary>
        /// <param name="roomId"></param>
        private void InitRoomData(int roomId)
        {
            var roomConf = TotalConfigManager.ConfigManager.FightRoomConfigCategory.Get(roomId);

            var roomUnit = SceneHelper.Scene().AddChild(EntityType.Room);
            RoomUnitId = roomUnit.Id;

            var roomInfoComp = roomUnit.AddComponent<RoomInfoComponent>();
            roomInfoComp.RoomID = roomId;
            roomInfoComp.EndTime = roomConf.GameTime;
            roomInfoComp.SetTimeSceneInfo(0);

            //继承上一局积分逻辑
            var sceneInfoComp = SceneHelper.GetSceneInfoComponent();
            var pointStartAdd = TotalConfigManager.ConfigManager.ConstConfigCategory.PointStartAdd;
            roomInfoComp.ScorePool = sceneInfoComp.LastScorePool * pointStartAdd;
            roomInfoComp.FansPool = sceneInfoComp.LastFansPool;
            sceneInfoComp.ClearPoolData();

            CreateCars();

            EventsManager.BroadCast(GameEnum.ViewBattleMainRefreshEvent);
        }

        /// <summary>
        /// 清除房间数据
        /// </summary>
        private async UniTask ClearRoomData()
        {
            EntityManager.Instance.RemoveEntity(RoomUnitId);
            RoomUnitId = 0;

            GC.Collect();

            // 模拟 Main.cs 加入强切充值状态
            GameStateCtrl.UpdateState(MGGameState.未进入游戏, true);

            await UniTask.CompletedTask;
        }

        private async UniTask CreateCars()
        {
            var roomConf = RoomHelper.GetFightRoomConfig();
            List<string> nameList = new();

            switch (roomConf.RoomType)
            {
                case FightRoomType.TextRoom:
                    for (int i = 1; i <= 7; i++)
                    {
                        nameList.Add(((TextName)i).ToString());
                    }

                    break;
                case FightRoomType.ZodiacRoom:
                    var allZodiacName = (ZodiacName[])Enum.GetValues(typeof(ZodiacName));
                    var zodiacNames = CalculateHelper.GetRandomUsingShuffle(allZodiacName, 7);
                    foreach (var zodiacName in zodiacNames)
                    {
                        nameList.Add(zodiacName.ToString());
                    }

                    break;
                case FightRoomType.FreeRoom:
                    nameList = new List<string> { "逮虾户", "86上山", "奔驰上树", "藤原豆坊", "F1", "钣金王", "GTR" };
                    break;
            }

            for (int i = 0; i < 7; i++)
            {
                Vector2 startPos = GroupLinePos[i][1];

                RoomHelper.CreateCar(startPos, i, nameList[i]);
            }
        }

        /// <summary>
        /// 更新一个人 初始加入游戏玩法的各排行信息， 辅助后续对比排名
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="ranks"></param>
        public void UpdatePlayerRank(string playerId, List<RankNode> ranks)
        {
            var roomInfoComp = RoomHelper.GetRoomInfoComponent();

            var rankTypeDic = roomInfoComp.RankTypeDic;

            foreach (RankNode oneRank in ranks)
            {
                if (!rankTypeDic.TryGetValue((RankType)oneRank.RankType,
                        out Dictionary<string, RankNode> oneRankTypeDic))
                {
                    oneRankTypeDic = new Dictionary<string, RankNode>();
                    rankTypeDic[(RankType)oneRank.RankType] = oneRankTypeDic;
                }

                oneRankTypeDic[playerId] = oneRank;
            }
        }

        public RankNode GetPlayerRank(RankType rankType, string playerId)
        {
            var roomInfoComp = RoomHelper.GetRoomInfoComponent();

            if (roomInfoComp.RankTypeDic.TryGetValue(rankType, out Dictionary<string, RankNode> rankTypeDic))
            {
                if (rankTypeDic.TryGetValue(playerId, out RankNode oneRank))
                {
                    return oneRank;
                }
            }

            return new RankNode
            {
                RankType = (int)rankType,
                rankIndex = -1, // 榜外
                score = -1.0,
            };
        }

        /// <summary>
        /// 预先加载房间特效
        /// </summary>
        public async UniTask AdvanceAddRoomEffect()
        {
            foreach (var conf in TotalConfigManager.ConfigManager.EffectInfoConfigCategory.DataList)
            {
                if (string.IsNullOrEmpty(conf.EffectRes))
                    continue;
                
                await ObjectPoolManager.Instance.AdvanceAddRes(conf.EffectRes, 50, PrefabType.Effect, obj =>
                {
                    obj.AddComponent<EffectCtrl>().InitData();
                });
            }

            foreach (var conf in TotalConfigManager.ConfigManager.DeviceInfoConfigCategory.DataList)
            {
                if (string.IsNullOrEmpty(conf.DeviceRes))
                    continue;
                
                await ObjectPoolManager.Instance.AdvanceAddRes(conf.DeviceRes, 10, PrefabType.None, obj =>
                {
                    obj.AddComponent<CarCtrl>().InitData(conf.DeviceRes);
                });
            }
        }
    }
}