// Copyright (c) Bytedance. All rights reserved.
// Description:

using ByteDance.CloudSync.External;

namespace ByteDance.CloudSync
{
    internal partial class CloudSyncSdk
    {
        public string Version => CloudSyncExternals.SdkVersion;
        public string PatchVersion => "";
    }
}