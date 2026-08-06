// Copyright@www.bytedance.com
// Author: DONEY Dong
// Date: 2024/06/28
// Description:

using System;

namespace ByteDance.CloudSync
{
    public enum StartStage
    {
        Sdk_Init,
        Sdk_InitMultiplayer,
        Wait_Anchors_Join,
    }

    public enum StartStageCode
    {
        Success,
        Timeout,
        Sdk_Init_Error,
        Sdk_InitMultiplayer_Error,
    }

    public struct StartStageParam
    {
        public StartStage Stage;
        public StartStageCode Code;
        public string Message;
        public string LinkInitParam;
        public string LinkRoomId;
        public long StartTime;

        internal StartStageParam(StartStage stage)
        {
            Stage = stage;
            Code = StartStageCode.Success;
            Message = "";
            LinkInitParam = null;
            LinkRoomId = null;
            StartTime = TimeUtil.NowTimestampMs;
        }

        internal void SetResult(StartStageCode code, string message = "")
        {
            Code = code;
            Message = message;
        }
    }

    internal partial class CloudGameSdkManager
    {
        public event Action<StartStageParam> StartStageChanged;

        private void ReportInit(StartStageParam param, InitCloudGameResult initResult)
        {
            StartStageCode code;
            switch (initResult.State)
            {
                case InitState.Success:
                case InitState.SuccessAlready:
                    code = StartStageCode.Success;
                    break;
                default:
                    code = StartStageCode.Sdk_Init_Error;
                    if (param.Stage == StartStage.Sdk_InitMultiplayer)
                        code = StartStageCode.Sdk_InitMultiplayer_Error;
                    break;
            }

            param.SetResult(code, initResult.Error);
            Report(param);
        }

        private void Report(StartStageParam param, StartStageCode code, string info)
        {
            param.SetResult(code, info);
            Report(param);
        }

        private void Report(StartStageParam param)
        {
            // param.LinkInitParam = LinkRoomInitParam;
            // param.LinkRoomId = LinkRoomId;
            try
            {
                StartStageChanged?.Invoke(param);
            }
            catch (Exception e)
            {
                CGLogger.LogError(e.ToString());
            }
        }
    }
}