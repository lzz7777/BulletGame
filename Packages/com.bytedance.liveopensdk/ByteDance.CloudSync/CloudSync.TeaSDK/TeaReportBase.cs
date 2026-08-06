using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ByteDance.CloudSync.TeaSDK
{
    public class TeaReportBase
    {
        private const string TAG = "[TeaReportBase]";
        private const string REPORT_TAG = "[TeaReportPoint]";
        private static readonly Dictionary<string, TeaSdk> _userTeaSdkDic = new();


        private static readonly Dictionary<string, object> _commonParams = new();

        public static void Report(string reportName, JObject reportParam)
        {
            // 构造json & 上报
            var data = GetCommonReportJObject();
            try
            {
                reportParam.Merge(data);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return;
            }

            ReportByCloudDid(reportName, reportParam);
        }

        /// <summary>
        ///     以一个主播为维度上报埋点, 一个游戏中可能有多个主播
        /// </summary>
        /// <param name="reportName">埋点名称</param>
        /// <param name="teaInitId">上报作为的数据</param>
        /// <param name="reportParam">埋点参数</param>
        public static void ReportOneAnchor(string reportName, string teaInitId, JObject reportParam)
        {
            // 构造json & 上报
            var data = GetCommonReportJObject();
            try
            {
                reportParam.Merge(data);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return;
            }

            if (!string.IsNullOrEmpty(teaInitId))
                ReportByUid(reportName, teaInitId, reportParam);
            else
                ReportByCloudDid(reportName, reportParam);
        }


        private static JObject GetCommonReportJObject()
        {
            var data = new JObject();
            foreach (var item in _commonParams)
                data[item.Key] = JToken.FromObject(item.Value == null ? "null" : item.Value);
            return data;
        }

        /// <summary>
        ///     【保持仅Report调用】使用uid进行埋点上报
        /// </summary>
        /// <param name="reportName">上报的埋点名称</param>
        /// <param name="uid">抖音账号uid，不能为空</param>
        /// <param name="jObject">上报的埋点内容</param>
        private static void ReportByUid(string reportName, string uid, JObject jObject)
        {
            if (string.IsNullOrEmpty(uid))
            {
                Debug.LogError($"{TAG}ReportByUid uid must be not empty or null. reportName: {reportName}");
                return;
            }

            ReportWithTeaInitUid(reportName, uid, jObject, uid);
        }

        /// <summary>
        ///     【保持仅Report调用】还没有抖音账号uid时用正在分配云机器的机器的did进行上报
        /// </summary>
        /// <param name="reportName">上报的埋点名称</param>
        /// <param name="jObject">上报的埋点内容</param>
        private static void ReportByCloudDid(string reportName, JObject jObject)
        {
            ReportWithTeaInitUid(reportName, "", jObject, SystemInfo.deviceUniqueIdentifier);
        }

        /// <summary>
        ///     【保持仅Report调用】使用teaInitUid对应的TeaSdk进行上报
        /// </summary>
        /// <param name="reportName">上报的埋点名称</param>
        /// <param name="uid">上报的uid，可能为空</param>
        /// <param name="jObject">上报的埋点内容</param>
        /// <param name="teaInitUid">需要以这个uid对应的TeaSdk进行上报</param>
        private static void ReportWithTeaInitUid(string reportName, string uid, JObject jObject, string teaInitUid)
        {
            if (!TeaReportConfig.IsReport)
                return;
            jObject[TeaReportConfig.ByteIOInitReportIdParamName] = uid;
            if (!_userTeaSdkDic.ContainsKey(teaInitUid))
            {
                _userTeaSdkDic.Add(teaInitUid, new TeaSdk());
                _userTeaSdkDic[teaInitUid]
                    .Init(TeaReportConfig.ByteIOAppId, teaInitUid, "TeaReport", Application.version);
            }

            var json = jObject.ToString();
            Debug.Log($"{REPORT_TAG} name: {reportName} data: {json}");
            _userTeaSdkDic[teaInitUid].Collect(reportName, json, null);
        }

        public static void UpdateCommonParams(string key, object value)
        {
            var result = _commonParams.TryAdd(key, value);
            Debug.Log($"{TAG} - Set common params [{key}] => [{value}] {(result ? "success" : "failed")}.");
        }

        private static void AppendParams(JObject data, Dictionary<string, object> paramsToAppend)
        {
            if (paramsToAppend == null) return;

            foreach (var kvp in paramsToAppend) data[kvp.Key] = JToken.FromObject(kvp.Value);
        }

        private class StarkTeaDebugProvider : ITeaDataProvider
        {
            public string TestDeviceId => TeaReportConfig.ByteIOUserUid;
            public Dictionary<string, object> CustomValues { get; } = new();
        }
    }
}