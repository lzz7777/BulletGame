using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

// ReSharper disable once ClassNeverInstantiated.Global

namespace ByteDance.CloudSync
{
    internal class StartupMonitor
    {
        private static readonly TimeRecord Startup = new("游戏启动");
        private static readonly TimeRecord Assembly = new("程序加载");
        private static readonly TimeRecord SplashAndScene = new("闪屏与首场景加载");
        private static readonly TimeRecord SceneAwake = new("首场景Awake");

        private static readonly TimeMonitor Monitor = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnRuntimeInitialize_Startup() => Monitor.Push(Startup);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void OnRuntimeInitialize_AssembliesLoaded() => Monitor.Log("程序集加载 完成");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void OnRuntimeInitialize_BeforeSplashScreen() => Monitor.Push(SplashAndScene);

        // note: unity doc: Callback invoked when the first scene's objects are loaded into memory but before Awake has been called.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnRuntimeInitialize_SceneLoad() => Monitor.Push(SceneAwake);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnRuntimeInitialize_SceneStarted() => Monitor.Pop(Startup);
    }

    internal class TimeMonitor
    {
        internal static string NowTime => DateTime.Now.ToString("HH:mm:ss.fff");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Log(string msg) => Debug.Log($"{NowTime} [Monitor] {msg}");

        private readonly Stack<TimeRecord> _stack = new();

        internal void Push(TimeRecord record, params TimeRecord[] records)
        {
            BeginRecord(record);
            _stack.Push(record);
            foreach (var it in records)
            {
                BeginRecord(it);
                _stack.Push(it);
            }
        }

        internal void Pop(TimeRecord record)
        {
            PopRecordsUntil(record);
        }

        internal void PopAndPush(TimeRecord record1, TimeRecord record2)
        {
            Pop(record1);
            Push(record2);
        }

        private void PopRecordsUntil(TimeRecord targetRecord)
        {
            if (!_stack.Contains(targetRecord))
            {
                EndRecord(targetRecord);
                return;
            }

            while (_stack.TryPop(out var record))
            {
                EndRecord(record);
                if (record == targetRecord)
                    return;
            }
        }

        private void BeginRecord(TimeRecord record)
        {
            record.Begin();
            var spacer = CurrentSpacer();
            Log($"{spacer} -> {record.Name} ...");
        }

        private void EndRecord(TimeRecord record)
        {
            record.End();
            var spacer = CurrentSpacer();
            Log($"{spacer} |< {record.Name} 完成 {record.TimeElapsed.TotalSeconds:0.000}s");
        }

        private static string Space(int depth) => depth >= 0 ? new string('\t', depth) : string.Empty;

        private string CurrentSpacer()
        {
            var depth = _stack.Count;
            var spacer = Space(depth);
            return spacer;
        }
    }

    internal class TimeRecord
    {
        public string Name { get; }
        public DateTime TimeBegin { get; private set; }
        public DateTime TimeEnd { get; private set; }
        public TimeSpan TimeElapsed { get; private set; }

        public TimeRecord(string name)
        {
            Name = name;
        }

        public void Begin()
        {
            TimeBegin = DateTime.Now;
        }

        public void End()
        {
            TimeEnd = DateTime.Now;
            TimeElapsed = TimeEnd - TimeBegin;
        }
    }
}