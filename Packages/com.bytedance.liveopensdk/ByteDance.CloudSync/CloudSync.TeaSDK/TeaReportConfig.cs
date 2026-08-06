using UnityEngine;

namespace ByteDance.CloudSync.TeaSDK
{
    public class TeaReportConfig
    {
        public static readonly int ByteIOAppId = 646224;

        // 用于埋点验证的指定uid
        public static readonly string ByteIOUserUid = "StarkTeaDebugProvider";

        public static bool IsReport
        {
            get
            {
                if (Application.isEditor)
                    return false;
                return true;
            }
        }

        public static readonly string ByteIOInitReportIdParamName = "uid";
    }
}