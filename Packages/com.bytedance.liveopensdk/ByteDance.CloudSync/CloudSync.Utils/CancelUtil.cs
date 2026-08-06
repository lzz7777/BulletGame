// Copyright (c) Bytedance. All rights reserved.
// Author: DONEY Dong
// Date: 2025/02/13
// Description:

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ByteDance.CloudSync
{
    public class QuittingException : TaskCanceledException
    {
    }

    public static class CancelUtil
    {
        public static CancellationToken PreCancelledToken
        {
            get
            {
                using var cts = new CancellationTokenSource();
                cts.Cancel();
                return cts.Token;
            }
        }

        public static CancellationToken SafeToken(CancellationTokenSource tokenSource, bool defaultCanceled) =>
            SafeToken(tokenSource, defaultCanceled ? PreCancelledToken : CancellationToken.None);

        public static CancellationToken SafeToken(CancellationTokenSource tokenSource, CancellationToken fallbackToken)
        {
            try
            {
                return tokenSource?.Token ?? fallbackToken;
            }
            catch (ObjectDisposedException e)
            {
                Debug.LogWarning(e);
                return fallbackToken;
            }
        }
    }
}