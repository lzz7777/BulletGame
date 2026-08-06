using System;
using System.Linq;
using System.Threading.Tasks;
using Sdk = ByteCloudGameSdk.Sdk;

// ReSharper disable once CheckNamespace
namespace ByteDance.CloudSync
{
    internal partial class CloudGameAPIWindows : ICloudGameAPI, ICloudGameAPIEx, ICloudGameMatchAPI
    {
        public static CloudGameAPIWindows Instance => _instance;

        // ReSharper disable once InconsistentNaming
        private static readonly CloudGameAPIWindows _instance = new();

        private CloudGameAPIWindows()
        {
        }

        public ISdkEnv SdkEnv { get; set; }
        public IMultiplayerListener MultiplayerListener { get; private set; }

        public string FileVersion => Sdk.FileVersion;

        public void SetMultiplayerListener(IMultiplayerListener listener)
        {
            MultiplayerListener = listener;
            _multiplayerCallbacks.SetListener(listener);
        }

        public Task<ICloudGameAPI.Response> Init()
        {
            try
            {
                var resp = Sdk.Init();
                CGLogger.Log("[SdkAPIWindows] resp:");
                CGLogger.Log($"{resp.code} ({(int)resp.code}) {resp.message}");
                switch (resp.code)
                {
                    case ByteCloudGameSdk.ByteCloudGameSdkErrorCode.Success:
                    case ByteCloudGameSdk.ByteCloudGameSdkErrorCode.Success_AlreadyInited:
                        SdkEnv.Merge(EnvParser.ParseAppParams(resp.startAppParam));
                        break;
                }

                var result = resp.ToApiResponse();
                return Task.FromResult(result);
            }
            catch (Exception e)
            {
                CGLogger.LogError($"[SdkAPIWindows] {e}");
                var errorMsg = $"exception in Sdk.Init: {e.GetType().Name}: {e.Message}";
                var errorResult = new ICloudGameAPI.Response(ICloudGameAPI.ErrorCode.Error, errorMsg);
                return Task.FromResult(errorResult);
            }
        }

        public async Task<ICloudGameAPI.Response> InitMultiplayer()
        {
            var resp = await Sdk.InitMultiplayer(_multiplayerCallbacks);
            return resp.ToApiResponse();
        }

        public void InitMatchAPI(IMatchAPIListener listener)
        {
            var callbacksAdapter = _matchAPICallbacks;
            callbacksAdapter.SetListener(listener);
            // note: 是同步接口，返回void
            CGLogger.LogDebug("[SdkAPIWindows] InitMatchService");
            Sdk.InitMatchService(callbacksAdapter);
        }

        /// 是否可用api: QueryRoomInfo
        public bool CanUse_QueryRoomInfo() => true;

        /// 查询房间用户信息，手动调用
        /// <para>最新版本sdk：OnPlayerJoin 时 只信任 `int roomIndex` 和 `param.RTCUserId`，总是去做 QueryRoomInfo</para>
        public async void QueryRoomInfo(SeatIndex roomIndex)
        {
            await Sdk.QueryRoomInfo(roomIndex.ToInt());
        }

        public void SetLogFunction(Action<string> sdkLog, Action<string> sdkLogError)
        {
            Sdk.SetLogFunction(msg => { sdkLog?.Invoke(msg); },
                msg => { sdkLogError?.Invoke(msg); });
        }

        public void SendPodQuit()
        {
            CGLogger.Log("[SdkAPIWindows] SendPodQuit");
            Sdk.SendPodQuit();
        }

        public Task<ICloudGameAPI.Response> SendOpenServiceCustomMessage(SeatIndex roomIndex, string msg)
        {
            throw new NotImplementedException();
        }

        public ICloudGameAPI.ErrorCode SendVideoFrame(SeatIndex roomIndex, long textureId)
        {
            var code = Sdk.SendVideoFrame(roomIndex.ToInt(), textureId);
            return code.ToApiCode();
        }

        public void SetAudioEnabled(SeatIndex roomIndex, bool enabled)
        {
            CGLogger.LogError("SetAudioEnabled not implemented on windows");
        }

        public async Task<ApiMatchStreamResponse> SendMatchBegin(ApiMatchParams matchParam)
        {
            CGLogger.LogDebug($"[SdkAPIWindows] SendMatchBegin, to host: {matchParam.hostToken}");
            var response = await Sdk.SendMatchBegin(matchParam);
            var streamResponse = new ApiMatchStreamResponse().Accept(response);
            CheckResponseError(streamResponse, "[SdkAPIWindows] SendMatchBegin error!");
            return streamResponse;
        }

        public async Task<ApiMatchStreamResponse[]> SendMatchEnd()
        {
            CGLogger.LogDebug("[SdkAPIWindows] SendMatchEnd");
            var response = await Sdk.SendMatchEnd();
            var responses = response.Select(s => new ApiMatchStreamResponse().Accept(s)).ToArray();
            var codes = string.Join(", ", responses.Select(s => s.code));
            var indexes = string.Join(", ", responses.Select(s => s.roomIndex));
            CGLogger.LogDebug($"[SdkAPIWindows] SendMatchEnd responses len: {responses.Length} code: [{codes}] roomIndex: [{indexes}]");
            foreach (var streamResponse in responses)
            {
                CheckResponseError(streamResponse, "[SdkAPIWindows] SendMatchEnd error!");
            }
            return responses;
        }

        public async Task<ApiMatchStreamResponse> SendMatchEnd(int roomIndex)
        {
            CGLogger.LogDebug($"[SdkAPIWindows] SendMatchEnd roomIndex: {roomIndex}");
            var response = await Sdk.SendMatchEnd(roomIndex);
            CGLogger.LogDebug($"[SdkAPIWindows] SendMatchEnd response code: {response.code}, roomIndex: {response.roomIndex}");
            var streamResponse = new ApiMatchStreamResponse().Accept(response);
            CheckResponseError(streamResponse, "[SdkAPIWindows] SendMatchEnd error!");
            return streamResponse;
        }

        public async Task<ICloudGameAPI.Response> SendPodCustomMessage(string token, ApiPodMessageData msgData)
        {
            CGLogger.LogDebug($"[SdkAPIWindows] SendPodCustomMessage from: {msgData.from}, to token: {token}");
            var response = await Sdk.SendPodCustomMessage(token, msgData.ToSdkPodMessage());
            CGLogger.LogDebug($"[SdkAPIWindows] SendPodCustomMessage response code: {response.code}");
            var apiResponse = response.ToApiResponse();
            CheckResponseError(apiResponse, "[SdkAPIWindows] SendPodCustomMessage error!");
            return apiResponse;
        }

        private void CheckResponseError(ApiMatchStreamResponse streamResponse, string msg)
        {
            if (streamResponse.code == ByteCloudGameSdk.MatchErrorCode.Success)
                return;
            CGLogger.LogError($"{msg} {streamResponse.ToStr()}");
        }

        private void CheckResponseError(ICloudGameAPI.Response apiResponse, string msg)
        {
            if (apiResponse.code.IsSuccessOrAlready())
                return;
            CGLogger.LogError($"{msg} {apiResponse.ToStr()}");
        }
    }
}