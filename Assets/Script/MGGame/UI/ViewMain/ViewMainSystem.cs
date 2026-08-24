using System;
using System.Collections.Generic;
using System.Linq;
using cfg;
using cfg.Fight;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine.UI;

namespace XN
{
    public static class ViewMainSystem
    {
        #region CircleLife

        public static void OnOpenSystem(this ViewMain self, UIWindowData uIWindowData)
        {
            self.currRoomType = (FightRoomType)uIWindowData.IntArgs1;
            self.currRoomId = 2;
            Debug.Log($"Room OnOpenSystem  --- {self.currRoomType} --- {self.currRoomId}");
            switch (self.currRoomType)
            {
                case FightRoomType.TextRoom:
                    self.UITextNameToggle.isOn = true;
                    break;
                case FightRoomType.ZodiacRoom:
                    self.UIZodiacNameToggle.isOn = true;
                    break;
                case FightRoomType.FreeRoom:
                    self.UIFreeNameToggle.isOn = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            self.ToggleDescItems[self.currRoomId].GetComponent<Toggle>().isOn = true;
            EventsManager.BroadCast(GameEnum.TopSettingRefreshEvent, "ViewMain");
            self.UIMapNodeButton.gameObject.SetActive(false);
        }

        public static void OnCloseSystem(this ViewMain self)
        {
        }

        #endregion

        #region UIEvents

        public static void UIStartButtonOnClick(this ViewMain self)
        {
            //TODO 传入房间id
            int roomId = self.GetConfigRoomoId();
            Debug.LogWarning($"Room Toggle Start : {self.currRoomType} + {self.currRoomId} => {roomId} => SceneId:{SceneHelper.GetSceneInfoComponent().SceneId}");
            EventsManager.BroadCast(GameEnum.EnterRoom, roomId);
            UIManager.Instance.OpenWindow<ViewBattleMain>();
        }


        public static void UIButtonRankButtonOnClick(this ViewMain self)
        {
            // UIManager.Instance.OpenWindow<ViewWorldRankMain>();
            UIManager.Instance.OpenWindow<ViewWorldRankMain>(new UIWindowData() { StringArgs1 = "World2Rank", }).ToCoroutine();
        }

        public static void UIButtonMapButtonOnClick(this ViewMain self)
        {
            self.RefreshMapToggle().ToCoroutine();
            self.UIMapNodeButton.gameObject.SetActive(true);
        }

        public static void UIMapNodeButtonOnClick(this ViewMain self)
        {
            self.UIMapNodeButton.gameObject.SetActive(false);
        }

        public static void UIReportBtnButtonOnClick(this ViewMain self)
        {
            // Debug.Log("Open Log Folder");
// #if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
//             System.Diagnostics.Process.Start("explorer.exe", LocalLog.dir.Replace("/", "\\"));
// #endif
            // LocalLog.UploadServer();
            
            UIManager.Instance.OpenWindow<ViewLoopListTest>().ToCoroutine();
        }

        public static void UIToggleRoomOnValueChanged(this ViewMain self, bool value, FightRoomType roomType)
        {
            if (!value) return;
            self.currRoomType = roomType;
            Debug.Log($"Room Toggle Type --- {roomType} --- {value}");
            FightRoomConfigCategory FightRoomConfig = TotalConfigManager.ConfigManager.FightRoomConfigCategory;
            if (FightRoomConfig.FightRoomTypeDic.TryGetValue(roomType, out List<FightRoomConfig> oneRoomConfigList))
            {
                for (int i = 0; i < oneRoomConfigList.Count; i++)
                {
                    var oneRoomConfig = oneRoomConfigList[i];
                    self.ToggleDescItems[i].GetComponentInChildren<Text>().text = oneRoomConfig.RoomName;
                    self.ToggleDescItems[i].SetActive(true);
                }

                if (self.ToggleDescItems.Count > oneRoomConfigList.Count)
                {
                    for (int i = oneRoomConfigList.Count; i < self.ToggleDescItems.Count; i++)
                    {
                        self.ToggleDescItems[i].SetActive(false);
                    }
                }
            }

            self.UIToggleOneRoomTimeOnValueChange(true, math.min(self.currRoomId, self.ToggleDescItems.Count));
        }

        public static void UIToggleOneRoomTimeOnValueChange(this ViewMain self, bool value, int Index)
        {
            if (!value) return;
            self.currRoomId = Index;
            // self.GetConfigRoomoId();
            self.RefreshMap().ToCoroutine();
        }

        #endregion

        #region GlobalEvents

        #endregion

        #region Logics

        public static int GetConfigRoomoId(this ViewMain self)
        {
            FightRoomConfigCategory fightRoomConfig = TotalConfigManager.ConfigManager.FightRoomConfigCategory;
            if (fightRoomConfig.FightRoomTypeDic.TryGetValue(self.currRoomType, out List<FightRoomConfig> oneRoomConfigList))
            {
                int roomId = oneRoomConfigList[self.currRoomId].RoomId;
                int time = fightRoomConfig.GetOrDefault(roomId).GameTime;
                Debug.Log($"Room Toggle ID:{roomId} / Time:{time}");
                return roomId;
            }

            return 0;
        }

        public static async UniTask RefreshMap(this ViewMain self)
        {
            int roomId = self.GetConfigRoomoId();
            int pickSceneId = self.CheckUsefulSceneIdInRoom(roomId);

            SceneGroupInfoConfig oneSceneInfo = TotalConfigManager.ConfigManager.SceneGroupInfoConfigCategory.GetOrDefault(pickSceneId);
            // 刷新按钮背景图
            if (self.UIMapImage.sprite != null && self.UIMapImage.sprite.name != oneSceneInfo.BtnBg)
            {
                Debug.Log($"btn sprite = {self.UIMapImage.sprite.name} -->{oneSceneInfo.BtnBg}");
                await YooAssetManager.Instance.LoadSpriteAsync(oneSceneInfo.BtnBg, self.UIMapImage, true);
            }

            self.SetSceneInfoCompSceneId(pickSceneId);
        }

        public static void SetSceneInfoCompSceneId(this ViewMain self, int sceneId)
        {
            var scenInfoComp = SceneHelper.GetSceneInfoComponent();
            scenInfoComp.SetSceneId(sceneId);
        }

        public static int GetSceneInfoCompSceneId(this ViewMain self)
        {
            return SceneHelper.GetSceneInfoComponent().SceneId;
        }

        public static async UniTask RefreshMapToggle(this ViewMain self)
        {
            int roomId = self.GetConfigRoomoId();
            FightRoomConfigCategory fightRoomCc = TotalConfigManager.ConfigManager.FightRoomConfigCategory;
            var fightSceneGroup = fightRoomCc.GetOrDefault(roomId).FightSceneGroup;
            int pickSceneId = self.CheckUsefulSceneIdInRoom(roomId);
            ObjectPoolManager.Instance.ReturnToPool(self.MapToggleItems);
            self.MapToggleItems.Clear();

            for (int i = 0; i < fightSceneGroup.Count; i++)
            {
                int sceneId = fightSceneGroup[i];
                // TODO 拉取排行榜玩家数据
                var obj = await ObjectPoolManager.Instance.GetFromPool<ViewMapToggleItem>(self.UIToggleGroupToggleGroup.transform);
                self.MapToggleItems.Add(obj);
                var itemData = new ViewMapToggleItemData()
                {
                    SceneId = sceneId,
                    isOn = pickSceneId == sceneId,
                    toggleGroup = self.UIToggleGroupToggleGroup,
                    OnClick = (int id) =>
                    {
                        // SaveData.SetInt(SaveData.Key.MapSceneId, id);
                        self.SetSceneInfoCompSceneId(id);
                        self.RefreshMap().ToCoroutine();
                    }
                };
                obj.GetComponent<ViewMapToggleItem>().OnRefresh(itemData);
                Debug.Log($"{sceneId} --> {itemData.isOn}");
            }
        }

        public static int CheckUsefulSceneIdInRoom(this ViewMain self, int roomId)
        {
            FightRoomConfigCategory fightRoomCc = TotalConfigManager.ConfigManager.FightRoomConfigCategory;
            var fightScene = fightRoomCc.GetOrDefault(roomId)?.FightSceneGroup;
            // var pickSceneId = SaveData.GetInt(SaveData.Key.MapSceneId, fightScene.First());
            var pickSceneId = self.GetSceneInfoCompSceneId();
            // 不同模式的矫正
            if (fightScene != null && !fightScene.Contains(pickSceneId))
            {
                Debug.Log($"Room：{roomId} 下修正地图：{pickSceneId} --> {fightScene.First()}");
                pickSceneId = fightScene.First();
                // SaveData.SetInt(SaveData.Key.MapSceneId, pickSceneId);
                self.SetSceneInfoCompSceneId(pickSceneId);
            }

            Debug.Log($"Room：{roomId} 选择地图：{pickSceneId} ");
            return pickSceneId;
        }


        public static void LoadTitleLogo(this ViewMain self)
        {
            var currChannel = TotalConfigManager.ConfigManager.ConstConfigCategory.CurrChannel;
            var constCc = TotalConfigManager.ConfigManager.LoginInfoConfigCategory.GetOrDefault(currChannel);
            YooAssetManager.Instance.LoadSpriteAsync(constCc.Logo, self.UITitleImage, true).ToCoroutine();
        }

        #endregion
    }
}