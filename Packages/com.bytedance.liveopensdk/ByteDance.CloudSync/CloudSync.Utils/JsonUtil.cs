// Copyright (c) Bytedance. All rights reserved.
// Description:

using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ByteDance.CloudSync
{
    public static class JsonUtil
    {
        [DebuggerStepThrough]
        public static string ToJson(object obj, bool indent = false) =>
            JsonConvert.SerializeObject(obj, indent ? Formatting.Indented : Formatting.None);

        [DebuggerStepThrough]
        public static string ToJson(object obj, bool indent, JsonSerializerSettings settings) =>
            JsonConvert.SerializeObject(obj, indent ? Formatting.Indented : Formatting.None, settings);

        [DebuggerStepThrough]
        public static string ToJson(Dictionary<string, string> stringMap, bool indent = false) =>
            JsonConvert.SerializeObject(stringMap, indent ? Formatting.Indented : Formatting.None);

        [DebuggerStepThrough]
        public static JToken ToJToken(object obj) => JToken.FromObject(obj);

        [DebuggerStepThrough]
        public static T ToObject<T>(string jsonString) => JsonConvert.DeserializeObject<T>(jsonString);
    }
}