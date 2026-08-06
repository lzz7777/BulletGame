// Copyright (c) Bytedance. All rights reserved.
// Author: DONEY Dong
// Date: 2025/04/10
// Description:

using ByteDance.CloudSync.MatchManager;
using NUnit.Framework;

namespace ByteDance.CloudSync.Tests
{
    [TestFixture]
    public class TestMatchConfig
    {
        private static string AppId { get; } = "tt12341234";
        private static int OlympusAppId { get; } = 12345;


        [Test]
        public void TestMatchConfig_Simple_Ctor()
        {
            var config = new SimpleMatchConfig();
            Assert.That(config, Is.Not.Null);
            Assert.That(config.PoolType, Is.EqualTo(SimpleMatchPoolType.P1v1));
            Assert.That(config.MatchTag, Is.Null.Or.Empty);
        }

        [Test]
        public void TestMatchConfig_Simple_ToMatchConfig()
        {
            var config = new SimpleMatchConfig();
            var matchConfig = config.ToMatchConfig();
            Assert.That(matchConfig, Is.Not.Null);
            Assert.That(matchConfig.MatchAppId, Is.EqualTo(SimpleMatchConfigExtension.PublicMatchAppId));
            Assert.That(matchConfig.PoolName, Does.Contain("1v1"));
            Assert.That(matchConfig.PoolName, Is.EqualTo(SimpleMatchConfigExtension.GetSimpleMatchPool(SimpleMatchPoolType.P1v1)));
            Assert.That(matchConfig.MatchTag, Is.Null.Or.Empty);
        }

        [Test]
        public void TestMatchConfig_Simple_PoolEnum()
        {
            Assert.That(SimpleMatchConfigExtension.GetSimpleMatchPool(SimpleMatchPoolType.P1v1), Does.Contain("1v1"));
            Assert.That(SimpleMatchConfigExtension.GetSimpleMatchPool(SimpleMatchPoolType.P2v2), Does.Contain("2v2"));
            Assert.That(SimpleMatchConfigExtension.GetSimpleMatchPool(SimpleMatchPoolType.P1x4), Does.Contain("x4"));
            Assert.That(SimpleMatchConfigExtension.GetSimpleMatchPool(SimpleMatchPoolType.P1x3), Does.Contain("x3"));
        }

        [Test]
        public void TestMatchConfig_InternalMatchConfig_Ctor()
        {
            var config = new InternalMatchConfig();
            Assert.That(config, Is.Not.Null);
            Assert.That(config.MatchAppId, Is.EqualTo(0));
            Assert.That(config.PoolName, Is.Null.Or.Empty);
            Assert.That(config.MatchTag, Is.Null.Or.Empty);
        }

        [Test]
        public void TestMatchConfig_InternalMatchConfig_CreateFrom()
        {
            var simpleConfig = new SimpleMatchConfig();
            var matchConfig = simpleConfig.ToMatchConfig();
            var internalConfig = InternalMatchConfig.CreateFrom(matchConfig, AppId);
            Assert.That(internalConfig, Is.Not.Null);
            Assert.That(internalConfig.AppId, Is.EqualTo(AppId));
            Assert.That(internalConfig.MatchAppId, Is.EqualTo(SimpleMatchConfigExtension.PublicMatchAppId));
            Assert.That(internalConfig.PoolName, Is.EqualTo(SimpleMatchConfigExtension.GetSimpleMatchPool(SimpleMatchPoolType.P1v1)));
            Assert.That(internalConfig.MatchTag, Is.EqualTo($"{simpleConfig.MatchTag}-{AppId}"));
        }

        [Test]
        public void TestMatchConfig_InternalMatchConfig_ToMatchInfo()
        {
            var simpleConfig = new SimpleMatchConfig();
            var matchConfig = simpleConfig.ToMatchConfig();
            var internalConfig = InternalMatchConfig.CreateFrom(matchConfig, AppId);
            var matchInfo = internalConfig.ToMatchInfo(OlympusAppId);
            Assert.That(matchInfo, Is.Not.Null);
            Assert.That(matchInfo.OlympusAppId, Is.EqualTo(OlympusAppId));
            Assert.That(matchInfo.StarkAppId, Is.EqualTo(SimpleMatchConfigExtension.PublicMatchAppId));
            Assert.That(matchInfo.ApiName, Is.EqualTo(SimpleMatchConfigExtension.GetSimpleMatchPool(SimpleMatchPoolType.P1v1)));
            Assert.That(matchInfo.MatchTag, Is.EqualTo($"{simpleConfig.MatchTag}-{AppId}"));
        }
    }
}