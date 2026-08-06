// Copyright (c) Bytedance. All rights reserved.
// Author: Potato Meng
// Date: 2025/05/09
// Description:

using System;
using UnityEngine;

namespace ByteDance.CloudSync
{
    /// <summary>
    /// Resolution更新器
    /// 提供如下作用：
    /// 1 初始化的时候进行锁帧相关设置检测，打印日志。
    /// 2 输出程序分辨率和屏幕分辨率。
    /// </summary>
    public class ResolutionUpdatable : ISafeActionsUpdatable
    {
        //全局变量
        private static int ScreenRenderWidth { get; set; }
        private static int ScreenRenderHeight { get; set; }

        private static bool IsScreenRenderInit { get; set; }

        private bool IsStarted { get; set; }
        private bool _hasInitStartData;
        private int _preWidth;
        private int _preHeight;

        private const float CheckInterval = 1f; // 检测间隔（秒）
        private const float CheckIntervalEditor = 30f; // 检测间隔（秒）
        private float _checkTimer;
        private float _interval;

        public ResolutionUpdatable Init()
        {
            InitStartedData();
            return this;
        }

        private void InitStartedData()
        {
            // 初始化之前的宽度和高度
            _preWidth = Screen.width;
            _preHeight = Screen.height;
            IsScreenRenderInit = false;
            OutputResolutions();
            _checkTimer = 0f;
            _interval = Application.isEditor ? CheckIntervalEditor : CheckInterval;
        }


        public void Update()
        {
            // 更新计时器
            _checkTimer += Time.deltaTime;

            if (_checkTimer >= _interval)
            {
                _checkTimer = 0f;
                // 检查程序分辨率是否变更
                if (Screen.width != _preWidth || Screen.height != _preHeight)
                {
                    // 更新之前的程序宽度和高度
                    _preWidth = Screen.width;
                    _preHeight = Screen.height;

                    // 打印变更后的程序分辨率和屏幕分辨率
                    OutputResolutions();
                }
            }

            // 只在第一次执行时检查锁帧设置
            if (!IsStarted)
            {
                CheckFrameRateSettings();
                IsStarted = true;
            }
        }
        private void CheckFrameRateSettings()
        {
            double refreshRate = Screen.currentResolution.refreshRate;
            if (QualitySettings.vSyncCount > 0)
            {
                if (QualitySettings.vSyncCount == 1)
                {
                    CGLogger.Log($"游戏因VSync开启，帧率被限制为显示器刷新率 {refreshRate}");
                }
                else if (QualitySettings.vSyncCount == 2)
                {
                    CGLogger.Log($"游戏因VSync开启，帧率被限制为显示器刷新率的一半{refreshRate/2}");
                }
            }
            else if (Application.targetFrameRate != -1)
            {
                CGLogger.Log($"游戏已锁帧，目标帧率为: {Application.targetFrameRate}");
            }
            else
            {
                CGLogger.LogWarning("游戏未锁帧");
            }
        }
        private void OutputResolutions()
        {
            // 获取当前程序分辨率
            int programWidth = Screen.width;
            int programHeight = Screen.height;

            // 获取当前屏幕分辨率
            Resolution resolution = Screen.currentResolution;
            // 打印当前程序分辨率和屏幕分辨率
            CGLogger.Log($"当前程序分辨率: {programWidth}x{programHeight} 当前屏幕分辨率: {resolution.width}x{resolution.height} @ {resolution.refreshRate}Hz");
        }

        public static void CheckStreamTexture(int index,Vector2Int resolution)
        {
            if (!IsScreenRenderInit||index==0)
            {
                ScreenRenderWidth = resolution.x;
                ScreenRenderHeight = resolution.y;
                IsScreenRenderInit = true;
                CGLogger.Log($"CheckStreamTexture 用户推流分辨率首次设置 Index: {index} 推流分辨率为{ScreenRenderWidth}x{ScreenRenderHeight}");
            }
            // 推流分辨率是否变更
            if (resolution.x != ScreenRenderWidth || resolution.y != ScreenRenderHeight)
            {
                CGLogger.Log($"CheckStreamTexture 用户推流分辨率变更 Index: {index} 之前推流分辨率为{ScreenRenderWidth}x{ScreenRenderHeight},新推流分辨率为{resolution.x}x{resolution.y}");
            }
        }
        public static bool TryGetHostResolution(out Vector2Int resolution)
        {
            resolution = new Vector2Int(ScreenRenderWidth, ScreenRenderHeight);
            return IsScreenRenderInit;
        }
    }
}