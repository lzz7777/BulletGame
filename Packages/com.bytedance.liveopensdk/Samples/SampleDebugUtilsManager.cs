using System;
using System.Threading;
using ByteDance.Live.Foundation.Logging;
using ByteDance.LiveOpenSdk;
using ByteDance.LiveOpenSdk.DebugUtils;
using ByteDance.LiveOpenSdk.Runtime;
using ByteDance.LiveOpenSdk.Runtime.Utilities;
using ByteDance.LiveOpenSdk.Utilities;
using UnityEngine;

namespace Douyin.LiveOpenSDK.Samples
{
    public class SampleDebugUtilsManager
    {
        private static readonly LogWriter Log = new LogWriter(SdkUnityLogger.LogSink, "SampleGameStartup");
        private static IDebugUtilsService DebugUtilsService => SampleLiveOpenSdkManager.Sdk.GetDebugUtilsService();


        /// <summary>
        /// 上报日志 <see cref="IDebugUtilsService.UploadLog"/>
        /// </summary>
        public static void UploadLog(
            UploadLogLevel logLevel,
            string content,
            string? traceId = null)
        {
            try
            {
                Log.Info($"上报日志：logLevel: {logLevel}, traceId: {traceId}, content: {content}");
                DebugUtilsService.UploadLog(logLevel, content, traceId);
            }
            catch (Exception)
            {
                // 正常情况下不会失败，若遇到问题，请和我们联系。
                Log.Error($"上报日志：失败");
                throw;
            }
        }
        /// <summary>
        /// 上报日志 <see cref="IDebugUtilsService.UploadLogWithTag"/>
        /// </summary>
        public static void UploadLogWithTag(
            UploadLogLevel logLevel,
            string tag,
            string content,
            string? traceId = null)
        {
            try
            {
                Log.Info($"上报日志：logLevel: {logLevel}, traceId: {traceId}, content: {content}, tag: {tag}");
                DebugUtilsService.UploadLogWithTag(logLevel, tag, content, traceId);
            }
            catch (Exception)
            {
                // 正常情况下不会失败，若遇到问题，请和我们联系。
                Log.Error($"上报日志：失败");
                throw;
            }
        }
        /// <summary>
        /// 上报日志 <see cref="IDebugUtilsService.UploadLogWithTags"/>
        /// </summary>
        public static void UploadLogWithTags(
            UploadLogLevel logLevel,
            string[]? tags,
            string content,
            string? traceId = null)
        {
            try
            {
                string tagsStr = tags == null? "" : string.Join(",", tags);
                Log.Info($"上报日志：logLevel: {logLevel}, traceId: {traceId}, content: {content}, tags: [{tagsStr}]");
                DebugUtilsService.UploadLogWithTags(logLevel, tags, content, traceId);
            }
            catch (Exception)
            {
                // 正常情况下不会失败，若遇到问题，请和我们联系。
                Log.Error($"上报日志：失败");
                throw;
            }
        }
    }
}