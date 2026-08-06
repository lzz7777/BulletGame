// Copyright (c) Bytedance. All rights reserved.
// Description: 性能数据监听接口，用于接收并处理性能数据

namespace ByteDance.LiveOpenSdk.Perf
{
    internal interface IPerfListener
    {
        void Start(InitInfo info);

        void Stop();

        void OnPerfItem(IReportItem item);
    }
}