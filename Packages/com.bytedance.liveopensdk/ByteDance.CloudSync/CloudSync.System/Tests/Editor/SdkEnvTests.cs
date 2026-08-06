// Copyright@www.bytedance.com
// Author: DONEY Dong
// Date: 2024/05/07
// Description:

using System.Collections.Generic;
using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace ByteDance.CloudSync.Tests
{
    sealed class SdkEnvTests
    {
        [Test]
        public void TestParseAppParamStyle()
        {
            var commandline = @"{\cloud_device_id\:\7365455304537987878\,\game_id\:\com.gameplus.live.link_game.pc\,\sub_channel\:\113_test_1\,\token\:\iixUW7crRHxaoPjfc+IC1sDg6gCENeoj3CZFFadRYd4eH3mARpFCXqQLE4dxKDrQdqvO8qiyilMRPnXUMglZNqFZnUBq0PdnHPLxGER00omeTllzEMni+6b/vGOp2Ib19XBTs/93GpjftnFrVK4h5wLKgeniIAykNI01sJFVk+xt2Z8PzvmpfxvnUHdbu9rQvP+Ym6UIucEFXr7YULZqRMbHXbXyH+yOTeebk6cf/4Q/hPUReFSGQdN+LZDbDpvoMHj9TMIEHINbYKYboGaoqRoz+szrf/GC5ekSctHnnM6V3HG3tHeSVMxfIUaJ8SZn\,\boe\:0,\device_platform\:\ios\,\log_id\:\2024050618444657A0C28EFBA5517D632F\,\debug\:2,\app_version\:\299000\,\app_id\:1128,\x_play_sdk_version\:\8001006\,\plugin_version\:\0\,\heartbeat_switch\:true,\network_detect_switch\:true,\network_detect_gap\:1,\network_detect_times\:5,\live_room_id\:\7365835367112624906\,\link_room_id\:\7365831794903749642\}";
            var envParams = EnvParser.ParseAppParams(commandline);
            IEnv env = new BasicEnv().Merge(envParams);
            Assert.AreEqual("7365455304537987878", env.GetStringValue("cloud_device_id"));
            Assert.AreEqual("com.gameplus.live.link_game.pc", env.GetStringValue("game_id"));
            Assert.AreEqual("7365831794903749642", env.GetStringValue("link_room_id"));
        }

        [Test]
        public void TestParseAppParamsStyle2()
        {
            var appParams = @"{\cloud_device_id\:\7423678366806629146\,\game_id\:\tt7a950c28e5a39d1810.pc\,\sub_channel\:\113_mul_1\,\token\:\cnOp0Ze/OPrDo7avoMOTxS6fI5IXQBankBhNwrmjfbTedFFi29d4+2b2kb4aHdXa/XHvlzmUKMXOO3uMRmjxshHbzaZiwAV4yqrkWvx5YeJm4lCUHxKDeTT9+CcCsXiRKLKZm4OOmxokIX5n3p4c2UDU71DkgxY+mMt8pBboBOJcKDoFv+/S2NHAa2CXeiIkFYL93LlrGEkRGUO1559vo74iatPw5FUlsUOn4BrpDEuySkioMGG0BTdB7cgsB0BjLl71fScp7uxOAoa5E8HVEQ==\,\boe\:0,\device_platform\:\pc\,\log_id\:\20241010165803F6ED1A92A711579E67A8\,\debug\:2,\app_version\:\\,\app_id\:2079,\x_play_sdk_version\:\0\,\plugin_version\:\0\,\heartbeat_switch\:false,\network_detect_switch\:true,\network_detect_gap\:1,\network_detect_times\:5,\live_room_id\:\\,\link_room_id\:\\,\frontier_service_id\:0,\env\:\ppe_cg_pk\,\custom_msg_use_data_channel\:true}";
            var envParams = EnvParser.ParseAppParams(appParams);
            Assert.AreEqual("true", envParams.Values.GetValueOrDefault("custom_msg_use_data_channel"));
            Assert.AreEqual(string.Empty, envParams.Values.GetValueOrDefault("app_version"));
        }

        [Test]
        public void TestParseAppParamsStyle3()
        {
            var appParams = @"{\cloud_device_id\:\7428412817151417129\,\game_id\:\tt794d0ff0e672ae1307.pc\,\sub_channel\:\112_danmu_1\,\token\:\6g7oW2Ke+fEesQh53ujRZ+pzdlfbkk6+Wfet/enTZVdrCeh5E83dlqbqAFr5QANsuiRJ6WkSz3mGxAftkbWNFMwmE5R1S1MB0aEdLqeOPoqSNud6Bo5GSlnpX6g5ocrtaXpPtpXwd+M2C+MuMWnLZHt0jqOGBBh+Mg6LFSdCb2F732MW3VGl9+Jw467LzwHO9S7PB9w6MsyV7wai/N9oIYMNAVpRCd8qPjNezPmMH2plIh6yIv6j5G4QHKcYreIKpqihyyZtWFlb3K1y55QGwg==\,\boe\:0,\device_platform\:\pc\,\log_id\:\2024102209595447182549130FCD07C68E\,\debug\:2,\app_version\:\,\app_id\:2079,\x_play_sdk_version\:\0\,\plugin_version\:\0\,\heartbeat_switch\:true,\network_detect_switch\:true,\network_detect_gap\:1,\network_detect_times\:5,\live_room_id\:\,\link_room_id\:\,\frontier_service_id\:0,\env\:\prod\,\custom_msg_use_data_channel\:true}";
            var envParams = EnvParser.ParseAppParams(appParams);
            Assert.AreEqual("true", envParams.Values.GetValueOrDefault("custom_msg_use_data_channel"));
            Assert.AreEqual(string.Empty, envParams.Values.GetValueOrDefault("app_version"));
            Assert.IsTrue(envParams.Values.TryGetValue("app_id", out var app_id));
            Assert.AreEqual("2079", app_id);
        }

        [Test]
        public void TestParseCommandLine()
        {
            var argLine = @"M:/Games/x/ugc.exe --StartAppParam='{\cloud_device_id\:\787878\,\game_id\:\com.gameplus.live.link_game.pc\,\sub_channel\:\113_test_1\,\token\:\iixUW7crRHxaoPjfc+IC1sDg6gCENeo\,\app_version\:\299000\,\app_id\:1128,\x_play_sdk_version\:\8001006\,\live_room_id\:\7365835367112624906\,\link_room_id\:\7365831794903749642\}' -screen-fullscreen 1 -cloud-game 1 -mobile 1 -token=Z4m/xqO91cj9qbMJd/6CsZ4= -open-id=_000hr-k28Zoq";
            var parser = new ArgLineParser();
            var envParams = parser.ParseIntoArray(argLine);
            var except = new[]
            {
                "M:/Games/x/ugc.exe",
                "--StartAppParam",
                @"{\cloud_device_id\:\787878\,\game_id\:\com.gameplus.live.link_game.pc\,\sub_channel\:\113_test_1\,\token\:\iixUW7crRHxaoPjfc+IC1sDg6gCENeo\,\app_version\:\299000\,\app_id\:1128,\x_play_sdk_version\:\8001006\,\live_room_id\:\7365835367112624906\,\link_room_id\:\7365831794903749642\}",
                "-screen-fullscreen", "1",
                "-cloud-game", "1",
                "-mobile", "1",
                "-token", "Z4m/xqO91cj9qbMJd/6CsZ4=",
                "-open-id", "_000hr-k28Zoq"
            };
            Assert.AreEqual(except, envParams);
        }

        [Test]
        public void TestParseCommandLine2()
        {
            var argLine = @"M:/Games/x/ugc.exe --StartAppParam='{\cloud_device_id\:\787878\,\game_id\:\com.gameplus.live.link_game.pc\,\sub_channel\:\113_test_1\,\token\:\iixUW7crRHxaoPjfc+IC1sDg6gCENeo\,\app_version\:\299000\,\app_id\:1128,\x_play_sdk_version\:\8001006\,\live_room_id\:\7365835367112624906\,\link_room_id\:\7365831794903749642\}' -screen-fullscreen 1 -cloud-game 1 -mobile 1 -token=Z4m/xqO91cj9qbMJd/6CsZ4= -open-id=_000hr-k28Zoq";
            var parser = new ArgLineParser();
            var envParams = parser.Parse(argLine);
            // Assert.AreEqual("787878", envParams.StringValues.GetValueOrDefault("cloud_device_id"));
            // Assert.AreEqual("com.gameplus.live.link_game.pc", envParams.StringValues.GetValueOrDefault("game_id"));
            Assert.AreEqual("Z4m/xqO91cj9qbMJd/6CsZ4=", envParams.Values.GetValueOrDefault("token"));
        }

        [Test]
        public void TestGetSet()
        {
            var env1 = new CloudSyncEnv();
            env1.SetValue("id", "1234");
            var hasId = env1.TryGetStringValue("id", out var id);
            Assert.IsTrue(hasId);
            Assert.AreEqual("1234", id);

            var env2 = new BasicEnv();
            Assert.IsTrue(env2.IsEmpty());
            Assert.IsFalse(env2.HasKey("abc"));

            env2.SetValue("abc", null);
            var hasAbc = env2.TryGetStringValue("abc", out var abc);
            Assert.IsFalse(env2.IsEmpty());
            Assert.IsTrue(env2.HasKey("abc"));
            Assert.IsTrue(hasAbc);
            Assert.AreEqual(null, abc);
        }

        [Test]
        public void TestOverride()
        {
            var env1 = new CloudSyncEnv();
            env1.SetValue("id", "1234");

            var env2 = new BasicEnv();
            env2.SetValue("abc", null);
            env1.OverrideWith(env2);

            var hasId = env1.TryGetStringValue("id", out var id);
            var hasAbc = env1.TryGetStringValue("abc", out var abc);
            Assert.IsTrue(hasId);
            Assert.AreEqual("1234", id);
            Assert.IsTrue(hasAbc);
            Assert.AreEqual(null, abc);
            
            env2.SetValue("abc", "xyz");
            env1.TryGetStringValue("abc", out var abc2);
            Assert.AreEqual("xyz", abc2);
        }

        [Test]
        public void SelfTest()
        {
            {
                var env = new SdkEnv();
                var argLine = @"M:/Games/x/ugc.exe --StartAppParam='{\cloud_device_id\:\787878\,\game_id\:\com.gameplus.live.link_game.pc\,\sub_channel\:\113_test_1\,\token\:\iixUW7crRHxaoPjfc+IC1sDg6gCENeo\,\app_version\:\299000\,\app_id\:1128,\x_play_sdk_version\:\8001006\,\live_room_id\:\7365835367112624906\,\link_room_id\:\7365831794903749642\}' -screen-fullscreen 1 -cloud-game 1 -mobile 1 -token=Z4m/xqO91cj9qbMJd/6CsZ4= -open-id=_000hr-k28Zoq";
                var args = new[]
                {
                    "M:/Games/x/ugc.exe",
                    @"--StartAppParam='{\cloud_device_id\:\787878\,\game_id\:\com.gameplus.live.link_game.pc\,\sub_channel\:\113_test_1\,\token\:\iixUW7crRHxaoPjfc+IC1sDg6gCENeo\,\app_version\:\299000\,\app_id\:1128,\x_play_sdk_version\:\8001006\,\live_room_id\:\7365835367112624906\,\link_room_id\:\7365831794903749642\}'"
                    , "-screen-fullscreen", "1", "-cloud-game", "1", "-mobile", "1", "-token=Z4m/xqO91cj9qbMJd/6CsZ4="
                };
                env.ParseArgLine(argLine);
                env.ParseArgs(args);
                env.SetReady();
                var token = env.GetLaunchToken();
                var startAppParam = env.GetStartAppParam();
                Assert.IsTrue(!string.IsNullOrEmpty(token));
                Assert.AreNotEqual(token, startAppParam);
                Assert.IsTrue(env.IsDouyin());
                Assert.IsTrue(env.IsCloud());
                Assert.IsTrue(env.IsStartFromMobile());
            }
            {
                var env = new SdkEnv();
                var argLine = @"M:/Games/x/ugc.exe --StartAppParam='{\cloud_device_id\:\787878\,\game_id\:\com.gameplus.live.link_game.pc\,\sub_channel\:\113_test_1\,\token\:\iixUW7crRHxaoPjfc+IC1sDg6gCENeo\,\app_version\:\299000\,\app_id\:1128,\x_play_sdk_version\:\8001006\,\live_room_id\:\7365835367112624906\,\link_room_id\:\7365831794903749642\}' -token=Z4m/xqO91cj9qbMJd/6CsZ4=";
                var args = new[]
                {
                    "M:/Games/x/ugc.exe",
                    @"--StartAppParam='{\cloud_device_id\:\787878\,\game_id\:\com.gameplus.live.link_game.pc\,\sub_channel\:\113_test_1\,\token\:\iixUW7crRHxaoPjfc+IC1sDg6gCENeo\,\app_version\:\299000\,\app_id\:1128,\x_play_sdk_version\:\8001006\,\live_room_id\:\7365835367112624906\,\link_room_id\:\7365831794903749642\}'"
                    , "-token=Z4m/xqO91cj9qbMJd/6CsZ4="
                };
                env.ParseArgLine(argLine);
                env.ParseArgs(args);
                env.SetReady();
                var token = env.GetLaunchToken();
                var startAppParam = env.GetStartAppParam();
                Assert.AreEqual("Z4m/xqO91cj9qbMJd/6CsZ4=", token);
                Assert.AreNotEqual(token, startAppParam);
                Assert.IsTrue(env.IsDouyin());
                Assert.IsTrue(!env.IsCloud());
                Assert.IsTrue(!env.IsStartFromMobile());
            }
            {
                var env = new SdkEnv();
                var argLine = @"M:/Games/x/ugc.exe --StartAppParam='{\cloud_device_id\:\787878\,\game_id\:\com.gameplus.live.link_game.pc\,\sub_channel\:\113_test_1\,\token\:\iixUW7crRHxaoPjfc+IC1sDg6gCENeo\,\app_version\:\299000\,\app_id\:1128,\x_play_sdk_version\:\8001006\,\live_room_id\:\7365835367112624906\,\link_room_id\:\7365831794903749642\}'";
                var args = new[]
                {
                    "M:/Games/x/ugc.exe",
                    @"--StartAppParam='{\cloud_device_id\:\787878\,\game_id\:\com.gameplus.live.link_game.pc\,\sub_channel\:\113_test_1\,\token\:\iixUW7crRHxaoPjfc+IC1sDg6gCENeo\,\app_version\:\299000\,\app_id\:1128,\x_play_sdk_version\:\8001006\,\live_room_id\:\7365835367112624906\,\link_room_id\:\7365831794903749642\}'"
                };
                env.ParseArgLine(argLine);
                env.ParseArgs(args);
                env.SetReady();
                var token = env.GetLaunchToken();
                Assert.IsTrue(string.IsNullOrEmpty(token));
                Assert.IsTrue(!env.IsDouyin());
                Assert.IsTrue(!env.IsCloud());
                Assert.IsTrue(!env.IsStartFromMobile());
            }
            {
                var env = new SdkEnv();
                var argLine = "M:/Games/x/ugc.exe -screen-fullscreen 1 -cloud-game 1 -mobile 1 -token=Z4m/xqO91cj9qbMJd/6CsZ4= -open-id=_000hr-k28Zoq";
                var args = new[]
                {
                    "M:/Games/x/ugc.exe", "-screen-fullscreen", "1", "-cloud-game", "1", "-mobile", "1", "-token=Z4m/xqO91cj9qbMJd/6CsZ4=",
                    "-open-id=_000hr-k28Zoq"
                };
                env.ParseArgLine(argLine);
                env.ParseArgs(args);
                env.SetReady();
                var token = env.GetLaunchToken();
                var startAppParam = env.GetStartAppParam();
                Assert.IsTrue(!string.IsNullOrEmpty(token));
                Assert.IsTrue(string.IsNullOrEmpty(startAppParam));
                Assert.IsTrue(env.IsDouyin());
                Assert.IsTrue(env.IsCloud());
                Assert.IsTrue(env.IsStartFromMobile());
            }
            {
                var env = new SdkEnv();
                var argLine = @"M:/Games/x/ugc.exe -token=Z4m/xqO91cj9qbMJd/6CsZ4=";
                var args = new[] { "M:/Games/x/ugc.exe", "-token=Z4m/xqO91cj9qbMJd/6CsZ4=" };
                env.ParseArgLine(argLine);
                env.ParseArgs(args);
                env.SetReady();
                var token = env.GetLaunchToken();
                var startAppParam = env.GetStartAppParam();
                Assert.IsTrue(!string.IsNullOrEmpty(token));
                Assert.IsTrue(string.IsNullOrEmpty(startAppParam));
                Assert.IsTrue(env.IsDouyin());
                Assert.IsTrue(!env.IsCloud());
                Assert.IsTrue(!env.IsStartFromMobile());
            }
            {
                var env = new SdkEnv();
                var argLine = @"M:/Games/x/ugc.exe";
                var args = new[] { "M:/Games/x/ugc.exe" };
                env.ParseArgLine(argLine);
                env.ParseArgs(args);
                env.SetReady();
                var token = env.GetLaunchToken();
                var startAppParam = env.GetStartAppParam();
                Assert.IsTrue(string.IsNullOrEmpty(token));
                Assert.IsTrue(string.IsNullOrEmpty(startAppParam));
                Assert.IsTrue(!env.IsDouyin());
                Assert.IsTrue(!env.IsCloud());
                Assert.IsTrue(!env.IsStartFromMobile());
            }
        }
    }
}