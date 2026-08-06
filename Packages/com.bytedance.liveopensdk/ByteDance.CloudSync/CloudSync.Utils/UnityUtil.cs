// Copyright (c) Bytedance. All rights reserved.
// Description:

using System.Text.RegularExpressions;
using UnityEngine;

namespace ByteDance.CloudSync
{
    public class UnityUtil
    {
        /// Is Unity Version Greater than or equal
        public static bool IsUnityVersionGte(int targetMajor, out int currentMajor)
        {
            var match = Regex.Match(Application.unityVersion, @"(\d+)\.\d+\..*");
            const int majorBase2K = 2000;
            const int majorNewBase = 6;
            currentMajor = 0;
            if (!match.Success)
                return false;

            var major = int.Parse(match.Groups[1].Value);
            currentMajor = major;
            switch (targetMajor)
            {
                case >= majorBase2K:
                    return major >= targetMajor;
                case >= majorNewBase:
                    return major >= targetMajor && major < majorBase2K;
                default:
                    return false;
            }
        }
    }
}