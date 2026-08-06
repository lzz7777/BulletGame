// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using ByteDance.LiveOpenSdk;

namespace ByteDance.CloudSync.External
{
    /// <summary>
    /// 外部能力提供
    /// </summary>
    internal static class CloudSyncExternals
    {
        public static Func<ILiveOpenSdk> LiveOpenSdkProvide { get; set; }
        public const string SdkVersion = "2.7.11";
    }
}