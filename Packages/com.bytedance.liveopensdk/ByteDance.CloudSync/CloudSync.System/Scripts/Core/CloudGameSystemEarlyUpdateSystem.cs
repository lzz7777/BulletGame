using System;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace ByteDance.CloudSync
{
    /// <summary>
    /// 插入回调至 PlayerLoop.EarlyUpdate
    /// </summary>
    internal struct CloudGameSystemEarlyUpdateSystem
    {
        private static event Action EarlyUpdateCallback;

        public static event Action OnEarlyUpdate
        {
            add
            {
                TryInjectPlayerLoop();
                EarlyUpdateCallback += value;
            }
            remove => EarlyUpdateCallback -= value;
        }

        private static void OnEarlyUpdateInternal()
        {
            EarlyUpdateCallback?.Invoke();
        }

        private static void TryInjectPlayerLoop()
        {
            InsertPlayerLoopSystemBefore(
                typeof(EarlyUpdate),
                typeof(EarlyUpdate.UpdateInputManager),
                new PlayerLoopSystem()
                {
                    type = typeof(CloudGameSystemEarlyUpdateSystem),
                    updateDelegate = OnEarlyUpdateInternal
                });
        }

        /// <summary>
        /// 插入子系统至 PlayerLoop
        /// </summary>
        /// <param name="systemType"></param> 目标 PlayerLoop stage
        /// <param name="anchorSystem"></param> 目标插入的位置，插入至某个已存在的子系统之前
        /// <param name="subSystem"></param> 目标插入的子系统
        private static void InsertPlayerLoopSystemBefore(Type systemType, Type anchorSystem, PlayerLoopSystem subSystem)
        {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            var systemIndex = Array.FindIndex(playerLoop.subSystemList, x => x.type == systemType);
            if (systemIndex == -1)
            {
                CGLogger.LogError("CloudGameSystemEarlyUpdateSystem - System not found: " + systemType);
                return;
            }

            var subSystems = playerLoop.subSystemList[systemIndex].subSystemList;

            if (Array.FindIndex(subSystems, x => x.type == subSystem.type) != -1)
            {
                // 待插入的 subSystem 已存在
                return;
            }

            // 1. 找到插入的目标位置
            var anchorIndex = Array.FindIndex(subSystems, x => x.type == anchorSystem);
            if (anchorIndex == -1)
            {
                CGLogger.LogError("CloudGameSystemEarlyUpdateSystem - Anchor System not found: " + anchorSystem);
                anchorIndex = subSystems.Length;
            }

            var prevSize = subSystems.Length;
            // 2. resize 数组
            Array.Resize(ref subSystems, prevSize + 1);

            if (anchorIndex != subSystems.Length)
            {
                // 3. 插入点后的 item 向后挪，把插入点腾出来
                Array.Copy(subSystems, anchorIndex, subSystems, anchorIndex + 1, prevSize - anchorIndex);
            }

            // 4. 插入 subSystem
            subSystems[anchorIndex] = subSystem;

            // 5. 插入后的数组设置进 Player Loop
            playerLoop.subSystemList[systemIndex].subSystemList = subSystems;
            PlayerLoop.SetPlayerLoop(playerLoop);
        }
    }
}