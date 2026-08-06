using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using cfg;
using cfg.Global;
using cfg.Item;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using XN.Tools.SensitiveWord;

namespace XN
{
    public class InputCmdData
    {
        public string PlayerId;
        public int InputId;
        public string Content;
        public bool Using;
        public bool IsDone;
    }
    
    public class CmdManager : MonoSingleton<CmdManager>
    {
        private Dictionary<string, Queue<InputCmdData>> _inputData = new();
        
        /// <summary>
        /// 保存开赛前玩家道具指令
        /// </summary>
        private Dictionary<string, Queue<InputCmdData>> _saveInputData = new();
        
        private HashSet<string> _needDequeue = new();

        private InputCmdData _tempCmdData;
        
        public bool IsInitialized { get; private set; }
        protected override async void OnInit()
        {
            await UniTask.WaitUntil(() => YooAssetManager.Instance.IsInitialized);
            await UniTask.WaitUntil(() => TotalConfigManager.Instance.IsLoadOver);
            InitSensitiveWord(); // 打开敏感词库
            IsInitialized = true;
        }

        protected override void OnRemove()
        {
        }

        private void Update()
        {
            if (_inputData.Count <= 0) return;
            
            foreach (var (pid, queue) in _inputData)
            {
                if (queue.Count <= 0)
                {
                    continue;
                }
                
                _tempCmdData = queue.Peek();
                if (!_tempCmdData.Using)
                {
                    _tempCmdData.Using = true;
                    DoCmd(_tempCmdData);
                    break;
                }

                if (!_tempCmdData.IsDone) continue;

                _needDequeue.Add(pid);
                break;
            }

            if (_needDequeue.Count > 0)
            {
                foreach (var pid in _needDequeue)
                {
                    _inputData[pid].Dequeue();
                }
            }
            
            _needDequeue.Clear();
        }

        public void ClearData()
        {
            _inputData.Clear();
            _needDequeue.Clear();
            _saveInputData.Clear();
        }

        /// <summary>
        /// 保存的指令放入执行指令队列中
        /// </summary>
        public void UpdateInputData()
        {
            foreach (var (playerId, saveQueue) in _saveInputData)
            {
                _inputData.TryAdd(playerId, new());
                _inputData[playerId] = CalculateHelper.MergeQueues(_inputData[playerId], saveQueue);
            }
        }

        /// <summary>
        /// gm
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="content"></param>
        public void GMCmd(string playerId, string content)
        {
            if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(content))
            {
                return;
            }
            
            if (UIManager.Instance.GameModel == GameModel.Debug)
            {
                if (playerId == "测试")
                {
                    var strs = content.Split(":");
                    if (strs.Length != 3)
                    {
                        return;
                    }

                    List<int> args = new();
                    foreach (var str in strs)
                    {
                        if (!int.TryParse(str, out int argInt))
                        {
                            return;
                        }
                    
                        args.Add(argInt);
                    }

                    Main.GmTestAll(args[0], args[1], args[2]);
                    return;
                }
                
                if (playerId == "测试脚本")
                {
                    GmTestCmd(content);
                    return;
                }

                if (playerId == "FPS")
                {
                    if (gameObject.TryGetComponent<FPSView>(out _))
                    {
                        gameObject.AddComponent<FPSView>();
                    }
                    return;
                }

                if (playerId == "MR")
                {
                    GameConst.IsOpenMaximumRange = content == "1";
                    Debug.Log($"最大里程数开关:{GameConst.IsOpenMaximumRange}");
                    return;
                }
            }
            
            if (string.IsNullOrEmpty(playerId))
            {
                return;
            }
            
            var inputConf = ParseCmd(content);
            if (inputConf != null)
            {
                AddCmd(playerId, inputConf.InputId, content);
                return;
            }
            
            if (int.TryParse(content, out int inputId) && TotalConfigManager.ConfigManager.InputIndexConfigCategory.GetOrDefault(inputId) != null)
            {
                AddCmd(playerId, inputId);
                return;
            }
        }
        
        [System.Serializable]
        private class TestCmdData
        {
            public int Delay = 0;
            public string PlayerId;
            public string Content;
        }
        
        [System.Serializable]
        private class TestCmdDatas
        {
            public List<TestCmdData> Datas = new();
        }
        
        private async UniTask GmTestCmd(string fileName = "TestCmd1")
        {
            fileName = $"TestCmd/{fileName}.json";
            string fullPath = Path.Combine(Application.streamingAssetsPath, fileName);
            using (UnityWebRequest request = UnityWebRequest.Get(fullPath))
            {
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    // 获取文件内容
                    string jsonContent = request.downloadHandler.text;
                    Debug.Log("成功加载JSON文件:\n" + jsonContent);

                    var datas = JsonUtility.FromJson<TestCmdDatas>(jsonContent);
                    foreach (var data in datas.Datas)
                    {
                        await UniTask.Delay(data.Delay);
                        GMCmd(data.PlayerId, data.Content);
                    }
                }
            }
        }
        
        /// <summary>
        /// 处理聊天
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="content"></param>
        public void ChatCmd(string playerId, string content)
        {
            var inputConf = ParseCmd(content);
            if (inputConf == null)
            {
                return;
            }

            AddCmd(playerId, inputConf.InputId, content);
        }

        /// <summary>
        /// 获取指令
        /// </summary>
        private InputIndexConfig ParseCmd(string content)
        {
            foreach (var conf in TotalConfigManager.ConfigManager.InputIndexConfigCategory.InputIndexConfigDic[InteractiveID.弹幕])
            {
                if (conf.InputStr == content)
                {
                    return conf;
                }
            }
            
            foreach (var conf in TotalConfigManager.ConfigManager.InputIndexConfigCategory.InputIndexConfigDic[InteractiveID.模糊匹配弹幕])
            {
                if (content.Contains(conf.InputStr))
                {
                    return conf;
                }
            }
            
            return null;
        }

        private void AddCmd(string playerId, int inputId, string content = "")
        {
            if (!GameStateCtrl.IsGameAllState)
            {
                return;
            }

            
            //开赛前保存道具指令
            var inputConf = TotalConfigManager.ConfigManager.InputIndexConfigCategory.GetOrDefault(inputId);
            if (GameStateCtrl.State < MGGameState.游戏开始 && (inputConf?.IsGift ?? false))
            {
                StartGamePlayerJoin(playerId, inputId);
                
                _saveInputData.TryAdd(playerId, new Queue<InputCmdData>());
                _saveInputData[playerId].Enqueue(new InputCmdData()
                {
                    PlayerId = playerId,
                    InputId = inputId,
                    Content = content
                });
                return;
            }
            
            _inputData.TryAdd(playerId, new Queue<InputCmdData>());
            _inputData[playerId].Enqueue(new InputCmdData()
            {
                PlayerId = playerId,
                InputId = inputId,
                Content = content
            });
        }

        /// <summary>
        /// 开赛前使用道具入座
        /// </summary>
        private async UniTask StartGamePlayerJoin(string playerId, int inputId)
        {
            //玩家入座
            //自动加入逻辑
            var roomInfoComp = RoomHelper.GetRoomInfoComponent();
            await roomInfoComp.CheckAddPlayer(playerId);
            roomInfoComp.CheckPlayerAddCar(playerId, inputId);
        }

        /// <summary>
        /// 执行指令
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="inputId"></param>
        /// <param name="content"></param>
        private async UniTask DoCmd(InputCmdData inputCmdData)
        {
            if (!GameStateCtrl.IsGameAllState)
            {
                return;
            }
            
            string playerId = inputCmdData.PlayerId;
            int inputId = inputCmdData.InputId;
            string content = inputCmdData.Content;
            
            var roomInfoComp = RoomHelper.GetRoomInfoComponent();
            if (roomInfoComp == null)
            {
                inputCmdData.IsDone = true;
                return;
            }
            
            await roomInfoComp.CheckAddPlayer(playerId);
            
            var inputIndexConf = TotalConfigManager.ConfigManager.InputIndexConfigCategory.GetOrDefault(inputId);
            if (inputIndexConf == null)
            {
                Debug.LogError($"inputIndexConf: {inputId} is not found");
                inputCmdData.IsDone = true;
                return;
            }

            var playerInfoComp = roomInfoComp.GetPlayerInfoComponent(playerId);
            //检查是否能执行指令
            if (!playerInfoComp.CheckDoCmd(inputId))
            {
                inputCmdData.IsDone = true;
                return;
            }
            
            //判断是否自动加入，游戏开始才能生效
            if (inputIndexConf.AutoJoinRoom && GameStateCtrl.IsGameStart)
            {
                //自动加入逻辑
                await roomInfoComp.CheckPlayerAddCar(playerId, inputId);
            }
            
            switch (inputIndexConf.Cmd)
            {
                case ECmd.None:
                    break;
                case ECmd.恢复:
                case ECmd.更换:
                case ECmd.还原:
                case ECmd.变形:
                    DoChangeSkin(inputId, playerId, content);
                    break;
                case ECmd.查询:
                    CheckOilDrum(playerId);
                    break;
                case ECmd.兑换:
                    DoExchange(inputId, playerId, content);
                    break;
                case ECmd.加:
                case ECmd.加入:
                    DoJoinCar(inputId, playerId, content);
                    break;
                case ECmd.切换:
                    DoSwitch(inputId, playerId, content);
                    break;
                case ECmd.点赞:
                    BuffHelper.DoBuff(inputId, playerId, (int)inputIndexConf.Cmd);
                    break;
            }
            
            if (inputIndexConf.IsGift)
            {
                BuffHelper.DoBuff(inputId, playerId, (int)inputIndexConf.Cmd);
            }
            
            //道具显示，游戏开始才能生效
            if (inputIndexConf.IsGift && GameStateCtrl.IsGameStart)
            {
                EventsManager.BroadCast(GameEnum.ViewBattleMainItemShowEvent, new ViewItemShowNodeData()
                {
                    PlayerId = playerId,
                    InputId = inputId,
                });
            }
            
            inputCmdData.IsDone = true;
        }

        /// <summary>
        /// 加入车队
        /// </summary>
        /// <param name="inputId"></param>
        /// <param name="playerId"></param>
        /// <param name="content"></param>
        private async UniTask DoJoinCar(int inputId, string playerId, string content)
        {
            //判断玩家是否有车队
            var playerInfoComp = RoomHelper.GetRoomInfoComponent().GetPlayerInfoComponent(playerId);
            if (playerInfoComp?.CarId != 0)
            {
                return;
            }
            
            string targetName = "";
            var roomConf = RoomHelper.GetFightRoomConfig();
            var roomInfoComp = RoomHelper.GetRoomInfoComponent();

            //精准匹配
            var inputIndexConf = TotalConfigManager.ConfigManager.InputIndexConfigCategory.GetOrDefault(inputId);
            string replaceStr = inputIndexConf.Cmd.ToString();
            var match = content.Substring(0, replaceStr.Length);
            if (match != replaceStr)
            {
                return;
            }
            
            //匹配名字
            string tempName = content.Substring(replaceStr.Length);
            switch (roomConf.RoomType)
            {
                case FightRoomType.TextRoom:
                    if (SensitiveManager.IsWhiteName(tempName))
                    {
                        targetName = tempName;
                    } else if (Enum.TryParse(tempName, true, out TextName textName) && Enum.IsDefined(typeof(TextName), tempName))
                    {
                        targetName = textName.ToString();
                    }
                    
                    break;
                case FightRoomType.ZodiacRoom:
                    if (Enum.TryParse(tempName, true, out ZodiacName zodiacName) && Enum.IsDefined(typeof(ZodiacName), tempName))
                    {
                        targetName = zodiacName.ToString();
                    }
                    
                    break;
                case FightRoomType.FreeRoom:
                    if (GetStringLength(tempName) > 8)
                    {
                        string tickerContent = $"<color=#00DCFF>{playerInfoComp.Name}</color> 创建 <color=#00FF00>{targetName}</color> 车队失败：词汇超出四字";
                        RoomHelper.AddTicker(tickerContent);
                        return;
                    }
                    string currName = GetFilter(tempName);

                    if (!currName.Equals(tempName))
                    {
                        string tickerContent = $"<color=#00DCFF>{playerInfoComp.Name}</color> 创建 <color=#00FF00>{targetName}</color> 车队失败：请更换正常词汇";
                        RoomHelper.AddTicker(tickerContent);
                        return;
                    }

                    targetName = currName;
                    break;
            }

            if (string.IsNullOrEmpty(targetName))
            {
                if (string.IsNullOrEmpty(tempName))
                {
                    //加，加入匹配到了，内容为空，走自动加入逻辑
                    roomInfoComp.CheckPlayerAddCar(playerId, inputId);
                }
                
                return;
            }

            long targetCarId = 0;
            
            //找到名字车队
            foreach (var carId in RoomHelper.GetCars())
            {
                var carInfoComp = EntityManager.Instance.GetEntityById(carId).GetComponent<CarInfoComponent>();
                if (carInfoComp.Name == targetName)
                {
                    targetCarId = carId;
                    break;
                }
            }

            if (targetCarId != 0)
            {
                roomInfoComp.CheckPlayerAddCar(playerId, inputId, targetCarId);
                return;
            }
            
            //找不到名称车队，创建新车
            var carIds = RoomHelper.GetCars();
            int targetGaroup = 0;
            float targetMileage = 0;
            float minMileage = -1;
            int targetDeviceId = 0;
            //从前往后找对第一个空车
            for (int i = 0; i < carIds.Count; i++)
            {
                var carId = carIds[i];
                var carInfoComp = EntityManager.Instance.GetEntityById(carId).GetComponent<CarInfoComponent>();
                if (carInfoComp.PlayerIds.Count == 0)
                {
                    targetCarId = carId;
                    break;
                }

                if (Mathf.Approximately(minMileage, -1) || carInfoComp.Mileage < minMileage)
                {
                    minMileage = carInfoComp.Mileage;
                }
            }

            //判断是否达到最大车辆数量上限
            int maxCarNum = TotalConfigManager.ConfigManager.ConstConfigCategory.MaxPlayer;
            if (targetCarId != 0)
            {
                //获取旧车队皮肤
                var targetCarInfoComp = EntityManager.Instance.GetEntityById(targetCarId).GetComponent<CarInfoComponent>();
                targetDeviceId = targetCarInfoComp.DeviceId;
                targetGaroup = targetCarInfoComp.Group;
                targetMileage = targetCarInfoComp.Mileage;
                
                //移除旧车队
                RoomHelper.RemoveCar(targetCarId);
            }
            else if (carIds.Count < maxCarNum)
            {
                //场上车满了，而且没有达到最大数量
                targetMileage = minMileage;
                targetGaroup = carIds.Count;
                
                //随机一个皮肤
                targetDeviceId = RoomHelper.GetRandomDevice();
            }
            else
            {
                //没找到车，显示飘字: XXX 创建 YY 车队失败：参赛车队已满
                string tickerContent = $"<color=#00DCFF>{playerInfoComp.Name}</color> 创建 <color=#00FF00>{targetName}</color> 车队失败：参赛车队已满";
                RoomHelper.AddTicker(tickerContent);
                return;
            }
            
            //创建新车队
            var startPos = RoomManager.Instance.GroupLinePos[targetGaroup][1];

            if (GameStateCtrl.IsGameStart)
            {
                startPos = new Vector2(-6, startPos.y);
            }

            var carEntity = await RoomHelper.CreateCar(startPos, targetGaroup, targetName, targetMileage, targetDeviceId);
            
            //刷新排名
            RoomHelper.CarsSort();
            
            var carViewComp = carEntity.GetComponent<CarViewComponent>();
            //刷新特效
            carViewComp.RefreshEffect();
            //刷新灯带
            carViewComp.RefreshTrackLight();
            roomInfoComp.CheckPlayerAddCar(playerId, inputId, carEntity.Id);
        }

        /// <summary>
        /// 换皮指令
        /// </summary>
        /// <param name="inputId"></param>
        /// <param name="playerId"></param>
        /// <param name="content"></param>
        public async UniTask DoChangeSkin(int inputId, string playerId, string content)
        {
            var inputConf = TotalConfigManager.ConfigManager.InputIndexConfigCategory.Get(inputId);
            
            int value = 0;

            if (inputConf.Interactive == InteractiveID.模糊匹配弹幕)
            {
                foreach (var extendedInstruction in inputConf.ExtendedInstructions)
                {
                    if (content.Equals($"{inputConf.Cmd}{extendedInstruction}"))
                    {
                        value = int.Parse(extendedInstruction);
                        break;
                    }
                }
            }

            if (value == 0 && inputConf.ExtendedInstructions.Count != 0)
            {
                return;
            }
            
            var response = await PlayerMessage.SendCmdOperationRequest(playerId, inputConf.Cmd, value);
            if (response.Code != 0)
            {
                Debug.LogError($"SendCmdOperationRequest:{response.Msg}");
                return;
            }
            
            var playerInfoComp = RoomHelper.GetRoomInfoComponent().GetPlayerInfoComponent(playerId);
            playerInfoComp.SetSkinData(response.Data.SkinId, response.Data.EffectId);
            
            //给车队推消息换皮
            playerInfoComp.RefreshCarSkin();
        }

        /// <summary>
        /// 兑换
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="content"></param>
        public async UniTask DoExchange(int inputId, string playerId, string content)
        {
            var inputConf = TotalConfigManager.ConfigManager.InputIndexConfigCategory.Get(inputId);

            int value = 0;
            foreach (var extendedInstruction in inputConf.ExtendedInstructions)
            {
                if (content.Contains($"兑换{extendedInstruction}"))
                {
                    value = int.Parse(extendedInstruction);
                    break;
                }
            }

            if (value == 0)
            {
                return;
            }

            var response = await PlayerMessage.SendCmdOperationRequest(playerId, ECmd.兑换, value);
            if (response.Code != 0)
            {
                Debug.LogError($"SendCmdOperationRequest:{response.Msg}");
                return;
            }
            
            //更新背包
            var respBagRequest = await PlayerMessage.SendBagRequest(playerId);
            var playerItemComp = RoomHelper.GetRoomInfoComponent().GetPlayerItemComponent(playerId);
            playerItemComp.SetItemData(respBagRequest.BagDataList);
            
            //跑马灯
            var playerInfoComp = RoomHelper.GetRoomInfoComponent().GetPlayerInfoComponent(playerId);
            var storeConf = TotalConfigManager.ConfigManager.StoreConfigCategory.Get(value);
            var storeItemConf = TotalConfigManager.ConfigManager.ItemInfoConfigCategory.Get(storeConf.Goods.ItemId);
            string tickerContent = $"<color=#00DCFF>{playerInfoComp.Name}</color> 成功兑换 <color=#00FF00>{storeItemConf.ItemName}</color>";
            RoomHelper.AddTicker(tickerContent);
        }
        
        /// <summary>
        /// 切换性别
        /// </summary>
        /// <param name="inputId"></param>
        /// <param name="playerId"></param>
        private async UniTask DoSwitch(int inputId, string playerId, string content)
        {
            var playerInfoComp = RoomHelper.GetRoomInfoComponent().GetPlayerInfoComponent(playerId);
            if (playerInfoComp == null)
            {
                return;
            }
            
            var response = await PlayerMessage.SendCmdOperationRequest(playerId, ECmd.切换);
            if (response.Code != 0)
            {
                Debug.LogError($"SendCmdOperationRequest DoSwitch:{response.Msg}");
                return;
            }

            playerInfoComp.SetSex((SexType)response.Data.Value);
        }
        
        /// <summary>
        /// 查询油桶
        /// </summary>
        /// <param name="playerId"></param>
        public async UniTask CheckOilDrum(string playerId)
        {
            var playerUnit = RoomHelper.GetRoomInfoComponent().GetPlayerUnit(playerId);
            var playerInfoComp = playerUnit.GetComponent<PlayerInfoComponent>();
            var playerItemComp = playerUnit.GetComponent<PlayerItemComponent>();
            
            var num = playerItemComp.GetItemNum(GameConst.OilDrum);
            
            //跑马灯
            string content = $"<color=#00DCFF>{playerInfoComp.Name}</color> 拥有 <color=#FCFFB3>x{num}</color>";
            RoomHelper.AddTicker(content, true);
        }

        #region =========== SDKMessage =========== 

        public void SDKMessageUpdateUserInfo(string playerId, string playerNickName, string playerAvatarUrl)
        {
            // 刷新玩家信息
            // 抖音 IUserInfo
            RoomHelper.UpdateUserInfo(playerId,playerNickName, playerAvatarUrl);
        }
        
        /// <summary>
        /// 聊天触发 模糊匹配
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="content"></param>
        /// <param name="timestamp"></param>
        public void SDKMessageChat(string playerId, string content, long timestamp)
        {
            Debug.Log($"SDKMessageChat:{playerId}:{content}  time:{timestamp}");
            ChatCmd(playerId, content);
        }
        
        
        /// <summary>
        /// 点赞
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="content"></param>
        /// <param name="timestamp"></param>
        public void SDKMessageLike(string playerId, int num, long timestamp)
        {
            InputIndexConfigCategory inputIndexCc = TotalConfigManager.ConfigManager.InputIndexConfigCategory;
            Debug.Log($"SDKMessageLike:{playerId}:{num}  time:{timestamp}");
            if (inputIndexCc.InputIndexConfigDic.TryGetValue(InteractiveID.点赞, out var gifts))
            {
                int inputId = gifts.First().InputId;
                Debug.Log($"SDKMessageLike: {playerId}  {inputId}:{num}  time:{timestamp}");
                
                if (num >= 2)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        AddCmd(playerId, inputId);
                    }
                }
                
                // TODO 客户端当前是一条条Add然后解析
                // for (int i = 0; i < num; i++)
                // {
                //     AddCmd(playerId, inputId);
                // }
            }
        }
        
        /// <summary>
        /// 礼物
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="giftId"></param>
        /// <param name="num"></param>
        /// <param name="timestamp"></param>
        public void SDKMessageGift(string playerId, string giftId, int num, long timestamp)
        {
            SignInfoConfigCategory signinfoCc = TotalConfigManager.ConfigManager.SignInfoConfigCategory;
            InputIndexConfigCategory inputIndexCc = TotalConfigManager.ConfigManager.InputIndexConfigCategory;

            Debug.Log($"SDKMessageLike:{playerId}: {giftId}:{num}  time:{timestamp}");
            // var oneGift = signinfoCc.DataList.FirstOrDefault(x => x.Channel == ChannelCmd.DouYin && x.ChannelSignID == giftId);
            signinfoCc.SignInfoConfigDic.TryGetValue((ChannelCmd.DouYin, giftId), out var oneGift);
            if (oneGift == null)
            {
                Debug.LogError($"{ChannelCmd.DouYin} + {giftId} 找不到商品");
                return;
            }
            if (inputIndexCc.InputIndexConfigDic.TryGetValue(oneGift.Gift, out var gifts))
            {
                List<(int, int)> cmdIdAndNumLit = new();
                int count = num;
                for (int i = gifts.Count-1; i >=0; i--)
                {
                    InputIndexConfig oneInput = gifts[i];
                    int repeat = count / oneInput.InputNumber;
                    if (repeat > 0)
                    {
                        count -= repeat * oneInput.InputNumber;
                        cmdIdAndNumLit.Add((oneInput.InputId, repeat));
                    }

                    if (count == 0)
                    {
                        break;
                    }
                }

                if (count > 0)
                {
                    UnityEngine.Debug.LogError($"{oneGift.Gift}  还有 {count} 未解析成功");
                }
                // TODO 客户端当前是一条条Add然后解析
                foreach ((int inputId, int number) in cmdIdAndNumLit)
                {
                    for (int i = 0; i < number; i++)
                    {
                        AddCmd(playerId, inputId);
                    }
                }
            }
            
            EventsManager.BroadCast(GameEnum.GroupBrushGifts, giftId, num);
        }
        
        /// <summary>
        /// 邀请入队
        /// </summary>
        /// <param name="inviterPlayerId"></param>
        /// <param name="secPlayerId"></param>
        /// <param name="timestamp"></param>
        public void SDKMessageInviteFriend(string inviterPlayerId, string secPlayerId, long timestamp)
        {
            var roomInfoComp = RoomHelper.GetRoomInfoComponent();
            var inviPlayerInfoComp = roomInfoComp.GetPlayerInfoComponent(inviterPlayerId);
            var secPlayerInfoComp = roomInfoComp.GetPlayerInfoComponent(secPlayerId);
            
            string inviterName = inviPlayerInfoComp?.Name ?? RoomHelper.GetUserInfo(inviterPlayerId).Nickname;
            string secJionName = secPlayerInfoComp?.Name ?? RoomHelper.GetUserInfo(secPlayerId).Nickname;

            // 1. 邀请者未下场
            if (inviPlayerInfoComp == null || inviPlayerInfoComp.CarId == 0)
            {
                if (secPlayerInfoComp != null && secPlayerInfoComp.CarId != 0)
                {
                    // 邀请者未下场，被邀请者早下场
                    return;
                }
                string stayStr = $"<color=#00DCFF>{inviterName}</color>邀请<color=#00DCFF>{secJionName}加入观赛席";
                RoomHelper.AddTicker(stayStr);
                return;
            }
            // 2. 邀请者下场 + 被邀请者已下场
            if (secPlayerInfoComp !=null && secPlayerInfoComp.CarId != 0)
            {
                return;
            }
            
            //3. 邀请者下场 + 被邀请这未下场
            // 添加玩家入座指令
            var inputId = TotalConfigManager.ConfigManager.InputIndexConfigCategory.ECmdInputIndexConfigDic[ECmd.加][0].InputId;

            var inviCarInfoComp = EntityManager.Instance.GetEntityById(inviPlayerInfoComp.CarId).GetComponent<CarInfoComponent>();
            string content = $"加{inviCarInfoComp.Name}";
            _inputData.TryAdd(secPlayerId, new Queue<InputCmdData>());
            _inputData[secPlayerId].Enqueue(new InputCmdData()
            {
                PlayerId = secPlayerId,
                InputId = inputId,
                Content = content
            });
            
            string jionStr = $"<color=#00DCFF>{inviterName}</color>邀请<color=#00DCFF>{secJionName}</color>加入<color=#00FF00>{inviCarInfoComp.Name}队</color>";
            RoomHelper.AddTicker(jionStr);
            RoomHelper.AddTicker(jionStr);
        }
        
        #endregion 
        
        private int GetStringLength(string str)
        {
            if (string.IsNullOrEmpty(str))
                return 0;
            int len = 0;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] <= 127)
                    len += 1;
                else
                    len += 2;
            }
            return len;
        }

        #region ============== 敏感词 ===============

        private AhoCorasick filter = new ();
        
        public void InitSensitiveWord(HashSet<string> whiteNames = null)
        {
            filter = new AhoCorasick();
            
            ShieldedLibraryConfigCategory sensitiveWorldCc = TotalConfigManager.ConfigManager.ShieldedLibraryConfigCategory;
            if (whiteNames is { Count: > 0 })
            {
                filter.LoadSensitiveWorlds(sensitiveWorldCc.DataList.Where(x=>!whiteNames.Contains(x.Word)).Select(x=>x.Word));    
            }
            else
            {
                filter.LoadSensitiveWorlds(sensitiveWorldCc.DataList.Select(x=>x.Word));    
            }
            
            // 构建失败指针
            filter.BuildFailureLinks();
        }

        public string GetFilter(string msg, bool openLog = false)
        {
            if(openLog) Debug.Log($"敏感词过滤: {msg} ===> {filter.Filter(msg)}");
            return filter.Filter(msg);
        }
        
        #endregion

    }
}