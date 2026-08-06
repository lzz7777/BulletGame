using System;
using System.Text;
using System.Threading;
using UnityEngine.Rendering;

namespace ByteDance.LiveOpenSdk.Perf
{
    public enum EventType
    {
        Custom,
        Pause,
        Resume,
    }

    internal enum ItemType
    {
        Init,
        Frame,
        Event,
    }

    internal interface IReportItem
    {
        ItemType ItemType { get; }

        int Frame { get; set; }

        long Time { get; set; }

        void Write(IDataWriter writer);

        void Retain();

        void Release();
    }

    internal abstract class ReportItem : IReportItem
    {
        public ItemType ItemType { get; protected set; }

        public int Frame { get; set; }

        public long Time { get; set; }

        private int _refCount;

        public void Retain()
        {
            Interlocked.Increment(ref _refCount);
        }

        public void Release()
        {
            Interlocked.Decrement(ref _refCount);
            if (_refCount > 0)
                return;
            Recycle();
        }

        protected abstract void Recycle();

        public abstract void Write(IDataWriter writer);
    }

    internal class InitInfo : ReportItem
    {
        public string ProjectName;

        public string Identifier;

        public int FrameRate;

        public long SystemMemorySize;

        public int ProcessorCount;

        public string ProcessorType;

        public string DeviceUniqueIdentifier;

        public int GraphicsMemorySize;

        public string GraphicsDeviceName;

        public GraphicsDeviceType GraphicsDeviceType;

        public InitInfo()
        {
            ItemType = ItemType.Init;
        }

        public override void Write(IDataWriter writer)
        {
            writer.WriteFieldString(nameof(ItemType), ItemType.ToString()).WriteSep()
                .WriteFieldInt(nameof(Frame), Frame).WriteSep()
                .WriteFieldLong(nameof(Time), Time).WriteSep()
                .WriteFieldString(nameof(ProjectName), ProjectName).WriteSep()
                .WriteFieldString(nameof(Identifier), Identifier).WriteSep()
                .WriteFieldInt(nameof(FrameRate), FrameRate).WriteSep()
                .WriteFieldLong(nameof(SystemMemorySize), SystemMemorySize).WriteSep()
                .WriteFieldInt(nameof(ProcessorCount), ProcessorCount).WriteSep()
                .WriteFieldString(nameof(ProcessorType), ProcessorType).WriteSep()
                .WriteFieldString(nameof(DeviceUniqueIdentifier), DeviceUniqueIdentifier).WriteSep()
                .WriteFieldInt(nameof(GraphicsMemorySize), GraphicsMemorySize).WriteSep()
                .WriteFieldString(nameof(GraphicsDeviceName), GraphicsDeviceName).WriteSep()
                .WriteFieldInt(nameof(GraphicsDeviceType), (int)GraphicsDeviceType);
        }

        protected override void Recycle()
        {
        }
    }

    internal class FrameInfo : ReportItem
    {
        public long FrameTime;
        public ulong ProcessorTime;
        public ulong FrameProcessorTime;
        public ulong SystemTime;
        public ulong FrameSystemTime;
        public ulong SystemBusyTime;
        public ulong FrameSystemBusyTime;

        public long Memory;

        // 1 means 1%, 100 means 100%
        public int CpuUsage;

        public FrameInfo()
        {
            ItemType = ItemType.Frame;
        }

        public override void Write(IDataWriter writer)
        {
            writer.WriteFieldString(nameof(ItemType), ItemType.ToString()).WriteSep()
                .WriteFieldInt(nameof(Frame), Frame).WriteSep()
                .WriteFieldLong(nameof(Time), Time).WriteSep()
                .WriteFieldLong(nameof(Memory), Memory).WriteSep()
                .WriteFieldInt(nameof(CpuUsage), CpuUsage).WriteSep()
                .WriteFieldLong(nameof(FrameTime), FrameTime);
        }

        protected override void Recycle()
        {
            ItemPool.Release(this);
        }
    }

    internal class EventInfo : ReportItem
    {
        public EventType Type;

        public string Key;

        public string Value;

        public EventInfo()
        {
            ItemType = ItemType.Event;
        }

        public override void Write(IDataWriter writer)
        {
            writer.WriteFieldString(nameof(ItemType), ItemType.ToString()).WriteSep()
                .WriteFieldInt(nameof(Frame), Frame).WriteSep()
                .WriteFieldLong(nameof(Time), Time).WriteSep()
                .WriteFieldInt(nameof(Type), (int)Type).WriteSep()
                .WriteFieldString(nameof(Key), Key).WriteSep()
                .WriteFieldString(nameof(Value), Value);
        }

        protected override void Recycle()
        {
            ItemPool.Release(this);
        }
    }

    internal static class ReportItemExtension
    {
        public static StringBuilder AppendJsonString(this StringBuilder sb, string v)
        {
            sb.Append('\"').Append(v).Append('\"');
            return sb;
        }
    }
}