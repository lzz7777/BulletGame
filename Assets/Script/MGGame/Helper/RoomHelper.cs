using System.Collections.Generic;
using System.Linq;
using cfg;
using cfg.Fight;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace XN
{
    public static class RoomHelper
    {
        public static Entity GetRoomUnit() =>
            EntityManager.Instance.GetEntityById(RoomManager.Instance.RoomUnitId);

        public static RoomInfoComponent GetRoomInfoComponent() => GetRoomUnit().GetComponent<RoomInfoComponent>();

        public static FightRoomConfig GetFightRoomConfig() => GetRoomInfoComponent().GetFightRoomConfig();

        public static List<long> GetCars() => GetRoomInfoComponent().CarIds;

        public static Dictionary<string, long> GetPlayers() => GetRoomInfoComponent().PlayerIds;

        /// <summary>
        /// 获取所有加入成功游戏的 本局人员ID
        /// </summary>
        /// <returns></returns>
        public static string[] GetPlayerIdsInCar() => GetRoomInfoComponent().PlayerIds.Keys
            .Where(playerId => GetRoomInfoComponent().GetPlayerInfoComponent(playerId).CarId != 0).ToArray();

        public static Dictionary<string, UserInfo> GetUserInfos() => GetRoomInfoComponent().UserInfos;

        /// <summary>
        /// 获取车辆排名
        /// </summary>
        /// <param name="carId"></param>
        /// <returns></returns>
        public static int GetCarRank(long carId)
        {
            return GetRoomInfoComponent().CarRankDic[carId];
        }

        /// <summary>
        /// 油桶助力
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="carId"></param>
        public static async UniTask DoCarHelp(string playerId, long carId)
        {
            var carViewComp = EntityManager.Instance.GetEntityById(carId).GetComponent<CarViewComponent>();
            if (carViewComp == null)
                return;

            var targetRt =
                carViewComp.ViewCarInfoItem
                    .transform as RectTransform;
            var go = await ObjectPoolManager.Instance.GetFromPool<ViewCarHelpItem>(targetRt);
            var item = go.GetComponent<ViewCarHelpItem>();
            item.OnRefresh(new ViewCarHelpItemData() { PlayerId = playerId });

            var goRt = go.transform as RectTransform;
            var targetY = -50;
            goRt.anchoredPosition = new Vector2(-1000, targetY);

            goRt.DOAnchorPosX(0, 1).OnComplete(() => { ObjectPoolManager.Instance.ReturnToPool(go); });
        }

        /// <summary>
        /// 触发参数变化ui
        /// </summary>
        public static void DoBuffChangeUI(long carId, ChangeType changeType, int changeValue = 0)
        {
            switch (changeType)
            {
                case ChangeType.MileageAddPct:
                case ChangeType.MileageDelPct:
                case ChangeType.MileageAddValue:
                case ChangeType.MileageDelValue:
                    DoAttributeFloat(carId, changeType, changeValue);
                    break;
                case ChangeType.ShieldAdd:
                case ChangeType.ShieldDel:
                    EntityManager.Instance.GetEntityById(carId).GetComponent<CarViewComponent>()?.ViewCarInfoItem
                        .RefreshShield();
                    break;
            }
        }

        /// <summary>
        /// 车辆飘字
        /// </summary>
        /// <param name="carId"></param>
        /// <param name="changeType"></param>
        /// <param name="changeValue"></param>
        public static async UniTask DoAttributeFloat(long carId, ChangeType changeType, int changeValue)
        {
            var carViewComp = EntityManager.Instance.GetEntityById(carId).GetComponent<CarViewComponent>();
            if (carViewComp == null)
                return;

            var targetRt =
                carViewComp.ViewCarInfoItem
                    .transform as RectTransform;
            var go = await ObjectPoolManager.Instance.GetFromPool<ViewAttributeFloatItem>(targetRt);
            if (!go)
            {
                return;
            }

            (go.transform as RectTransform).anchoredPosition = new Vector2(0, -100);
            var item = go.GetComponent<ViewAttributeFloatItem>();
            item.OnRefresh(new ViewAttributeFloatItemData()
            {
                ChangeType = changeType,
                ChangeValue = changeValue,
            });

            await UniTask.Delay(2000);
            ObjectPoolManager.Instance.ReturnToPool(go);
        }

        /// <summary>
        /// 添加跑马灯信息
        /// </summary>
        public static async UniTask AddTicker(string content, bool isShowOil = false)
        {
            bool isOpen = UIManager.Instance.JudgeWindowOpen(out ViewTicker viewTicker);
            if (!isOpen)
            {
                viewTicker = (ViewTicker)await UIManager.Instance.OpenWindow<ViewTicker>();
            }

            viewTicker.AddData(new ViewTickerItemData()
            {
                Content = content,
                IsShowOil = isShowOil,
            });
        }

        /// <summary>
        /// 创建车队
        /// </summary>
        /// <param name="startPos"></param>
        /// <param name="group"></param>
        /// <param name="name"></param>
        /// <param name="mileage"></param>
        /// <param name="deviceId"></param>
        public static async UniTask<Entity> CreateCar(Vector2 startPos, int group, string name, float mileage = 0,
            int deviceId = 0)
        {
            var roomUnit = GetRoomUnit();
            var roomInfoComp = GetRoomInfoComponent();
            var roomConf = GetFightRoomConfig();

            var carEntity = roomUnit.AddChild(EntityType.Character);
            roomInfoComp.CarIds.Add(carEntity.Id);
            roomInfoComp.CarRankDic[carEntity.Id] = group;

            //创建
            var carInfoComp = carEntity.AddComponent<CarInfoComponent>();
            carInfoComp.Group = group;
            carInfoComp.Line = 1;
            carInfoComp.Name = name;
            carInfoComp.Speed = roomInfoComp.GetScenePlanning().BaseSpeed;
            carInfoComp.ChangeLineDelay = UnityEngine.Random.Range(1,
                TotalConfigManager.ConfigManager.ConstConfigCategory.ChangeRoadRandom + 1);
            carInfoComp.DeviceId = deviceId != 0 ? deviceId : roomConf.BaseDevice[group];
            carInfoComp.Mileage = mileage;

            var carPositionComp = carEntity.AddComponent<CarPositionComponent>();
            carPositionComp.X = startPos.x;
            carPositionComp.Y = startPos.y;

            //view层
            if (group <= 6)
            {
                var carViewComp = carEntity.AddComponent<CarViewComponent>();
                await carViewComp.InitSystem();
            }

            return carEntity;
        }

        /// <summary>
        /// 移除车队
        /// </summary>
        /// <param name="carId"></param>
        public static void RemoveCar(long carId)
        {
            var roomInfoComp = GetRoomInfoComponent();
            roomInfoComp.CarIds.Remove(carId);
            roomInfoComp.CarRankDic.Remove(carId);
            var carInfoComp = EntityManager.Instance.GetEntityById(carId).GetComponent<CarInfoComponent>();
            carInfoComp.IsDiscard = true;
        }

        /// <summary>
        /// 落座表现
        /// </summary>
        public static async UniTask DoTakeSeatView(string playerId)
        {
            var playerInfoComp = GetRoomInfoComponent().GetPlayerInfoComponent(playerId);
            if (playerInfoComp?.CarId == 0)
            {
                return;
            }

            var carUnit = EntityManager.Instance.GetEntityById(playerInfoComp.CarId);

            if (carUnit.GetComponent(out CarViewComponent carViewComp))
            {
                carViewComp.ViewCarInfoItem.DoTakeSeatView(playerId);
                await UniTask.Delay(1200);
                carViewComp.PlayCarTint();
                playerInfoComp.FinishTakeSeat();
                carViewComp.ViewCarInfoItem?.RefreshMembers();
            }
            else
            {
                playerInfoComp.FinishTakeSeat();
            }
        }

        /// <summary>
        /// 车辆里程排序
        /// </summary>
        public static void CarsSort()
        {
            var roomInfoComp = GetRoomInfoComponent();
            var carIds = roomInfoComp.CarIds;

            carIds.Sort((a, b) =>
            {
                var carInfoComp1 = EntityManager.Instance.GetEntityById(a).GetComponent<CarInfoComponent>();
                var carInfoComp2 = EntityManager.Instance.GetEntityById(b).GetComponent<CarInfoComponent>();

                if (!Mathf.Approximately(carInfoComp1.Mileage, carInfoComp2.Mileage))
                {
                    return carInfoComp2.Mileage.CompareTo(carInfoComp1.Mileage);
                }

                if (carInfoComp2.PlayerIds.Count != carInfoComp1.PlayerIds.Count)
                {
                    return carInfoComp2.PlayerIds.Count.CompareTo(carInfoComp1.PlayerIds.Count);
                }

                return carInfoComp1.Entity.Id.CompareTo(carInfoComp2.Entity.Id);
            });

            for (int i = 0; i < carIds.Count; i++)
            {
                roomInfoComp.CarRankDic[carIds[i]] = i;
            }
        }

        /// <summary>
        /// 获取车辆房间随机皮肤
        /// </summary>
        public static int GetRandomDevice()
        {
            var devices = GetFightRoomConfig().BaseDevice;
            int deviceId = devices[UnityEngine.Random.Range(0, devices.Count)];
            return deviceId;
        }

        /// <summary>
        /// 获取加入车队
        /// </summary>
        /// <returns></returns>
        public static long GetJoinCar()
        {
            //1.在场车队，优先从没人的车队里面随机加入
            //2.车队全满的情况下，再全车队随机
            var carIds = GetCars();

            List<long> nobodyCars = new();

            foreach (var carId in carIds)
            {
                var carInfoComp = EntityManager.Instance.GetEntityById(carId).GetComponent<CarInfoComponent>();
                if (carInfoComp.PlayerIds.Count == 0)
                {
                    nobodyCars.Add(carId);
                }
            }

            long targetCarId;

            if (nobodyCars.Count > 0)
            {
                targetCarId = nobodyCars[Random.Range(0, nobodyCars.Count)];
            }
            else
            {
                targetCarId = carIds[Random.Range(0, carIds.Count)];
            }

            return targetCarId;
        }

        /// <summary>
        /// 获取有人车队
        /// </summary>
        /// <returns></returns>
        public static List<long> GetPlayerCars()
        {
            var carIds = new List<long>();

            foreach (var carId in GetCars())
            {
                if (EntityManager.Instance.GetEntityById(carId).GetComponent<CarInfoComponent>().PlayerIds.Count == 0)
                    continue;

                carIds.Add(carId);
            }

            return carIds;
        }

        #region UserInfo

        public static void UpdateUserInfo(string playerId, string nickname, string avatarUrl)
        {
            // Post ...
            var roomInfoComp = GetRoomInfoComponent();
            if (roomInfoComp.UserInfos.TryGetValue(playerId, out var userInfo))
            {
                if (userInfo.AvatarUrl != null && userInfo.AvatarUrl != avatarUrl)
                {
                    Debug.Log($"{playerId} : 换头像咯~~~ {avatarUrl}");
                    userInfo.AvatarUrl = avatarUrl;
                }

                if (userInfo.Nickname != null && userInfo.Nickname != nickname)
                {
                    Debug.Log($"{playerId} :改名咯~~~ {nickname}");
                    userInfo.Nickname = nickname;
                }
            }
            else
            {
                userInfo = new UserInfo()
                {
                    OpenId = playerId,
                    Nickname = nickname,
                    AvatarUrl = avatarUrl,
                };
                if (!roomInfoComp.UserInfos.TryAdd(playerId, userInfo))
                {
                    Debug.LogError($"{playerId} 新加入数据失败");
                }
            }
        }


        public static UserInfo GetUserInfo(string playerId)
        {
            var roomInfoComp = GetRoomInfoComponent();
            if (!roomInfoComp.UserInfos.TryGetValue(playerId, out var userInfo))
            {
                userInfo = new UserInfo()
                {
                    OpenId = playerId,
                    Nickname = playerId,
                    AvatarUrl = ResHelper.GetAvatarUrl(),
                    IsEditor = true,
                };
                roomInfoComp.UserInfos.Add(playerId, userInfo);
            }

            return userInfo;
        }

        #endregion
    }
}