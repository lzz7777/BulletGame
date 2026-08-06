using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ByteDance.LiveOpenSdk.Perf
{
    internal static class ProcessTool
    {
        [DllImport("process-tool.dll")]
        private static extern IntPtr current_process();

        [DllImport("process-tool.dll")]
        private static extern IntPtr open_process(long pid);

        [DllImport("process-tool.dll")]
        private static extern void close_process(IntPtr handle);

        [DllImport("process-tool.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern ulong get_process_memory_info(IntPtr handle, out ulong privateWorkingSet, out ulong workingSet);

        [DllImport("process-tool.dll")]
        private static extern ulong get_process_times(IntPtr handle);

        [DllImport("process-tool.dll")]
        private static extern ulong get_system_time();

        [DllImport("process-tool.dll")]
        private static extern ulong get_system_busy_time();

        public static IntPtr GetCurrentProcess()
        {
            return current_process();
        }

        public static long GetProcessMemory(IntPtr pid)
        {
            ulong workingSet = 0;
            ulong privateWorkingSet = 0;
            ulong mb = get_process_memory_info(pid, out privateWorkingSet, out workingSet);
            // Debug.Log($"get_process_memory_info. mb = {mb}, privateWorkingSet = {privateWorkingSet}, workingSet = {workingSet}");
            return (long)workingSet;
        }

        public static ulong GetProcessTimes(IntPtr pid)
        {
            return get_process_times(pid);
        }

        public static ulong GetSystemTime()
        {
            return get_system_time();
        }

        public static ulong GetSystemBusyTime()
        {
            return get_system_busy_time();
        }
    }
}