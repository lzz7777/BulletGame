using System;
using System.Threading.Tasks;

namespace ByteDance.CloudSync
{
    internal interface ICloudGameAPI : ICloudGameMatchAPI
    {
        public enum ErrorCode
        {
            Err_MC_Init_EnvNoHostPID = -1000, // 0xFFFFFC18
            Err_MC_Init_ShareMemNotFound = -999, // 0xFFFFFC19
            Err_MC_Init_CannotOpenShareMem = -998, // 0xFFFFFC1A
            Err_MC_Init_IPCNameEmpty = -997, // 0xFFFFFC1B
            Err_MC_Init_InvalidAccountID = -996, // 0xFFFFFC1C
            Err_MC_Not_Init = -995, // 0xFFFFFC1D
            Err_MC_Invalid_Arg = -994, // 0xFFFFFC1E
            Err_MC_Send_NotConnect = -993, // 0xFFFFFC1F
            Err_MC_Send_Failed = -992, // 0xFFFFFC20
            Err_MC_Send_SizeError = -991, // 0xFFFFFC21
            Err_MC_Send_Timeout = -990, // 0xFFFFFC22
            Err_Frontier_Init_Error = -100, // 0xFFFFFF9C
            Err_Frontier_Init_Timeout = -99, // 0xFFFFFF9D
            Err_Frontier_Init_InvalidArg = -98, // 0xFFFFFF9E
            Err_Frontier_Init_ConnectFailed = -97, // 0xFFFFFF9F
            Err_Frontier_Init_DisconnectFailed = -96, // 0xFFFFFFA0
            Err_Frontier_Not_Init = -95, // 0xFFFFFFA1
            Err_Frontier_Invalid_Arg = -94, // 0xFFFFFFA2
            Err_Frontier_Send_Failed = -93, // 0xFFFFFFA3
            Err_Frontier_Send_Timeout = -92, // 0xFFFFFFA4
            Err_Frontier_Send_NotConnect = -91, // 0xFFFFFFA5

            Success = 0,
            Success_AlreadyInited = 1,
            Err_Sdk_Init_Invalid_Arg = 2,

            Error = -1
        }

        public struct Response
        {
            public ErrorCode code;
            public string message;

            public Response(ErrorCode code, string message)
            {
                this.code = code;
                this.message = message;
            }

            public string ToStr() => $"{{ code: {code} ({(int)code}), message: {message} }}";
        }

        string FileVersion { get; }

        void SetMultiplayerListener(IMultiplayerListener listener);

        Task<Response> Init();

        Task<Response> InitMultiplayer();

        void SetLogFunction(Action<string> sdkLog, Action<string> sdkLogError);


        Task<Response> SendOpenServiceCustomMessage(SeatIndex roomIndex, string msg);

        ErrorCode SendVideoFrame(SeatIndex roomIndex, long textureId);

        void SendPodQuit();

        void SetAudioEnabled(SeatIndex roomIndex, bool enabled);
    }

    interface ICloudGameAPIEx
    {
        ISdkEnv SdkEnv { get; set; }
        IMultiplayerListener MultiplayerListener { get; }
    }
}