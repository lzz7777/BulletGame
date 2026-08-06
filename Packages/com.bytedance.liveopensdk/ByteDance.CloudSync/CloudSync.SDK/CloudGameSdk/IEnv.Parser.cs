using System.Collections.Generic;
using ByteCloudGameSdk;

namespace ByteDance.CloudSync
{
    public static class EnvParser
    {
        public static bool IsVerboseLog { get; set; } = true;

        public static EnvParams ParseLaunchParams(CloudGameAndroidLaunchParams input)
        {
            var parser = new AppParamParser();
            return parser.Parse(input.startAppParam);
        }

        public static EnvParams ParseAppParams(StartAppParam appParam)
        {
            return new EnvParams()
                .WithValue(SdkConsts.ArgAppCloudGameToken, appParam.Token)
                .WithValue(SdkConsts.ArgAppCloudDeviceId, appParam.CloudDeviceId)
                .WithValue(SdkConsts.ArgAppLogId, appParam.LogId)
                .WithValue(SdkConsts.ArgAppDevicePlatform, appParam.DevicePlatform)
                .WithValue(SdkConsts.ArgAppGameId, appParam.GameId);
        }

        public static EnvParams ParseArgs(string[] args)
        {
            var parser = new ArgLineParser();
            parser.IsVerboseLog = IsVerboseLog;
            var parsedEnv = parser.Parse(args);
            return parsedEnv;
        }

        /// <summary>
        /// GetStartAppParam
        /// commandline = "{\\cloud_device_id\\:\\7365455304537987878\\,\\game_id\\:\\com.gameplus.live.link_game.pc\\,\\sub_channel\\:\\113_test_1\\,\\token\\:\\iixUW7crRHxaoPjfc+IC1sDg6gCENeoj3CZFFadRYd4eH3mARpFCXqQLE4dxKDrQdqvO8qiyilMRPnXUMglZNqFZnUBq0PdnHPLxGER00omeTllzEMni+6b/vGOp2Ib19XBTs/93GpjftnFrVK4h5wLKgeniIAykNI01sJFVk+xt2Z8PzvmpfxvnUHdbu9rQvP+Ym6UIucEFXr7YULZqRMbHXbXyH+yOTeebk6cf/4Q/hPUReFSGQdN+LZDbDpvoMHj9TMIEHINbYKYboGaoqRoz+szrf/GC5ekSctHnnM6V3HG3tHeSVMxfIUaJ8SZn\\,\\boe\\:0,\\device_platform\\:\\ios\\,\\log_id\\:\\2024050618444657A0C28EFBA5517D632F\\,\\debug\\:2,\\app_version\\:\\299000\\,\\app_id\\:1128,\\x_play_sdk_version\\:\\8001006\\,\\plugin_version\\:\\0\\,\\heartbeat_switch\\:true,\\network_detect_switch\\:true,\\network_detect_gap\\:1,\\network_detect_times\\:5,\\live_room_id\\:\\7365835367112624906\\,\\link_room_id\\:\\7365831794903749642\\}";
        /// </summary>
        /// <returns></returns>
        public static EnvParams ParseAppParams(string s)
        {
            var parser = new AppParamParser();
            return parser.Parse(s);
        }
    }

    /// <summary>
    /// 基于语法的命令行参数解析器
    /// </summary>
    public abstract class ParserBase
    {
        protected string Source;
        private int _len;
        private int _pos;

        protected int Position => _pos;

        protected void Begin(string s)
        {
            Source = s;
            _len = s.Length;
            _pos = 0;
        }

        protected bool Eof() => _pos >= _len;

        protected char Current => Source[_pos];

        protected bool Advance()
        {
            if (_pos >= _len)
                return false;
            _pos++;
            return true;
        }

        protected bool LookAhead(int lookAhead, char lookAheadChar)
        {
            var pos = _pos + lookAhead;
            if (pos < 0 || pos >= _len) return false;
            return Source[pos] == lookAheadChar;
        }

        protected bool SkipAll(char c)
        {
            var count = 0;
            while (!Eof() && Current == c)
            {
                Advance();
                count++;
            }
            return count > 0;
        }

        protected void SkipWhiteSpace()
        {
            while (!Eof() && char.IsWhiteSpace(Current))
            {
                Advance();
            }
        }

        protected bool EatToken(char token)
        {
            if (MatchToken(token))
            {
                Advance();
                return true;
            }

            return false;
        }

        protected bool MatchToken(char token)
        {
            SkipWhiteSpace();
            return Current == token;
        }
    }

    /// <summary>
    /// 解析这种风格的配置
    /// commandline = "{\\cloud_device_id\\:\\7365455304537987878\\,\\game_id\\:\\com.gameplus.live.link_game.pc\\,\\sub_channel\\:\\113_test_1\\,\\token\\:\\iixUW7crRHxaoPjfc+IC1sDg6gCENeoj3CZFFadRYd4eH3mARpFCXqQLE4dxKDrQdqvO8qiyilMRPnXUMglZNqFZnUBq0PdnHPLxGER00omeTllzEMni+6b/vGOp2Ib19XBTs/93GpjftnFrVK4h5wLKgeniIAykNI01sJFVk+xt2Z8PzvmpfxvnUHdbu9rQvP+Ym6UIucEFXr7YULZqRMbHXbXyH+yOTeebk6cf/4Q/hPUReFSGQdN+LZDbDpvoMHj9TMIEHINbYKYboGaoqRoz+szrf/GC5ekSctHnnM6V3HG3tHeSVMxfIUaJ8SZn\\,\\boe\\:0,\\device_platform\\:\\ios\\,\\log_id\\:\\2024050618444657A0C28EFBA5517D632F\\,\\debug\\:2,\\app_version\\:\\299000\\,\\app_id\\:1128,\\x_play_sdk_version\\:\\8001006\\,\\plugin_version\\:\\0\\,\\heartbeat_switch\\:true,\\network_detect_switch\\:true,\\network_detect_gap\\:1,\\network_detect_times\\:5,\\live_room_id\\:\\7365835367112624906\\,\\link_room_id\\:\\7365831794903749642\\}";
    /// </summary>
    /// <returns></returns>
    public class AppParamParser : ParserBase
    {
        private static readonly SdkDebugLogger Debug = new (nameof(AppParamParser));
        private EnvParams _params;

        public EnvParams Parse(string s)
        {
            Debug.Log($"Begin parse: {s}");

            _params = new EnvParams();
            s = FixSource(s);
            Begin(s);
            SkipWhiteSpace();

            if (!EatToken('{'))
            {
                Debug.LogError("Invalid startAppParam string. Must be start with '{' ");
                return _params;
            }

            while (!Eof())
            {
                if (ReadKeyValue())
                {
                    if (EatToken(','))
                        continue;
                    if (EatToken('}'))
                        break;
                }

                Debug.LogError($"Env parser encountered unexpected token '{Current}' at position {Position}");
                break;
            }

            Debug.Log($"End parse: {_params}");
            return _params;
        }

        // 修复传入串，与gamesdk ParseCommandLine()一致
        private string FixSource(string s)
        {
            // 用于修复双斜杠被转义单斜杠导致的解析错误
            s = s.Replace("\\:\\,", "\\:\\\\,").Replace("\\:\\}", "\\:\\\\},");
            return s;
        }

        private bool ReadString(out string value)
        {
            value = null;
            if (!MatchToken('\\')) return false;

            var p = Position + 1;
            while (Advance())
            {
                if (Current == '\\')
                {
                    value = Source.Substring(p, Position - p);
                    Debug.Log($"read string: {value}");
                    Advance();
                    return true;
                }
            }

            return false;
        }

        private bool ReadNonString(out string value)
        {
            var p = Position;
            while (Advance())
            {
                if (Current is '\\' or ',' or '}')
                {
                    value = Source.Substring(p, Position - p);
                    return true;
                }
            }

            value = null;
            return false;
        }

        private bool ReadKeyValue()
        {
            var ok = true;

            // key
            ok &= ReadString(out var key);

            // :
            ok &= EatToken(':');

            // value
            if (ok && ReadString(out var s))
            {
                _params.Values[key] = s;
            }
            else if (ok && ReadNonString(out var n))
            {
                _params.Values[key] = n;
            }
            else
            {
                ok = false;
            }

            return ok;
        }
    }

    /// <summary>
    /// 常规命令行解析
    ///
    /// var argLine = @"M:/Games/x/ugc.exe --StartAppParam='{\cloud_device_id\:\787878\,\game_id\:\com.gameplus.live.link_game.pc\,\sub_channel\:\113_test_1\,\token\:\iixUW7crRHxaoPjfc+IC1sDg6gCENeo\,\app_version\:\299000\,\app_id\:1128,\x_play_sdk_version\:\8001006\,\live_room_id\:\7365835367112624906\,\link_room_id\:\7365831794903749642\}' -screen-fullscreen 1 -cloud-game 1 -mobile 1 -token=Z4m/xqO91cj9qbMJd/6CsZ4= -open-id=_000hr-k28Zoq";
    /// var args = new[]
    /// {
    ///     "M:/Games/x/ugc.exe",
    ///     @"--StartAppParam='{\cloud_device_id\:\787878\,\game_id\:\com.gameplus.live.link_game.pc\,\sub_channel\:\113_test_1\,\token\:\iixUW7crRHxaoPjfc+IC1sDg6gCENeo\,\app_version\:\299000\,\app_id\:1128,\x_play_sdk_version\:\8001006\,\live_room_id\:\7365835367112624906\,\link_room_id\:\7365831794903749642\}'"
    ///     , "-screen-fullscreen", "1", "-cloud-game", "1", "-mobile", "1", "-token=Z4m/xqO91cj9qbMJd/6CsZ4="
    /// };
    /// </summary>
    public class ArgLineParser : ParserBase
    {
        private static readonly SdkDebugLogger Debug = new (nameof(ArgLineParser));

        private string _str;

        public bool IsVerboseLog { get; set; }

        private bool IsTokenEnd() => Eof() || char.IsWhiteSpace(Current);

        private bool ReadParamName(out string value)
        {
            value = null;
            var p = Position;

            if (!SkipAll('-')) return false;

            while (Advance())
            {
                // case1: `param xxx`
                if (IsTokenEnd())
                {
                    value = Source.Substring(p, Position - p);
                    return true;
                }

                // case2: `param=xxx`
                if (EatToken('='))
                {
                    value = Source.Substring(p, Position - p - 1);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 读取用 ' " 括起来的字符串，形如 'xxx' "xxx"
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool ReadLiteralString(out string value)
        {
            value = null;

            var token = Current;
            if (token != '\'' && token != '\"')
                return false;
            Advance();

            var p = Position;
            while (Advance())
            {
                if (Current == token)
                {
                    value = Source.Substring(p, Position - p);
                    Advance();
                    return true;
                }
            }
            return false;
        }

        private bool ReadParamValue(out string value)
        {
            value = null;
            var p = Position;

            while (Advance())
            {
                if (IsTokenEnd())
                {
                    value = Source.Substring(p, Position - p);
                    return true;
                }
            }

            return false;
        }

        public string[] ParseIntoArray(string s)
        {
            Begin(s);

            var list = new List<string>();
            while (!Eof())
            {
                SkipWhiteSpace();
                if (ReadParamName(out var paramName))
                {
                    if (IsVerboseLog) Debug.LogDebug($"read param name: {paramName}");
                    list.Add(paramName);
                }
                else if (ReadLiteralString(out var paramValue) || ReadParamValue(out paramValue))
                {
                    if (IsVerboseLog) Debug.LogDebug($"read param value: {paramValue}");
                    list.Add(paramValue);
                }
                else
                {
                    Debug.LogError($"Unexpected char: {Current}");
                    Advance();
                }
            }

            return list.ToArray();
        }

        /// <summary>
        /// 将参数名 -name --name 这种转为 name
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private string NormalizeParamName(string s)
        {
            var i = 0;
            while (s[i] == '-')
            {
                if (++i == s.Length) return null;
            }
            return s[i..];
        }

        private EnvParams ParseEnv(string[] paramNameOrValues)
        {
            var env = new EnvParams();
            var index = 0;
            while (index < paramNameOrValues.Length)
            {
                var item = paramNameOrValues[index++];
                if (item.StartsWith('-'))
                {
                    var paramName = NormalizeParamName(item);
                    var next = index >= paramNameOrValues.Length ? null : paramNameOrValues[index];

                    if (next == null || next.StartsWith('-'))
                        env.Values[paramName] = string.Empty;
                    else
                        env.Values[paramName] = next;
                }
            }

            return env;
        }

        public EnvParams Parse(string argLine)
        {
            Debug.Log($"Parse arg line: {argLine}");
            var arr = ParseIntoArray(argLine);
            var envParams = ParseEnv(arr);
            Debug.Log($"End parse. {envParams}");
            return envParams;
        }

        public EnvParams Parse(string[] args)
        {
            Debug.Log($"Parse args: {string.Join("\n", args)}");
            var list = new List<string>();
            foreach (var arg in args)
            {
                var intoArray = ParseIntoArray(arg);
                list.AddRange(intoArray);
            }

            var envParams = ParseEnv(list.ToArray());
            if (IsVerboseLog)
                Debug.Log($"End parse. {envParams}");
            return envParams;
        }
    }
}