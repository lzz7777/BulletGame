// ReSharper disable RedundantUsingDirective
// ReSharper disable UnusedType.Global
// ReSharper disable InconsistentNaming

using System;
using Newtonsoft.Json.Linq;

namespace ByteDance.CloudSync.TeaSDK
{
    public partial class TeaReport
    {
        
            public static long CloudgamesystemLoadStartTime { get; set; }
            /// <summary>
            /// 云系统加载开始
            /// </summary>
            public static void Report_cloudgamesystem_load_start() 
            {
                CloudgamesystemLoadStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var reportParams = new JObject();
                
                TeaReportBase.Report("cloudgamesystem_load_start", reportParams);
            }

            
            /// <summary>
            /// 云系统加载结束
            /// </summary>
            public static void Report_cloudgamesystem_load_end(int duration = 0) 
            {
                
                var reportParams = new JObject();
                
                if (duration == 0)
                {
                   duration = (int) (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - CloudgamesystemLoadStartTime);
                }
                reportParams["duration"] = duration;
                TeaReportBase.Report("cloudgamesystem_load_end", reportParams);
            }

            public static long CloudgamesystemInitStartTime { get; set; }
            /// <summary>
            /// 云系统初始化开始
            /// </summary>
            public static void Report_cloudgamesystem_init_start() 
            {
                CloudgamesystemInitStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var reportParams = new JObject();
                
                TeaReportBase.Report("cloudgamesystem_init_start", reportParams);
            }

            
            /// <summary>
            /// 云系统初始化结束
            /// </summary>
            public static void Report_cloudgamesystem_init_end(bool result, int duration = 0) 
            {
                
                var reportParams = new JObject();
                
                reportParams["result"] = result;
                if (duration == 0)
                {
                   duration = (int) (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - CloudgamesystemInitStartTime);
                }
                reportParams["duration"] = duration;
                TeaReportBase.Report("cloudgamesystem_init_end", reportParams);
            }

            
            /// <summary>
            /// 云游戏SDK设置监听器
            /// </summary>
            public static void Report_sdk_set_listener(int duration = 0) 
            {
                
                var reportParams = new JObject();
                
                reportParams["duration"] = duration;
                TeaReportBase.Report("sdk_set_listener", reportParams);
            }

            public static long SdkInitStartTime { get; set; }
            /// <summary>
            /// 云游戏SDK初始化开始
            /// </summary>
            public static void Report_sdk_init_start() 
            {
                SdkInitStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var reportParams = new JObject();
                
                TeaReportBase.Report("sdk_init_start", reportParams);
            }

            
            /// <summary>
            /// 云游戏SDK初始化结束
            /// </summary>
            public static void Report_sdk_init_end(int code, bool result, int duration = 0) 
            {
                
                var reportParams = new JObject();
                
                reportParams["code"] = code;
                reportParams["result"] = result;
                if (duration == 0)
                {
                   duration = (int) (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - SdkInitStartTime);
                }
                reportParams["duration"] = duration;
                TeaReportBase.Report("sdk_init_end", reportParams);
            }

            public static long SdkInitMultiplayerStartTime { get; set; }
            /// <summary>
            /// 云游戏SDK多人初始化开始
            /// </summary>
            public static void Report_sdk_init_multiplayer_start() 
            {
                SdkInitMultiplayerStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var reportParams = new JObject();
                
                TeaReportBase.Report("sdk_init_multiplayer_start", reportParams);
            }

            
            /// <summary>
            /// 云游戏SDK多人初始化结束
            /// </summary>
            public static void Report_sdk_init_multiplayer_end(int code, bool result, int duration = 0) 
            {
                
                var reportParams = new JObject();
                
                reportParams["code"] = code;
                reportParams["result"] = result;
                if (duration == 0)
                {
                   duration = (int) (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - SdkInitMultiplayerStartTime);
                }
                reportParams["duration"] = duration;
                TeaReportBase.Report("sdk_init_multiplayer_end", reportParams);
            }

            public static long CloudclientmanagerInitStartTime { get; set; }
            /// <summary>
            /// cloudclientmanager初始化开始
            /// </summary>
            public static void Report_cloudclientmanager_init_start() 
            {
                CloudclientmanagerInitStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var reportParams = new JObject();
                
                TeaReportBase.Report("cloudclientmanager_init_start", reportParams);
            }

            
            /// <summary>
            /// cloudclientmanager初始化结束
            /// </summary>
            public static void Report_cloudclientmanager_init_end(int duration = 0) 
            {
                
                var reportParams = new JObject();
                
                if (duration == 0)
                {
                   duration = (int) (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - CloudclientmanagerInitStartTime);
                }
                reportParams["duration"] = duration;
                TeaReportBase.Report("cloudclientmanager_init_end", reportParams);
            }

            public static long CloudmatchmanagerInitStartTime { get; set; }
            /// <summary>
            /// cloudmatchmanager初始化开始
            /// </summary>
            public static void Report_cloudmatchmanager_init_start() 
            {
                CloudmatchmanagerInitStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var reportParams = new JObject();
                
                TeaReportBase.Report("cloudmatchmanager_init_start", reportParams);
            }

            
            /// <summary>
            /// cloudmatchmanager初始化结束
            /// </summary>
            public static void Report_cloudmatchmanager_init_end(string domain, int duration = 0) 
            {
                
                var reportParams = new JObject();
                
                reportParams["domain"] = domain;
                if (duration == 0)
                {
                   duration = (int) (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - CloudmatchmanagerInitStartTime);
                }
                reportParams["duration"] = duration;
                TeaReportBase.Report("cloudmatchmanager_init_end", reportParams);
            }

            public static long CloudmatchmanagerFetchPlayerInfoStartTime { get; set; }
            /// <summary>
            /// cloudmatchmanager获取用户信息开始
            /// </summary>
            public static void Report_cloudmatchmanager_fetch_player_info_start() 
            {
                CloudmatchmanagerFetchPlayerInfoStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var reportParams = new JObject();
                
                TeaReportBase.Report("cloudmatchmanager_fetch_player_info_start", reportParams);
            }

            
            /// <summary>
            /// cloudmatchmanager获取用户信息结束
            /// </summary>
            public static void Report_cloudmatchmanager_fetch_player_info_end(int code, bool result, int duration = 0) 
            {
                
                var reportParams = new JObject();
                
                reportParams["code"] = code;
                reportParams["result"] = result;
                if (duration == 0)
                {
                   duration = (int) (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - CloudmatchmanagerFetchPlayerInfoStartTime);
                }
                reportParams["duration"] = duration;
                TeaReportBase.Report("cloudmatchmanager_fetch_player_info_end", reportParams);
            }

            
            /// <summary>
            /// 玩家状态变化
            /// </summary>
            public static void Report_player_state_change(int player_state) 
            {
                
                var reportParams = new JObject();
                
                reportParams["player_state"] = player_state;
                TeaReportBase.Report("player_state_change", reportParams);
            }

            
            /// <summary>
            /// 开始等待第一个主播进入
            /// </summary>
            public static void Report_anchor_wait_join() 
            {
                
                var reportParams = new JObject();
                
                TeaReportBase.Report("anchor_wait_join", reportParams);
            }

            
            /// <summary>
            /// 第一个主播进入
            /// </summary>
            public static void Report_anchor_joined(int duration = 0) 
            {
                
                var reportParams = new JObject();
                
                reportParams["duration"] = duration;
                TeaReportBase.Report("anchor_joined", reportParams);
            }

            
            /// <summary>
            /// 云游戏SDK玩家进入
            /// </summary>
            public static void Report_sdk_on_player_join(int code, string link_room_id, int room_index, string rtc_user_id) 
            {
                
                var reportParams = new JObject();
                
                reportParams["code"] = code;
                reportParams["link_room_id"] = link_room_id;
                reportParams["room_index"] = room_index;
                reportParams["rtc_user_id"] = rtc_user_id;
                TeaReportBase.Report("sdk_on_player_join", reportParams);
            }

            public static long PlayerJoinFetchPlayerInfoStartTime { get; set; }
            /// <summary>
            /// 获取进入玩家信息开始
            /// </summary>
            public static void Report_player_join_fetch_player_info_start() 
            {
                PlayerJoinFetchPlayerInfoStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var reportParams = new JObject();
                
                TeaReportBase.Report("player_join_fetch_player_info_start", reportParams);
            }

            
            /// <summary>
            /// 获取进入玩家信息结束
            /// </summary>
            public static void Report_player_join_fetch_player_info_end(int code, bool result, int room_index, int duration = 0) 
            {
                
                var reportParams = new JObject();
                
                reportParams["code"] = code;
                reportParams["result"] = result;
                reportParams["room_index"] = room_index;
                if (duration == 0)
                {
                   duration = (int) (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - PlayerJoinFetchPlayerInfoStartTime);
                }
                reportParams["duration"] = duration;
                TeaReportBase.Report("player_join_fetch_player_info_end", reportParams);
            }

            
            /// <summary>
            /// 虚拟屏幕开启
            /// </summary>
            public static void Report_visual_screen_enable(int room_index) 
            {
                
                var reportParams = new JObject();
                
                reportParams["room_index"] = room_index;
                TeaReportBase.Report("visual_screen_enable", reportParams);
            }

            
            /// <summary>
            /// 虚拟输入器开启
            /// </summary>
            public static void Report_visual_device_enable(int room_index) 
            {
                
                var reportParams = new JObject();
                
                reportParams["room_index"] = room_index;
                TeaReportBase.Report("visual_device_enable", reportParams);
            }

            
            /// <summary>
            /// host首帧
            /// </summary>
            public static void Report_host_first_frame(int duration = 0) 
            {
                
                var reportParams = new JObject();
                
                reportParams["duration"] = duration;
                TeaReportBase.Report("host_first_frame", reportParams);
            }

            public static long CloudmatchmanagerRequestMatchStartTime { get; set; }
            /// <summary>
            /// 外部接口触发匹配功能开始
            /// </summary>
            public static void Report_cloudmatchmanager_request_match_start(int current_match_state, bool is_manual_switch, int match_appid, string match_param_json, string match_tag, string pool_name) 
            {
                CloudmatchmanagerRequestMatchStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var reportParams = new JObject();
                
                reportParams["current_match_state"] = current_match_state;
                reportParams["is_manual_switch"] = is_manual_switch;
                reportParams["match_appid"] = match_appid;
                reportParams["match_param_json"] = match_param_json;
                reportParams["match_tag"] = match_tag;
                reportParams["pool_name"] = pool_name;
                TeaReportBase.Report("cloudmatchmanager_request_match_start", reportParams);
            }

            
            /// <summary>
            /// 外部接口触发匹配功能结束
            /// </summary>
            public static void Report_cloudmatchmanager_request_match_end(int code, bool is_host, bool is_manual_switch, string logid, int match_appid, string match_param_json, string match_tag, string pool_name, bool result, int room_index, int teams_count, int duration = 0) 
            {
                
                var reportParams = new JObject();
                
                reportParams["code"] = code;
                reportParams["is_host"] = is_host;
                reportParams["is_manual_switch"] = is_manual_switch;
                reportParams["logid"] = logid;
                reportParams["match_appid"] = match_appid;
                reportParams["match_param_json"] = match_param_json;
                reportParams["match_tag"] = match_tag;
                reportParams["pool_name"] = pool_name;
                reportParams["result"] = result;
                reportParams["room_index"] = room_index;
                reportParams["teams_count"] = teams_count;
                if (duration == 0)
                {
                   duration = (int) (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - CloudmatchmanagerRequestMatchStartTime);
                }
                reportParams["duration"] = duration;
                TeaReportBase.Report("cloudmatchmanager_request_match_end", reportParams);
            }

            public static long MatchserviceStartMatchStartTime { get; set; }
            /// <summary>
            /// 触发匹配服务开始
            /// </summary>
            public static void Report_matchservice_start_match_start() 
            {
                MatchserviceStartMatchStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var reportParams = new JObject();
                
                TeaReportBase.Report("matchservice_start_match_start", reportParams);
            }

            
            /// <summary>
            /// 触发匹配服务结束
            /// </summary>
            public static void Report_matchservice_start_match_end(int code, string olympus_appid, bool result, int duration = 0) 
            {
                
                var reportParams = new JObject();
                
                reportParams["code"] = code;
                reportParams["olympus_appid"] = olympus_appid;
                reportParams["result"] = result;
                if (duration == 0)
                {
                   duration = (int) (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - MatchserviceStartMatchStartTime);
                }
                reportParams["duration"] = duration;
                TeaReportBase.Report("matchservice_start_match_end", reportParams);
            }

            public static long WaitPlayerJoinStartTime { get; set; }
            /// <summary>
            /// 等待非房主玩家进入开始
            /// </summary>
            public static void Report_wait_player_join_start(int timeout_ms) 
            {
                WaitPlayerJoinStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var reportParams = new JObject();
                
                reportParams["timeout_ms"] = timeout_ms;
                TeaReportBase.Report("wait_player_join_start", reportParams);
            }

            
            /// <summary>
            /// 等待非房主玩家进入结束
            /// </summary>
            public static void Report_wait_player_join_end(bool result, int duration = 0) 
            {
                
                var reportParams = new JObject();
                
                reportParams["result"] = result;
                if (duration == 0)
                {
                   duration = (int) (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - WaitPlayerJoinStartTime);
                }
                reportParams["duration"] = duration;
                TeaReportBase.Report("wait_player_join_end", reportParams);
            }

            public static long SdkSendMatchStartTime { get; set; }
            /// <summary>
            /// 发起sdk切流开始
            /// </summary>
            public static void Report_sdk_send_match_start() 
            {
                SdkSendMatchStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var reportParams = new JObject();
                
                TeaReportBase.Report("sdk_send_match_start", reportParams);
            }

            
            /// <summary>
            /// 发起sdk切流结束
            /// </summary>
            public static void Report_sdk_send_match_end(int code, string host_token, string logid, string match_key, bool result, int room_index, int duration = 0) 
            {
                
                var reportParams = new JObject();
                
                reportParams["code"] = code;
                reportParams["host_token"] = host_token;
                reportParams["logid"] = logid;
                reportParams["match_key"] = match_key;
                reportParams["result"] = result;
                reportParams["room_index"] = room_index;
                if (duration == 0)
                {
                   duration = (int) (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - SdkSendMatchStartTime);
                }
                reportParams["duration"] = duration;
                TeaReportBase.Report("sdk_send_match_end", reportParams);
            }

            
            /// <summary>
            /// 非房主玩家离屏推流第一帧
            /// </summary>
            public static void Report_non_host_send_frame(int duration = 0) 
            {
                
                var reportParams = new JObject();
                
                reportParams["duration"] = duration;
                TeaReportBase.Report("non_host_send_frame", reportParams);
            }

            public static long CloudmatchmanagerEndMatchStartTime { get; set; }
            /// <summary>
            /// 外部接口触发结束匹配功能开始
            /// </summary>
            public static void Report_cloudmatchmanager_end_match_start() 
            {
                CloudmatchmanagerEndMatchStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var reportParams = new JObject();
                
                TeaReportBase.Report("cloudmatchmanager_end_match_start", reportParams);
            }

            
            /// <summary>
            /// 外部接口触发结束匹配功能结束
            /// </summary>
            public static void Report_cloudmatchmanager_end_match(int code, string end_info, bool is_host, string logid, bool result, int room_index, int duration = 0) 
            {
                
                var reportParams = new JObject();
                
                reportParams["code"] = code;
                reportParams["end_info"] = end_info;
                reportParams["is_host"] = is_host;
                reportParams["logid"] = logid;
                reportParams["result"] = result;
                reportParams["room_index"] = room_index;
                reportParams["duration"] = duration;
                TeaReportBase.Report("cloudmatchmanager_end_match", reportParams);
            }

            public static long SdkSendMatchEndStartTime { get; set; }
            /// <summary>
            /// 发起sdk结束推流开始
            /// </summary>
            public static void Report_sdk_send_match_end_start() 
            {
                SdkSendMatchEndStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var reportParams = new JObject();
                
                TeaReportBase.Report("sdk_send_match_end_start", reportParams);
            }

            
            /// <summary>
            /// 发起sdk结束推流结束
            /// </summary>
            public static void Report_sdk_send_match_end_end(int code, string logid, bool result, int room_index) 
            {
                
                var reportParams = new JObject();
                
                reportParams["code"] = code;
                reportParams["logid"] = logid;
                reportParams["result"] = result;
                reportParams["room_index"] = room_index;
                TeaReportBase.Report("sdk_send_match_end_end", reportParams);
            }

            
            /// <summary>
            /// sdk发送信息给其他pod
            /// </summary>
            public static void Report_sdk_send_pod_message(string from) 
            {
                
                var reportParams = new JObject();
                
                reportParams["from"] = from;
                TeaReportBase.Report("sdk_send_pod_message", reportParams);
            }

            
            /// <summary>
            /// sdk收到其他pod发来的消息
            /// </summary>
            public static void Report_sdk_on_pod_message(string from) 
            {
                
                var reportParams = new JObject();
                
                reportParams["from"] = from;
                TeaReportBase.Report("sdk_on_pod_message", reportParams);
            }

            
            /// <summary>
            /// sdk收到玩家离开
            /// </summary>
            public static void Report_sdk_on_player_exit(int room_index) 
            {
                
                var reportParams = new JObject();
                
                reportParams["room_index"] = room_index;
                TeaReportBase.Report("sdk_on_player_exit", reportParams);
            }

            
            /// <summary>
            /// 作为host
            /// </summary>
            public static void Report_switchmanager_begin_host() 
            {
                
                var reportParams = new JObject();
                
                TeaReportBase.Report("switchmanager_begin_host", reportParams);
            }

            public static long SwitchmanagerSwitchtoStartTime { get; set; }
            /// <summary>
            /// 自定义匹配开始
            /// </summary>
            public static void Report_switchmanager_switchto_start(int room_index) 
            {
                SwitchmanagerSwitchtoStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var reportParams = new JObject();
                
                reportParams["room_index"] = room_index;
                TeaReportBase.Report("switchmanager_switchto_start", reportParams);
            }

            
            /// <summary>
            /// 自定义匹配结束
            /// </summary>
            public static void Report_switchmanager_switchto_end(int code, string match_id, bool result, int room_index, string switch_token, int duration = 0) 
            {
                
                var reportParams = new JObject();
                
                reportParams["code"] = code;
                reportParams["match_id"] = match_id;
                reportParams["result"] = result;
                reportParams["room_index"] = room_index;
                reportParams["switch_token"] = switch_token;
                if (duration == 0)
                {
                   duration = (int) (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - SwitchmanagerSwitchtoStartTime);
                }
                reportParams["duration"] = duration;
                TeaReportBase.Report("switchmanager_switchto_end", reportParams);
            }

            
            /// <summary>
            /// 外部接口触发结束匹配功能结束
            /// </summary>
            public static void Report_cloudmatchmanager_end_match_end(int code, string end_info, bool is_host, string logid, bool result, int room_index, int duration = 0) 
            {
                
                var reportParams = new JObject();
                
                reportParams["code"] = code;
                reportParams["end_info"] = end_info;
                reportParams["is_host"] = is_host;
                reportParams["logid"] = logid;
                reportParams["result"] = result;
                reportParams["room_index"] = room_index;
                if (duration == 0)
                {
                   duration = (int) (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - CloudmatchmanagerEndMatchStartTime);
                }
                reportParams["duration"] = duration;
                TeaReportBase.Report("cloudmatchmanager_end_match_end", reportParams);
            }

            public static long CloudswitchmanagerInitStartTime { get; set; }
            /// <summary>
            /// cloudswitchmanager初始化开始
            /// </summary>
            public static void Report_cloudswitchmanager_init_start() 
            {
                CloudswitchmanagerInitStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var reportParams = new JObject();
                
                TeaReportBase.Report("cloudswitchmanager_init_start", reportParams);
            }

            
            /// <summary>
            /// cloudswitchmanager初始化结束
            /// </summary>
            public static void Report_cloudswitchmanager_init_end(int duration = 0) 
            {
                
                var reportParams = new JObject();
                
                if (duration == 0)
                {
                   duration = (int) (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - CloudswitchmanagerInitStartTime);
                }
                reportParams["duration"] = duration;
                TeaReportBase.Report("cloudswitchmanager_init_end", reportParams);
            }

            public static long AnchorWaitJoinStartTime { get; set; }
            /// <summary>
            /// 开始等待第一个主播进入
            /// </summary>
            public static void Report_anchor_wait_join_start() 
            {
                AnchorWaitJoinStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var reportParams = new JObject();
                
                TeaReportBase.Report("anchor_wait_join_start", reportParams);
            }

            
            /// <summary>
            /// 第一个主播进入
            /// </summary>
            public static void Report_anchor_wait_join_end(int duration = 0) 
            {
                
                var reportParams = new JObject();
                
                if (duration == 0)
                {
                   duration = (int) (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - AnchorWaitJoinStartTime);
                }
                reportParams["duration"] = duration;
                TeaReportBase.Report("anchor_wait_join_end", reportParams);
            }

    }
}