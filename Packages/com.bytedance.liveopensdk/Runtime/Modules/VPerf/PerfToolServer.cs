using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace ByteDance.LiveOpenSdk.Perf
{
    /// <summary>
    /// VPerf-Tool 工具后台服务
    /// VPerf 使用手册：https://bytedance.larkoffice.com/docx/V5X4dZi06oWVj9xxPBQcrzswnnd
    /// </summary>
    internal class PerfToolServer : IPerfListener
    {
        private WebSocketServer _server;
        private ReportClient _client;
        private InitInfo _initInfo;
        private bool _running;
        private DataWriter _writer;

        private readonly StringDataWriter _buffer = new ();
        private readonly ConcurrentQueue<IReportItem> _itemsToSend = new();
        private readonly ManualResetEventSlim _waitWrite = new(false);

        public PerfToolServer()
        {
            _server = new WebSocketServer(8894);
            _server.AddWebSocketService("/reporter", () => new ReportClient(this));
        }

        private void OnClientOpen(ReportClient client)
        {
            if (_client != null)
                return;
            _client = client;
        }

        private void OnClientClosed(ReportClient client)
        {
            _client = null;
        }

        private class ReportClient : WebSocketBehavior
        {
            private readonly PerfToolServer _perfToolServer;

            public bool InitSend { get; set; }

            public ReportClient(PerfToolServer perfToolServer)
            {
                _perfToolServer = perfToolServer;
            }

            public void SendData(string data)
            {
                Send(data);
            }

            protected override void OnClose(CloseEventArgs e)
            {
                _perfToolServer.OnClientClosed(this);
            }

            protected override void OnOpen()
            {
                _perfToolServer.OnClientOpen(this);
            }
        }

        void IPerfListener.Start(InitInfo info)
        {
            _running = true;
            _initInfo = info;
            _server.Start();
            _writer = new DataWriter();
            _writer.Start(info);
            ThreadPool.QueueUserWorkItem(SendQueue);
        }

        void IPerfListener.Stop()
        {
            _running = false;
            _waitWrite.Set();
            _server?.Stop();
            _server = null;
            _writer?.Stop();
            _writer = null;
        }

        private void SendQueue(object _)
        {
            while (_running)
            {
                _waitWrite.Wait();
                _waitWrite.Reset();
                if (_running == false)
                    break;
                SendAll();
            }
        }

        private readonly List<IReportItem> _items = new();

        private void SendAll()
        {
            _items.AddRange(_itemsToSend);
            _itemsToSend.Clear();
            _items.Sort((a, b) => a.Frame.CompareTo(b.Frame));
            foreach (var item in _items)
            {
                Send(item);
            }
            _items.Clear();
        }

        private void Send(IReportItem item)
        {
            _writer?.Write(item);

            if (_client != null)
            {
                var client = _client;
                if (!client.InitSend)
                {
                    SendToClient(client, _initInfo);
                    client.InitSend = true;
                }

                SendToClient(client, item);
            }

            item.Release();
        }

        private void SendToClient(ReportClient client, IReportItem item)
        {
            _buffer.Clear();
            _buffer.WriteDefault(item);
            var data = _buffer.ToString();
            client.SendData(data);
        }

        void IPerfListener.OnPerfItem(IReportItem item)
        {
            item.Retain();

            _itemsToSend.Enqueue(item);
            _waitWrite.Set();
        }
    }
}