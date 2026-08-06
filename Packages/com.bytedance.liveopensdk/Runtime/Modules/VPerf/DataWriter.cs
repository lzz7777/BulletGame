using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Unity.Profiling;
using UnityEngine;

namespace ByteDance.LiveOpenSdk.Perf
{
    internal class DataWriter
    {
        private readonly ManualResetEventSlim _readEvent = new(false);
        private BinaryWriter _writer;
        private InitInfo _initInfo;
        private bool _initWrite;

        private readonly Stack<BinaryDataWriter> _dataWriters = new();

        private BinaryDataWriter GetWriter()
        {
            if (_dataWriters.Count > 0)
            {
                return _dataWriters.Pop();
            }
            var w = new BinaryDataWriter();
            return w;
        }

        public void Stop()
        {
            _readEvent.Set();
            _writer?.Dispose();
            _writer = null;
        }

        public void Start(InitInfo info)
        {
            var fileName = $"profile.{DateTime.Now:yyyy-MM-dd HHmmss}.data";
            var output = File.Create(Path.Combine(Application.dataPath, "..", fileName));
            _initInfo = info;
            _writer = new BinaryWriter(output);
        }

        private static ProfilerMarker _updateMarker = new ("Perf.Write.IReportItem");

        public void Write(IReportItem item)
        {
            if (!_initWrite)
            {
                _initWrite = true;
                DoWrite(_initInfo);
            }
            DoWrite(item);
        }

        private void DoWrite(IReportItem item)
        {
            using var _ = _updateMarker.Auto();

            var dataWriter = GetWriter();
            dataWriter.Reset();
            item.Write(dataWriter);

            // Write file
            var len = (int)dataWriter.Length;
            // Debug.Log($"write len = {len}");

            _writer.Write(len);
            var pos = _writer.BaseStream.Position;
            dataWriter.WriteTo(_writer.BaseStream);
            var writeLen = _writer.BaseStream.Position - pos;
            Debug.Assert(writeLen == len);

            // 回收
            _dataWriters.Push(dataWriter);
        }
    }

    internal class BinaryDataWriter : IDataWriter
    {
        private readonly MemoryStream _stream = new();
        private readonly BinaryWriter _writer;

        public BinaryDataWriter()
        {
            _writer = new BinaryWriter(_stream);
        }

        public void Reset()
        {
            _stream.Position = 0;
        }

        public void Dispose()
        {
            _writer.Dispose();
            _stream.Dispose();
        }

        public long Length => _stream.Position;

        public void WriteTo(Stream target)
        {
            _stream.SetLength(_stream.Position);
            _stream.WriteTo(target);
        }

        IDataWriter IDataWriter.WriteFieldString(string field, string value)
        {
            var span = Encoding.UTF8.GetBytes(value);
            Debug.Assert(span.Length < ushort.MaxValue);
            _writer.Write((ushort)span.Length);
            _writer.Write(span);
            return this;
        }

        IDataWriter IDataWriter.WriteFieldInt(string field, int value)
        {
            _writer.Write(value);
            return this;
        }

        IDataWriter IDataWriter.WriteFieldFloat(string field, float value)
        {
            _writer.Write(value);
            return this;
        }

        IDataWriter IDataWriter.WriteFieldLong(string field, long value)
        {
            _writer.Write(value);
            return this;
        }

        IDataWriter IDataWriter.WriteSep()
        {
            return this;
        }
    }

    internal interface IDataWriter
    {
        IDataWriter WriteFieldString(string field, string value);
        IDataWriter WriteFieldInt(string field, int value);
        IDataWriter WriteFieldFloat(string field, float value);
        IDataWriter WriteFieldLong(string field, long value);
        IDataWriter WriteSep();
    }

    internal class StringDataWriter : IDataWriter
    {
        private readonly StringBuilder sb = new();

        public override string ToString()
        {
            return sb.ToString();
        }

        public void Clear()
        {
            sb.Clear();
        }

        public void WriteDefault(IReportItem item)
        {
            sb.Append('{');
            item.Write(this);
            sb.Append('}');
        }

        public IDataWriter AppendJsonString(string v)
        {
            sb.Append('\"').Append(v).Append('\"');
            return this;
        }

        public IDataWriter WriteSep()
        {
            sb.Append(',');
            return this;
        }

        public IDataWriter WriteFieldString(string field, string value)
        {
            sb.AppendJsonString(field).Append(":").AppendJsonString(value);
            return this;
        }

        public IDataWriter WriteFieldInt(string field, int value)
        {
            sb.AppendJsonString(field).Append(":").Append(value);
            return this;
        }

        public IDataWriter WriteFieldFloat(string field, float value)
        {
            sb.AppendJsonString(field).Append(":").Append(value);
            return this;
        }

        public IDataWriter WriteFieldLong(string field, long value)
        {
            sb.AppendJsonString(field).Append(":").Append(value);
            return this;
        }
    }
}