// Copyright@www.bytedance.com
// Author: zhouxu.ken
// Date: 2024/06/11
// Description:

using System;
using System.Threading.Tasks;
using ByteDance.CloudSync.TeaSDK;
using UnityEngine;

namespace ByteDance.CloudSync
{
    internal partial class CloudGameSdkManager
    {
        private InitCloudGameResult _initResult;
        private TaskCompletionSource<InitCloudGameResult> _initTask;
        private SafeMultiplayerListener _multiplayerListenerProxy;

        /// <summary>
        /// 初始化
        /// </summary>
        public async Task<InitCloudGameResult> InitializeSdk()
        {
            if (_initTask != null)
                return await _initTask.Task;

            _initTask = new TaskCompletionSource<InitCloudGameResult>();
            try
            {
                _initResult.State = InitState.InProgress;
                _initResult = await DoInitializeSdk();
                _initTask.SetResult(_initResult);
                // 发生错误，置为 null，允许下次可以再调用
                if (!_initResult.State.IsSuccessOrAlready())
                    _initTask = null;
                return _initResult;
            }
            catch (Exception e)
            {
                _initResult.State = InitState.Error;
                _initResult.Code = ICloudGameAPI.ErrorCode.Error;
                _initResult.Error = e.Message;
                _initTask.SetException(e);
                // 发生错误，置为 null，允许下次可以再调用
                _initTask = null;
                throw;
            }
        }

        /// <summary>
        /// 初始化-Sdk
        /// </summary>
        private async Task<InitCloudGameResult> DoInitializeSdk()
        {
            if (!SdkFeatureEnabled)
            {
                var error = $"{LogTag}feature not enabled.";
                CGLogger.LogWarning(error);
                return new InitCloudGameResult
                {
                    State = InitState.Error,
                    Code = ICloudGameAPI.ErrorCode.Error,
                    Error = error
                };
            }

            // prepare
            if (CloudGameSdk.API is ICloudGameAPIEx apiEx)
                apiEx.SdkEnv = CloudSyncSdk.InternalEnv.SdkEnv;
            CloudGameSdk.API.SetLogFunction(SDKLog, SDKLogError);
            CloudGameSdk.API.SetMultiplayerListener(_multiplayerListenerProxy = new SafeMultiplayerListener(this));
            TeaReport.Report_sdk_set_listener();

            InitCloudGameResult result;

            // step1: _SdkInit
            {
                result = await _SdkInit();
                CGLogger.Log($"{LogTag}_SdkInit, state: {result.State}");
                if (result.State.IsSuccessOrAlready() == false)
                {
                    var errorLog = $"{LogTag}_SdkInit error! code: {result.Code}, message: {result.Error}";
                    CGLogger.LogError(errorLog);
                    return result;
                }
            }

            // step2: _SdkInitMultiplayer
            {
                result = await _SdkInitMultiplayer();
                CGLogger.Log($"{LogTag}_SdkInitMultiplayer, state: {result.State}");
                if (result.State.IsSuccessOrAlready() == false)
                {
                    var errorLog = $"{LogTag}_SdkInitMultiplayer error! code: {result.Code}, message: {result.Error}";
                    CGLogger.LogErrorWarning(errorLog, !Application.isEditor);
                    return result;
                }
            }

            var isEnvReady = SdkEnv.IsEnvReady;
            var isCloud = SdkEnv.IsCloud();
            var isDouyin = SdkEnv.IsDouyin();
            TeaReportBase.UpdateCommonParams("is_douyin", isDouyin);
            CGLogger.Log($"{LogTag}Initialize Sdk success. code: {result.Code}, message: {result.Error}, isEnvReady: {isEnvReady}, isCloud: {isCloud}, isDouyin: {isDouyin}");
            return result;
        }

        /// <summary>
        /// 初始化-Sdk #1
        /// </summary>
        private async Task<InitCloudGameResult> _SdkInit()
        {
            var reportParam = new StartStageParam(StartStage.Sdk_Init);
            CGLogger.Log($"{LogTag}CloudGameSdk.API.Init...");
            TeaReport.Report_sdk_init_start();
            var resp = await CloudGameSdk.API.Init();
            var code = resp.code;
            var result = new InitCloudGameResult
            {
                Code = code,
                Error = resp.message
            };

            switch (code)
            {
                case ICloudGameAPI.ErrorCode.Success:
                case ICloudGameAPI.ErrorCode.Success_AlreadyInited:
                    SetSdkInitEnvReady();
                    CGLogger.Log($"{LogTag}CloudGameSdk.API.Init result code {code}, message: {result.Error}");
                    NotifyProgress("CloudGameSdk.API.Init Success");
                    result.State = InitState.Success;
                    break;
                default:
                    if (code is ICloudGameAPI.ErrorCode.Err_Sdk_Init_Invalid_Arg or ICloudGameAPI.ErrorCode.Err_Frontier_Init_InvalidArg)
                        result.State = InitState.InvalidArg;
                    else
                        result.State = InitState.Error;

                    result.Error ??= code.ToString();
                    var errorLog = $"{LogTag}CloudGameSdk.API.Init error: {code} = {(int)code}, message: {result.Error}";
                    CGLogger.LogErrorWarning(errorLog, !Application.isEditor);
                    NotifyProgress($"CloudGameSdk.API.Init error: {code} = {(int)code}");
                    break;
            }
            TeaReport.Report_sdk_init_end((int)code, result.State == InitState.Success);
            ReportInit(reportParam, result);
            return result;
        }

        /// <summary>
        /// 初始化-Sdk #2
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        private async Task<InitCloudGameResult> _SdkInitMultiplayer()
        {
            var reportParam = new StartStageParam(StartStage.Sdk_InitMultiplayer);
            var isCloud = SdkEnv.IsCloud();
            CGLogger.Log($"{LogTag}await CloudGameSdk.API.InitMultiplayer... , isCloud: {isCloud}");
            TeaReport.Report_sdk_init_multiplayer_start();
            var resp = await CloudGameSdk.API.InitMultiplayer();
            var code = resp.code;
            var result = new InitCloudGameResult
            {
                Code = code,
                Error = resp.message
            };

            switch (code)
            {
                case ICloudGameAPI.ErrorCode.Success:
                case ICloudGameAPI.ErrorCode.Success_AlreadyInited:
                    CGLogger.Log($"{LogTag}CloudGameSdk.API.InitMultiplayer result code {code}, message: {result.Error}");
                    NotifyProgress("CloudGameSdk.API.InitMultiplayer Success");
                    result.State = InitState.Success;
                    break;
                default:
                    if (code is ICloudGameAPI.ErrorCode.Err_Sdk_Init_Invalid_Arg or ICloudGameAPI.ErrorCode.Err_Frontier_Init_InvalidArg)
                        result.State = InitState.InvalidArg;
                    else
                        result.State = InitState.Error;

                    result.Error ??= code.ToString();
                    var errorLog = $"{LogTag}CloudGameSdk.API.InitMultiplayer error: {code} = {(int)code}, message: {result.Error}";
                    CGLogger.LogErrorWarning(errorLog, !Application.isEditor && isCloud);
                    NotifyProgress($"CloudGameSdk.API.InitMultiplayer error: {code} = {(int)code}");
                    break;
            }

            TeaReport.Report_sdk_init_multiplayer_end((int)code, result.State == InitState.Success);
            ReportInit(reportParam, result);
            return result;
        }
    }

    /// <summary>
    /// Sdk 初始化状态
    /// </summary>
    public enum InitState
    {
        None = 0,
        InProgress,
        Success,
        SuccessAlready,
        InvalidArg = 11,
        Error = 500,
    }

    public static class InitStateExtension
    {
        public static bool IsSuccessOrAlready(this InitState state)
        {
            return state is InitState.Success or InitState.SuccessAlready;
        }
    }
}