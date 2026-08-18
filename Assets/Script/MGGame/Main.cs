using System;
using System.Collections.Generic;
using System.IO;
using ByteDance.LiveOpenSdk.Runtime;
using cfg;
using cfg.Global;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace XN
{
    public class Main : MonoBehaviour
    {
        [SerializeField] [LabelText("限制帧率")] private int frameRate = 60;
        public static long SceneUnitId;
        public GameObject LoadingUI;
        private ViewLoadingMain viewLoadingMain;
        public static Camera MainCamera;

        private async void Awake()
        {
            MainCamera = Camera.main;

            Application.targetFrameRate = frameRate;
#if UNITY_EDITOR
            // UIManager.Instance.GameModel = GameModel.Debug;
            // Y轴排序 URP下会隐藏的
            SetIsomerticSortMode();
// #else
//             UIManager.Instance.GameModel = GameModel.Release;
#endif

            viewLoadingMain = LoadingUI.GetComponent<ViewLoadingMain>();
            LocalLog.LaunchHandleLog();
        }

        private async void Start()
        {
            LiveOpenSdk.CloudGameApi.TryInitFullScreen();

            LoadingUI.gameObject.SetActive(true);
            int totalNum = 8;
            viewLoadingMain.Refresh(0, totalNum);
            // TODO  loading 界面补充，开始多起来右前后关系了
            await UniTask.WaitUntil(() => YooAssetManager.Instance.IsInitialized);
            viewLoadingMain.Refresh(1, totalNum);
            await UniTask.WaitUntil(() => TotalConfigManager.Instance.IsLoadOver);
            viewLoadingMain.Refresh(2, totalNum);

            viewLoadingMain.LoadLogo();

            await UniTask.WaitUntil(() => ObjectPoolManager.Instance.IsInitialized);
            viewLoadingMain.Refresh(3, totalNum);
            await UniTask.WaitUntil(() => UIManager.Instance.IsInitialized);
            viewLoadingMain.Refresh(4, totalNum);
            await UniTask.WaitUntil(() => SoundManager.Instance.IsInitialized);
            viewLoadingMain.Refresh(5, totalNum);
            await UniTask.WaitUntil(() => DySdkManager.Instance.IsInitialized);
            viewLoadingMain.Refresh(6, totalNum);
            await RoomManager.Instance.AdvanceAddRoomEffect();
            viewLoadingMain.Refresh(7, totalNum);
            await UniTask.WaitUntil(() => CmdManager.Instance.IsInitialized);
            viewLoadingMain.Refresh(8, totalNum);

            InitData();

            GameStateCtrl.UpdateState(MGGameState.未进入游戏);
            SoundManager.Instance.PlayMusic(MGGameState.未进入游戏);

            await UIManager.Instance.OpenWindow<ViewMain>(new UIWindowData()
            {
                IntArgs1 = (int)FightRoomType.TextRoom,
            });
            UIManager.Instance.OpenWindow<TopSetting>().ToCoroutine();
            LoadingUI.gameObject.SetActive(false);

            gameObject.AddComponent<FPSView>();
        }

        private async UniTask InitData()
        {
            //建立scene节点
            var sceneUnit = EntityManager.Instance.CreateEntity(EntityType.Scene);
            SceneUnitId = sceneUnit.Id;
            sceneUnit.AddComponent<SceneInfoComponent>();

            var rankUnit = sceneUnit.AddChild(EntityType.Rank);
            rankUnit.AddComponent<RankInfoComponent>();

            var timeUnit = sceneUnit.AddChild(EntityType.Time);
            timeUnit.AddComponent<TimeInfoComponent>();

            // Server ....  Data ... 初始记录处理~~~
            ServerResponse resp = await DataManager.GetRankOverTimes();
            var timeComp = SceneHelper.GetTimeUnit()?.GetComponent<TimeInfoComponent>();
            if (timeComp != null)
            {
                timeComp.ServerId = resp.ServerId;
                timeComp.ServerClientTime = resp.ClientTime;

                // TODO 验证时间 排行榜起止
                Debug.Log("server时间 : ServerId " + resp.ServerId);
                Debug.Log("server时间 : ServerTime " + resp.ServerTime);
                Debug.Log("client时间 : DateTime now" + DateTime.Now);
                Debug.Log("client时间 : DateTime now Millisecond" + DateTime.Now.Millisecond);
                // Debug.Log("server : DateTime 北京0+8" + DateTime.UtcNow.AddHours(8));
                // Debug.Log("server : 本地时间戳:" + DateTime.UtcNow.Subtract(timeComp.DateTime1970).TotalMilliseconds);
                // timeComp.ServerTimeAndLocalOffset = resp.ServerTime - DateTime.Now.Millisecond;
                // Debug.Log("server : 时差:" + timeComp.ServerTimeAndLocalOffset);
            }

            var rankComp = SceneHelper.GetRankUnit()?.GetComponent<RankInfoComponent>();
            if (rankComp != null && resp.RankTimes.Count > 0)
            {
                Dictionary<RankType, RankTimesData> rankOverTimes = new();
                foreach (var kv in resp.RankTimes)
                {
                    if (Enum.TryParse<RankType>(kv.Key, true, out RankType enumKey))
                    {
                        rankOverTimes.TryAdd(enumKey, kv.Value);
                    }
                }

                rankComp.RankInfo = rankOverTimes;
            }

            var sceneInfoComponent = SceneHelper.GetSceneInfoComponent();

#if UNITY_EDITOR
            sceneInfoComponent.AnchorOpenId = "UnityEditor";
            sceneInfoComponent.RoomId = "UnityEditor";
#else
            sceneInfoComponent.AnchorOpenId = "UnityAPK";
            sceneInfoComponent.RoomId = "UnityAPK";
#endif

            SceneHelper.GetSceneInfoComponent().InitPoolData();

#if !UNITY_EDITOR
            var roomInfo = await DySdkManager.SampleMessagePushManager.RoomInfoService.WaitForRoomInfoAsync();
            sceneInfoComponent.AnchorOpenId = roomInfo.Anchor.OpenId;
            sceneInfoComponent.RoomId = roomInfo.RoomId;
#endif
            // TODO  lixin : 打包出来以后，如果是自己跑，上面的SceneInfo信息等不到直播伴侣拉取信息，下面不执行，影响Socket,排行榜时间显示
            Debug.Log($"AnchorOpenId:{sceneInfoComponent.AnchorOpenId} | RoomId:{sceneInfoComponent.RoomId}");
            SocketManager.Instance.InitSocket(sceneInfoComponent.RoomId);
        }

#if UNITY_EDITOR
        [Button("2D渲染Y偏移模式")]
        public void SetIsomerticSortMode()
        {
            UnityEngine.Rendering.GraphicsSettings.transparencySortMode = TransparencySortMode.CustomAxis;
            UnityEngine.Rendering.GraphicsSettings.transparencySortAxis = new Vector3(-0.0001f, 0.5f, 0);
        }

        [Button("测试礼物Cmd接口")]
        public void CmdGift()
        {
            List<Func<UniTask>> Funcs = new();
            Funcs.Add(async () =>
            {
                CmdManager.Instance.SDKMessageUpdateUserInfo("_000Bee6QAsXg7FQBVPQl1cgASsP3KmGXJSy", "七曜",
                    "https://p26.douyinpic.com/aweme/100x100/aweme-avatar/tos-cn-avt-0015_1b7857340ec4f06d606a770b78b6bed6.jpeg?from=3067671334");
                CmdManager.Instance.SDKMessageChat("_000Bee6QAsXg7FQBVPQl1cgASsP3KmGXJSy", " 加入钟", 1764829620040);
            });
            // Funcs.Add(async () =>
            // {
            //     UnityEngine.Debug.Log("SDKMessageLike");
            //     CmdManager.Instance.SDKMessageLike("lx", 666, TimeHelper.GetTimeStampMs());
            // });
            // Funcs.Add(async () =>
            // {
            //     UnityEngine.Debug.Log("SDKMessageGift");
            //     string dyGiftId = "n1/Dg1905sj1FyoBlQBvmbaDZFBNaKuKZH6zxHkv8Lg5x2cRfrKUTb8gzMs=";
            //     CmdManager.Instance.SDKMessageGift("lx", dyGiftId, 1000, TimeHelper.GetTimeStampMs());
            // });

            Funcs[Random.Range(0, Funcs.Count)].Invoke();
        }

        [BoxGroup("GM")]
        [Button("设置游戏速度")]
        public void SetGameSpeed(int gameSpeed = 1)
        {
            Time.timeScale = gameSpeed;
        }

        [BoxGroup("GM")]
        [Button("设置场景id")]
        public void SetSceneId(int sceneId = 1)
        {
            SceneHelper.GetSceneInfoComponent().SceneId = sceneId;
        }

        [BoxGroup("GM")]
        [Button("GM")]
        public async UniTask GM(string playerId = "lzz1", string content = "121")
        {
            CmdManager.Instance.GMCmd(playerId, content);
        }

        [BoxGroup("GM")]
        [Button("GM结束")]
        public void GmGameEnd()
        {
            GameStateCtrl.UpdateState(MGGameState.到达终点);
            GameStateCtrl.UpdateState(MGGameState.游戏结束);
        }
#endif

        [BoxGroup("GM")]
        [Button("GM测试所有指令")]
        public static async UniTask GmTestAll(int maxNum = 8, int cmd1 = 111, int cmd2 = 111)
        {
            if (UIManager.Instance.GameModel == GameModel.Release) return;

            List<TextName> textNames = new()
            {
                TextName.乌尔古宸,
                TextName.一那蒌,
                TextName.丑穆陵,
                TextName.万俟,
                TextName.上官,
            };

            int maxCarNum = TotalConfigManager.ConfigManager.ConstConfigCategory.MaxPlayer;
            for (int i = textNames.Count; i < maxCarNum; i++)
            {
                textNames.Add((TextName)Random.Range(1, Enum.GetValues(typeof(TextName)).Length));
            }

            for (int i = 1; i <= maxNum; i++)
            {
                string name = "lzz" + i;

                // CmdManager.Instance.GMCmd(name, $"加入{textNames[Random.Range(0, textNames.Count)]}");
                CmdManager.Instance.GMCmd(name, $"加入{textNames[Math.Clamp(i, 0, textNames.Count)]}");
                for (int j = cmd1; j <= cmd2; j++)
                {
                    CmdManager.Instance.GMCmd(name, j.ToString());
                }
            }
        }

#if UNITY_EDITOR
        [Button("时间转换")]
        public void Time2Date(long ms = 1765382400000)
        {
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(ms);
            Debug.Log($"传入Unix：{ms} ===> {dateTimeOffset.DateTime} ===> 时区偏移: {dateTimeOffset.ToLocalTime().DateTime}");
            Debug.Log($"next Day --> {RankHelper.GetNextRefreshDay(ms, RankEnum.Day).ToString("M月d日HH:mm:ss")}");
            Debug.Log($"next Week --> {RankHelper.GetNextRefreshDay(ms, RankEnum.Week).ToString("M月d日HH:mm:ss")}");
            Debug.Log(
                $"next HalfMonth --> {RankHelper.GetNextRefreshDay(ms, RankEnum.HalfMonth).ToString("M月d日HH:mm:ss")}");
            Debug.Log($"next Month --> {RankHelper.GetNextRefreshDay(ms, RankEnum.Month).ToString("M月d日HH:mm:ss")}");
        }

        // [Button("单位转换")]
        // public void MathCeil()
        // {
        //     
        //     Debug.Log(UIManagerHelper.UIMathCeil(8888.88f));
        //     Debug.Log(UIManagerHelper.UIMathCeil(15005.4f));
        //     Debug.Log(UIManagerHelper.UIMathCeil(125000));
        //     Debug.Log(UIManagerHelper.UIMathCeil(120370));
        //     Debug.Log(UIManagerHelper.UIMathCeil(120334456.78f));
        // }

        [Button("测试流程")]
        public async UniTask TestAddPlayer()
        {
            // 7分钟
            // 加入车队
            await UniTask.Delay(1000);
            CmdManager.Instance.GMCmd("沙雕艺术家", "加入李");
            await UniTask.Delay(1000);
            CmdManager.Instance.GMCmd("沙雕艺术家2", "加入李");
            await UniTask.Delay(1000);
            CmdManager.Instance.GMCmd("青柠薄荷糖", "加入赵");
            await UniTask.Delay(1000);
            CmdManager.Instance.GMCmd("星河漫游者", "加入周");
            await UniTask.Delay(1000);
            CmdManager.Instance.GMCmd("星河漫游者2", "加入周");
            await UniTask.Delay(1000);
            CmdManager.Instance.GMCmd("星河漫游者3", "加入周");
            await UniTask.Delay(10000);

            CmdManager.Instance.GMCmd("零度冰川", "加入钱");
            await UniTask.Delay(2000);
            CmdManager.Instance.GMCmd("暗夜猎手", "加入孙");
            await UniTask.Delay(2000);
            CmdManager.Instance.GMCmd("奶盖兔叽", "加入吴");
            await UniTask.Delay(2000);
            CmdManager.Instance.GMCmd("桃桃乌龙茶", "加入王");
            await UniTask.Delay(2000);
            CmdManager.Instance.GMCmd("秃头少女自救指南", "加入张");
            await UniTask.Delay(10000);

            // 兑换
            CmdManager.Instance.GMCmd("沙雕艺术家", "10");
            CmdManager.Instance.GMCmd("沙雕艺术家", "查询");
            await UniTask.Delay(3000);
            CmdManager.Instance.GMCmd("沙雕艺术家", "兑换117");
            await UniTask.Delay(3000);
            CmdManager.Instance.GMCmd("沙雕艺术家", "更换117");
            await UniTask.Delay(3000);
            CmdManager.Instance.GMCmd("沙雕艺术家", "查询");
            await UniTask.Delay(3000);

            CmdManager.Instance.GMCmd("星河漫游者", "10");
            CmdManager.Instance.GMCmd("星河漫游者", "666");
            await UniTask.Delay(3000);
            CmdManager.Instance.GMCmd("星河漫游者", "还原");
            await UniTask.Delay(1000);
            CmdManager.Instance.GMCmd("星河漫游者", "查询");
            await UniTask.Delay(3000);
            CmdManager.Instance.GMCmd("星河漫游者", "兑换116");
            await UniTask.Delay(3000);
            CmdManager.Instance.GMCmd("星河漫游者", "更换116");
            await UniTask.Delay(3000);

            CmdManager.Instance.GMCmd("沙雕艺术家", "10");
            await UniTask.Delay(3000);
            CmdManager.Instance.GMCmd("星河漫游者", "11");
            await UniTask.Delay(3000);
            CmdManager.Instance.GMCmd("沙雕艺术家", "12");
            await UniTask.Delay(3000);
            CmdManager.Instance.GMCmd("星河漫游者", "13");
            await UniTask.Delay(3000);
            CmdManager.Instance.GMCmd("沙雕艺术家", "14");
            await UniTask.Delay(3000);
            CmdManager.Instance.GMCmd("星河漫游者", "15");
            await UniTask.Delay(3000);

            CmdManager.Instance.GMCmd("沙雕艺术家", "16");
            await UniTask.Delay(3000);
            CmdManager.Instance.GMCmd("星河漫游者", "17");
            await UniTask.Delay(3000);
            CmdManager.Instance.GMCmd("沙雕艺术家", "18");
            await UniTask.Delay(3000);
            CmdManager.Instance.GMCmd("星河漫游者", "19");
            await UniTask.Delay(3000);
            CmdManager.Instance.GMCmd("沙雕艺术家", "20");
            await UniTask.Delay(3000);
            CmdManager.Instance.GMCmd("星河漫游者", "21");
            await UniTask.Delay(3000);


            CmdManager.Instance.GMCmd("沙雕艺术家", "24");
            await UniTask.Delay(5000);
            CmdManager.Instance.GMCmd("星河漫游者", "29");
            await UniTask.Delay(5000);

            CmdManager.Instance.GMCmd("沙雕艺术家", "34");
            CmdManager.Instance.GMCmd("星河漫游者", "34");
            await UniTask.Delay(5000);

            CmdManager.Instance.GMCmd("沙雕艺术家", "35");
            await UniTask.Delay(5000);
            CmdManager.Instance.GMCmd("星河漫游者", "36");
            await UniTask.Delay(5000);

            CmdManager.Instance.GMCmd("沙雕艺术家", "39");
            await UniTask.Delay(5000);
            CmdManager.Instance.GMCmd("星河漫游者", "41");
            await UniTask.Delay(5000);

            CmdManager.Instance.GMCmd("沙雕艺术家2", "44");
            await UniTask.Delay(10000);

            // 两三分钟的换车
            CmdManager.Instance.GMCmd("青柠薄荷糖", "29");
            await UniTask.Delay(1000);
            CmdManager.Instance.GMCmd("青柠薄荷糖", "34");
            await UniTask.Delay(1000);
        }

        [Button("测试道具")]
        public async UniTask Test2()
        {
            int time = 1;
            CmdManager.Instance.GMCmd("沙雕艺术家", "加入李");
            CmdManager.Instance.GMCmd("星河漫游者", "加入杨");
            CmdManager.Instance.GMCmd("暗夜猎手", "加入孙");
            await UniTask.Delay(time);

            CmdManager.Instance.GMCmd("沙雕艺术家", "16");
            await UniTask.Delay(time);
            CmdManager.Instance.GMCmd("星河漫游者", "17");
            await UniTask.Delay(time);
            CmdManager.Instance.GMCmd("暗夜猎手", "18");
            await UniTask.Delay(time);
            CmdManager.Instance.GMCmd("星河漫游者", "19");
            await UniTask.Delay(time);
            CmdManager.Instance.GMCmd("沙雕艺术家", "20");
            await UniTask.Delay(time);
            CmdManager.Instance.GMCmd("零度冰川", "21");
            await UniTask.Delay(time);

            CmdManager.Instance.GMCmd("沙雕艺术家", "24");
            await UniTask.Delay(time);
            CmdManager.Instance.GMCmd("星河漫游者", "29");
            await UniTask.Delay(time);

            CmdManager.Instance.GMCmd("零度冰川", "34");
            await UniTask.Delay(time);
            CmdManager.Instance.GMCmd("暗夜猎手", "34");
            await UniTask.Delay(time);

            CmdManager.Instance.GMCmd("沙雕艺术家", "35");
            await UniTask.Delay(time);
            CmdManager.Instance.GMCmd("奶盖兔叽", "36");
            await UniTask.Delay(time);

            CmdManager.Instance.GMCmd("沙雕艺术家", "39");
            await UniTask.Delay(time);
            CmdManager.Instance.GMCmd("星河漫游者", "41");
            await UniTask.Delay(time);

            CmdManager.Instance.GMCmd("零度冰川", "44");
            await UniTask.Delay(time);

            // 两三分钟的换车
            CmdManager.Instance.GMCmd("青柠薄荷糖", "29");
            await UniTask.Delay(time);
            CmdManager.Instance.GMCmd("青柠薄荷糖", "34");
            await UniTask.Delay(time);
        }

        [Button("测试道具item")]
        public async UniTask TestItem()
        {
            for (int i = 0; i < 10; i++)
            {
                CmdManager.Instance.GMCmd("沙雕艺术家", "10");
            }

            await UniTask.Delay(300);
            CmdManager.Instance.GMCmd("沙雕艺术家", "16");
            await UniTask.Delay(300);
            CmdManager.Instance.GMCmd("星河漫游者", "16");
        }

        [Button("测试换肤")]
        public async UniTask Test3()
        {
            // 101~117 换肤
            List<(string, int, int)> list = new List<(string, int, int)>()
            {
                ("雾里看花", 101, 105),
                ("‌普鲁斯特效应‌", 106, 110),
                ("叛逆甜心", 111, 115),
                ("雾里看花", 101, 105),
            };
            CmdManager.Instance.GMCmd(list[0].Item1, "加入赵");
            await UniTask.Delay(3000);
            CmdManager.Instance.GMCmd(list[0].Item1, "20");
            await UniTask.Delay(1000);
            CmdManager.Instance.GMCmd(list[0].Item1, $"兑换101");
            await UniTask.Delay(1000);
            CmdManager.Instance.GMCmd(list[0].Item1, $"兑换102");
            await UniTask.Delay(1000);
            CmdManager.Instance.GMCmd(list[0].Item1, $"兑换103");
            await UniTask.Delay(1000);
            CmdManager.Instance.GMCmd(list[0].Item1, $"兑换104");
            await UniTask.Delay(1000);
            CmdManager.Instance.GMCmd(list[0].Item1, $"兑换105");
            await UniTask.Delay(1000);
            CmdManager.Instance.GMCmd(list[0].Item1, $"兑换106");
            await UniTask.Delay(1000);
            CmdManager.Instance.GMCmd(list[0].Item1, $"兑换107");
            await UniTask.Delay(1000);
            CmdManager.Instance.GMCmd(list[0].Item1, $"兑换108");
            await UniTask.Delay(1000);

            CmdManager.Instance.GMCmd(list[1].Item1, "加入钱");
            await UniTask.Delay(3000);
            CmdManager.Instance.GMCmd(list[1].Item1, "20");
            await UniTask.Delay(1000);
            CmdManager.Instance.GMCmd(list[1].Item1, $"兑换109");
            await UniTask.Delay(1000);
            CmdManager.Instance.GMCmd(list[1].Item1, $"兑换110");
            await UniTask.Delay(1000);
            CmdManager.Instance.GMCmd(list[1].Item1, $"兑换111");
            await UniTask.Delay(1000);
            CmdManager.Instance.GMCmd(list[1].Item1, $"兑换112");
            await UniTask.Delay(1000);
            CmdManager.Instance.GMCmd(list[1].Item1, $"兑换113");
            await UniTask.Delay(1000);
            CmdManager.Instance.GMCmd(list[1].Item1, $"兑换114");
            await UniTask.Delay(1000);
            CmdManager.Instance.GMCmd(list[1].Item1, $"兑换115");
            await UniTask.Delay(1000);
            CmdManager.Instance.GMCmd(list[1].Item1, $"兑换116");
            await UniTask.Delay(1000);


            CmdManager.Instance.GMCmd(list[0].Item1, $"使用101");
            CmdManager.Instance.GMCmd(list[1].Item1, $"使用109");
            await UniTask.Delay(1000);
            CmdManager.Instance.GMCmd(list[0].Item1, $"使用102");
            CmdManager.Instance.GMCmd(list[1].Item1, $"使用109");
            await UniTask.Delay(1000);
            CmdManager.Instance.GMCmd(list[0].Item1, $"使用103");
            CmdManager.Instance.GMCmd(list[1].Item1, $"使用109");
            await UniTask.Delay(1000);
            CmdManager.Instance.GMCmd(list[0].Item1, $"使用104");
            CmdManager.Instance.GMCmd(list[1].Item1, $"使用109");
            await UniTask.Delay(1000);
            CmdManager.Instance.GMCmd(list[0].Item1, $"使用105");
            CmdManager.Instance.GMCmd(list[1].Item1, $"使用109");
            await UniTask.Delay(1000);
            CmdManager.Instance.GMCmd(list[0].Item1, $"使用106");
            CmdManager.Instance.GMCmd(list[1].Item1, $"使用109");
            await UniTask.Delay(1000);
            CmdManager.Instance.GMCmd(list[0].Item1, $"使用107");
            CmdManager.Instance.GMCmd(list[1].Item1, $"使用109");
            await UniTask.Delay(1000);
            CmdManager.Instance.GMCmd(list[0].Item1, $"使用108");
            CmdManager.Instance.GMCmd(list[1].Item1, $"使用109");
            await UniTask.Delay(1000);
        }

        [Button("测试多人加入车队")]
        public async UniTask Test4()
        {
            CmdManager.Instance.GMCmd("沙雕艺术家", "16");
            CmdManager.Instance.GMCmd("暗夜猎手", "18");
            await UniTask.Delay(1000);
            CmdManager.Instance.GMCmd("云朵棉花糖", "20");

            //⌚😈🍚🍖🦋🉑☀️🌹💞💰🍪ℋღ浪漫🌹💞࿐
            CmdManager.Instance.GMCmd("⌚😈\ud83d\ude08😇\ud83d\ude07🍚🍖🦋🉑☀️🌹💞💰🍪", "加入周");

            CmdManager.Instance.GMCmd("青柠薄荷糖", "加入赵");
            CmdManager.Instance.GMCmd("星河漫游者", "加入周");
            CmdManager.Instance.GMCmd("🍚ℋღ浪漫🌹💞࿐", "加入周");
            CmdManager.Instance.GMCmd("ℋღ浪漫\ud83c\udf39\ud83d\udc9e࿐", "加入周");

            CmdManager.Instance.GMCmd("🍚零度冰川", "加入钱");
            CmdManager.Instance.GMCmd("暗夜猎手", "加入孙");
            CmdManager.Instance.GMCmd("奶盖兔叽", "加入吴");
            CmdManager.Instance.GMCmd("桃桃乌龙茶", "加入王");
            CmdManager.Instance.GMCmd("秃头少女自救指南", "加入张");
        }

        [BoxGroup("GM")]
        [Button("GM测试聊天指令")]
        public void GmTestChat(int maxNum = 10)
        {
            List<string> msgs = new() { "变形", "查询", "恢复", "还原" };
            for (int i = 1; i <= maxNum; i++)
            {
                string name = "lzz" + i;
                // CmdManager.Instance.GMCmd(name, msgs[Random.Range(0, msgs.Count)]);
                CmdManager.Instance.GMCmd(name, $"加入{(TextName)Random.Range(1, 10)}");
            }

            // for (int i = 0; i < 10; i++)
            // {
            //     CmdManager.Instance.GMCmd("lz001", "16");
            // }
        }

        // [Button("GM修复数据")]   //  比较重的修复，一般不显示怕误触，会修改整个排行榜玩家数据
        public async UniTask Test()
        {
            // test1. all       // 月  score   -->  ItemId = GameConst.MonthScore
            // List<RankDataRet> DatRankList = await DataManager.GetRankIndexInfo(RankType.MonthRank, 0,-1);

            // test2. one
            List<RankDataRet> DatRankList =
                await DataManager.GetRankIndexInfo(RankType.WeekRank, new string[] { "lixin" });

            List<CombatResultData> CombatResultDataList = new();
            var scenInfoComp = SceneHelper.GetSceneInfoComponent();
            // TODO 完善sdk参数上报
            var param = new Dictionary<string, object>
            {
                { "GameName", TotalConfigManager.ConfigManager.ConstConfigCategory.GameName },
                { "RoomId", scenInfoComp.RoomId }, // 房间Id - 会变化新建开播
                { "OpenId", scenInfoComp.AnchorOpenId }, // 主播Id
                { "CombatResultDataList", CombatResultDataList }
            };

            foreach (RankDataRet oneRankData in DatRankList)
            {
                var SendItemDataList = new List<BagData>();

                // 需要修改的背包 Id:Num
                SendItemDataList.Add(new BagData()
                {
                    ItemId = GameConst.FansId,
                    ItemNum = 3000,
                });

                CombatResultData oneData = new CombatResultData()
                {
                    PlayerId = oneRankData.PlayerId,
                    Nickname = oneRankData.Nickname,
                    AvatarUrl = oneRankData.AvatarUrl,
                    RewardDataList = SendItemDataList,
                    Index = 0,
                };

                CombatResultDataList.Add(oneData);

                // 额外检测
                Debug.Log(
                    $"{oneRankData.PlayerId} | {oneRankData.Nickname} | ItemId:{GameConst.MonthScore} RankScore:{oneRankData.Score}");
            }

            var resp = await DataManager.AsyncSendPost(GameConst.Url.Post_BattleResult, body: param);
            Debug.Log("======OKKKKK======");
            Debug.Log(resp);
        }

        // [BoxGroup("GM")]
        // [Button("常量测试")]
        public async UniTask ConstTest()
        {
            var fileName = "ConstConfig.json";
            string fullPath = Path.Combine(Application.streamingAssetsPath, fileName);
            UnityWebRequest request = UnityWebRequest.Get(fullPath);
            await request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                // 获取文件内容
                string jsonContent = request.downloadHandler.text;
                Debug.Log("成功加载JSON文件:\n" + jsonContent);

                var data = JsonUtility.FromJson<ConstConfigCategory>(jsonContent);
            }
            // lixin: 实际使用案例 在 TotalConfigManager.CheckConstConfig() 
        }

        // [Button("敏感词测试")]
        public void SensitiveWorldsTest()
        {
            CmdManager.Instance.GetFilter("臭傻逼", true);
            CmdManager.Instance.GetFilter("台湾独立", true);
            CmdManager.Instance.GetFilter("黑奴贸易", true);
            CmdManager.Instance.GetFilter("小熊维尼", true);
            CmdManager.Instance.GetFilter("大纪元", true);
            CmdManager.Instance.GetFilter("刁近平习包子", true);
            CmdManager.Instance.GetFilter("翠", true);
            CmdManager.Instance.GetFilter("六四 坦克人", true);
            CmdManager.Instance.GetFilter("法轮大法", true);
            CmdManager.Instance.GetFilter("嫩爹灵车", true);
        }

        // [BoxGroup("GM")]
        // [Button("测试震屏")]
        public async UniTask TestDoCameraShake(float duration, float strength, int vibrato)
        {
            CameraHelper.DoCameraShake(duration, strength, vibrato);
        }

        // [Button("测试上结算时间")]
        public async UniTask TestTime()
        {
            long StartTime = 1767196800000;
            DateTime dt = TimeHelper.Time2DateTimeMs(StartTime);
            DateTime lastDt = dt.AddMilliseconds(-1);
            Debug.Log($"{dt} -1  ==> {lastDt.Month} , {(lastDt.Day <= 15 ? "上" : "下")}");
        }

        [Button("测试组刷")]
        public void TestGroupGifts(string playerId = "lzz",
            string giftId = "PJ0FFeaDzXUreuUBZH6Hs+b56Jh0tQjrq0bIrrlZmv13GSAL9Q1hf59fjGk=", int num = 10)
        {
            CmdManager.Instance.SDKMessageGift(playerId, giftId, num, 0);
        }

        [Button("退出车队")]
        public void ExitCar(string playerId = "lzz")
        {
            EventsManager.BroadCast(GameEnum.PlayerExitCar, playerId);
        }
#endif
    }
}