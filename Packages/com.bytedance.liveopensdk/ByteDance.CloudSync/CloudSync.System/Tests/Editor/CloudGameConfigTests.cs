using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace ByteDance.CloudSync.Tests
{
    class TestCloudGameConfigLoader : CloudGameConfigLoader
    {
        public string TestPath;
        protected override string ConfigPath => TestPath;
    }

    [TestFixture]
    sealed class CloudGameConfigTests
    {
        private string TestPath => $"{Application.dataPath}/../tests.cloud_game_config.json";

        [OneTimeSetUp]
        public void Setup()
        {
            Debug.Log("Setup");
        }

        [Test]
        public void TestLoad0()
        {
            File.Delete(TestPath);

            var loader = new TestCloudGameConfigLoader()
            {
                TestPath = TestPath
            };
            var data = loader.Load();
            Assert.IsNull(data);
        }

        [Test]
        public void TestLoad1()
        {
            File.Delete(TestPath);
            string content = "{\n  \"appId\": \"\"\n}";
            File.WriteAllText(TestPath, content);

            var loader = new TestCloudGameConfigLoader()
            {
                TestPath = TestPath
            };
            var data = loader.Load();
            Assert.IsNotNull(data);
            Assert.IsTrue(string.IsNullOrEmpty(data.AppId));
            Assert.IsEmpty(data.AppId);
        }

        [Test]
        public void TestLoad123()
        {
            File.Delete(TestPath);
            string content = "{\n  \"appId\": \"123\"\n}";
            File.WriteAllText(TestPath, content);

            var loader = new TestCloudGameConfigLoader()
            {
                TestPath = TestPath
            };
            var data = loader.Load();
            Assert.IsNotNull(data);
            Assert.AreEqual("123", data.AppId);
        }

        [Test]
        public void TestLoadEnv()
        {
            File.Delete(TestPath);
            string content = "{\n  \"appId\": \"123\"\n}";
            File.WriteAllText(TestPath, content);

            Debug.Log("TestCloudGameConfigLoader");
            var loader = new TestCloudGameConfigLoader()
            {
                TestPath = TestPath
            };
            var env = loader.LoadAsEnv();
            Assert.IsNotNull(env);
            var hasAppId = env.TryGetStringValue(SdkConsts.GameArgAppId, out var appId);
            Assert.IsTrue(hasAppId);
            Assert.AreEqual("123", appId);
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            Debug.Log("Cleanup");
            File.Delete(TestPath);
        }
    }
}