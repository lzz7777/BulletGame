// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Scripting;

namespace ByteDance.LiveOpenSdk.Perf
{
    public enum PerfCpuCalcStyle
    {
        DefaultBalanced,
        RawProcessTime,
        ProcessExplorer,
        TaskManagerWin10,
        TaskManagerWin11,
    }

    internal class PerfReporterImpl : IPerfReporter
    {
        private const string Tag = nameof(IPerfReporter);

        [Preserve]
        public PerfCpuCalcStyle PerfCpuCalcStyle { get; set; }

        private readonly List<IPerfListener> _listeners = new();
        private readonly IntPtr _currentPid;
        private FrameInfo _prevFrame;
        private bool _isFirstFrame = true;
        private bool _isPaused;
        private bool _start;

        public PerfReporterImpl()
        {
            PerfCpuCalcStyle = PerfCpuCalcStyle.DefaultBalanced;
            _currentPid = ProcessTool.GetCurrentProcess();
            Application.quitting += OnQuit;
        }

        private void OnQuit()
        {
            Stop();
        }

        private void AddInitInfo(IPerfListener listener)
        {
            var info = new InitInfo
            {
                ProjectName = Application.productName,
                Identifier = Application.identifier,
                FrameRate = Application.targetFrameRate,
                SystemMemorySize = SystemInfo.systemMemorySize,
                ProcessorCount = SystemInfo.processorCount,
                ProcessorType = SystemInfo.processorType,
                DeviceUniqueIdentifier = SystemInfo.deviceUniqueIdentifier,
                GraphicsMemorySize = SystemInfo.graphicsMemorySize,
                GraphicsDeviceName = SystemInfo.graphicsDeviceName,
                GraphicsDeviceType = SystemInfo.graphicsDeviceType
            };
            InitReportItem(info);
            listener.Start(info);
        }

        public void Start()
        {
            if (_start)
                return;

            _start = true;
            Debug.Log($"{Tag} Start...");
            EarlyUpdateSystem.OnEarlyUpdate += OnUpdate;

            foreach (var listener in _listeners)
            {
                AddInitInfo(listener);
            }
            Debug.Log($"{Tag} Start Complete");
        }

        public void Stop()
        {
            if (_start == false)
                return;

            _start = false;
            Debug.Log($"{Tag} Stop");
            EarlyUpdateSystem.OnEarlyUpdate -= OnUpdate;
            foreach (var listener in _listeners)
            {
                listener.Stop();
            }
        }

        public void Pause()
        {
            if (_isPaused)
                return;
            Debug.Log($"{Tag} Pause");
            _isPaused = true;
            Event(EventType.Pause);
        }

        public void Resume()
        {
            if (!_isPaused)
                return;
            _isFirstFrame = true;
            _isPaused = false;
            Debug.Log($"{Tag} Resume");
            Event(EventType.Resume);
        }

        public T AddListener<T>() where T : IPerfListener, new()
        {
            foreach (var listener in _listeners)
            {
                if (listener is T exist)
                    return exist;
            }

            var t = new T();
            _listeners.Add(t);
            return t;
        }

        public void AddListener(IPerfListener listener)
        {
            _listeners.Add(listener);
            if (_start) AddInitInfo(listener);
        }

        private void Event(EventType type)
        {
            var e = ItemPool.GetEventInfo();
            e.Type = type;
            InitReportItem(e);

            foreach (var listener in _listeners)
            {
                listener.OnPerfItem(e);
            }
        }

        public void Event(string key, string value)
        {
            var e = ItemPool.GetEventInfo();
            e.Key = key;
            e.Value = value;
            InitReportItem(e);
        }

        [Preserve]
        internal static void InitReportItem(IReportItem item)
        {
            item.Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            item.Frame = Time.frameCount;
        }

        private void CalcFrameInfo(FrameInfo frameInfo, FrameInfo prevFrame)
        {
            UpdateFrameInfo(frameInfo, prevFrame, _currentPid, PerfCpuCalcStyle);
        }

        [Preserve]
        internal static void UpdateFrameInfo(FrameInfo frameInfo, FrameInfo prevFrame, IntPtr pid, PerfCpuCalcStyle calcStyle)
        {
            frameInfo.FrameTime = frameInfo.Time - prevFrame.Time;
            frameInfo.Memory = ProcessTool.GetProcessMemory(pid);

            frameInfo.ProcessorTime = ProcessTool.GetProcessTimes(pid);
            frameInfo.FrameProcessorTime = frameInfo.ProcessorTime - prevFrame.ProcessorTime;
            frameInfo.SystemTime = ProcessTool.GetSystemTime();
            frameInfo.FrameSystemTime = frameInfo.SystemTime - prevFrame.SystemTime;
            frameInfo.SystemBusyTime = ProcessTool.GetSystemBusyTime();
            frameInfo.FrameSystemBusyTime = frameInfo.SystemBusyTime - prevFrame.SystemBusyTime;

            var cpu = frameInfo.FrameProcessorTime;
            var sysTime = frameInfo.FrameSystemTime;
            var busyTime = frameInfo.FrameSystemBusyTime;
            if (sysTime <= 0)
                sysTime = 0;
            if (busyTime <= 0)
                busyTime = 0;
            var p = CalcCpuUsage(cpu, sysTime, busyTime, calcStyle);
            frameInfo.CpuUsage = (int)(p * 100);
        }

        [Preserve]
        internal static float CalcCpuUsage(ulong cpu, ulong sysTime, ulong busyTime, PerfCpuCalcStyle calcStyle)
        {
            float kBaseFactor;
            switch (calcStyle)
            {
                case PerfCpuCalcStyle.RawProcessTime:
                    kBaseFactor = 1f;
                    break;
                case PerfCpuCalcStyle.ProcessExplorer:
                    kBaseFactor = 0.4f;
                    break;
                case PerfCpuCalcStyle.TaskManagerWin10:
                    kBaseFactor = 0.6f;
                    break;
                case PerfCpuCalcStyle.TaskManagerWin11:
                    kBaseFactor = 0.2f;
                    break;
                default:
                case PerfCpuCalcStyle.DefaultBalanced:
                    kBaseFactor = 0.5f;
                    break;
            }

            var kBusyFactor = 1f - kBaseFactor;
            var protection = sysTime == 0 && busyTime == 0 ? 1f : 0;
            return cpu / (sysTime * kBaseFactor + busyTime * kBusyFactor + protection);
        }

        private static ProfilerMarker _updateMarker = new ("Perf.Update");

        private void OnUpdate()
        {
            using var _ = _updateMarker.Auto();

            if (_isPaused || !_start)
                return;

            var frameInfo = ItemPool.GetFrameInfo();
            frameInfo.Retain();

            InitReportItem(frameInfo);

            if (_isFirstFrame)
            {
                _isFirstFrame = false;
            }
            else
            {
                CalcFrameInfo(frameInfo, _prevFrame);
                OnFrameInfo(frameInfo);
            }

            _prevFrame?.Release();
            _prevFrame = frameInfo;
        }

        private void OnFrameInfo(FrameInfo frameInfo)
        {
            foreach (var listener in _listeners)
            {
                listener.OnPerfItem(frameInfo);
            }
        }

        public long GetMemory()
        {
            return ProcessTool.GetProcessMemory(_currentPid);
        }
        public IntPtr GetCurrentProcess()
        {
            return ProcessTool.GetCurrentProcess();
        }
    }
}