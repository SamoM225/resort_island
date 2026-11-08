using System;
using System.IO;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using UnityEngine;
using ResortIsland.Config;

namespace ResortIsland.Tests
{
    public class GameConfigProviderTests
    {
        private static readonly DateTime Now = new DateTime(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

        private string directory;
        private GameConfigCache cache;
        private string bundled;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "ResortIslandConfigTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            cache = new GameConfigCache(directory);
            bundled = ResponseWithStartingCoins(1);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }

        private GameConfigProvider StartGame() => new GameConfigProvider(cache, bundled);

        private static string ResponseWithStartingCoins(int coins)
        {
            JObject response = JObject.Parse(File.ReadAllText(Path.Combine(Application.dataPath, "Tests/EditMode/Fixtures/configs_response.json")));
            foreach (JToken item in (JArray)response["data"])
                if (item.Value<string>("key") == "economy")
                    item["data"]["starting_coins"] = coins;
            return response.ToString();
        }

        [Test]
        public void WithoutCache_UsesBundledCopyAndAsksBackend()
        {
            GameConfigProvider provider = StartGame();

            Assert.AreEqual(GameConfigSource.Bundled, provider.Source);
            Assert.AreEqual(1, provider.Current.Economy.StartingCoins);
            Assert.IsNull(provider.ETag);
            Assert.IsTrue(provider.NeedsRefresh(Now));
        }

        [Test]
        public void Downloaded_IsUsedAndCachedForNextStart()
        {
            GameConfigProvider provider = StartGame();

            Assert.IsTrue(provider.ApplyDownloaded(ResponseWithStartingCoins(2), "\"etag-1\"", "max-age=60, public", Now));
            Assert.AreEqual(GameConfigSource.Backend, provider.Source);
            Assert.AreEqual(2, provider.Current.Economy.StartingCoins);

            GameConfigProvider restarted = StartGame();
            Assert.AreEqual(GameConfigSource.Cache, restarted.Source);
            Assert.AreEqual(2, restarted.Current.Economy.StartingCoins);
            Assert.AreEqual("\"etag-1\"", restarted.ETag);
        }

        [Test]
        public void Cache_IsCurrentUntilMaxAgeRunsOut()
        {
            StartGame().ApplyDownloaded(ResponseWithStartingCoins(2), "\"etag-1\"", "max-age=60, public", Now);

            GameConfigProvider restarted = StartGame();
            Assert.IsFalse(restarted.NeedsRefresh(Now.AddSeconds(59)));
            Assert.IsTrue(restarted.NeedsRefresh(Now.AddSeconds(60)));
        }

        [Test]
        public void ExpiredCache_IsStillUsedWhileBackendIsDown()
        {
            StartGame().ApplyDownloaded(ResponseWithStartingCoins(2), "\"etag-1\"", "max-age=60, public", Now);

            GameConfigProvider restarted = StartGame();

            Assert.IsTrue(restarted.NeedsRefresh(Now.AddHours(1)));
            Assert.AreEqual(GameConfigSource.Cache, restarted.Source);
            Assert.AreEqual(2, restarted.Current.Economy.StartingCoins);
        }

        [Test]
        public void NotModified_RenewsExpiryAndKeepsConfigs()
        {
            StartGame().ApplyDownloaded(ResponseWithStartingCoins(2), "\"etag-1\"", "max-age=60, public", Now);
            GameConfigProvider restarted = StartGame();

            restarted.ApplyNotModified("max-age=60, public", Now.AddMinutes(10));

            Assert.IsFalse(restarted.NeedsRefresh(Now.AddMinutes(10).AddSeconds(59)));
            Assert.AreEqual(2, restarted.Current.Economy.StartingCoins);
            Assert.IsFalse(StartGame().NeedsRefresh(Now.AddMinutes(10).AddSeconds(59)), "Renewed expiry must be saved.");
        }

        [Test]
        public void InvalidDownload_KeepsCurrentConfigsAndCache()
        {
            GameConfigProvider provider = StartGame();
            provider.ApplyDownloaded(ResponseWithStartingCoins(2), "\"etag-1\"", "max-age=60", Now);

            Assert.IsFalse(provider.ApplyDownloaded("<html>Bad Gateway</html>", "\"etag-2\"", "max-age=60", Now));

            Assert.AreEqual(2, provider.Current.Economy.StartingCoins);
            Assert.AreEqual("\"etag-1\"", StartGame().ETag);
        }

        [Test]
        public void CorruptedCache_FallsBackToBundledCopy()
        {
            File.WriteAllText(cache.CachePath, "{ broken");

            GameConfigProvider provider = StartGame();

            Assert.AreEqual(GameConfigSource.Bundled, provider.Source);
            Assert.AreEqual(1, provider.Current.Economy.StartingCoins);
            Assert.IsTrue(provider.NeedsRefresh(Now));
        }

        [TestCase("max-age=60, public", 60)]
        [TestCase("public, max-age=3600", 3600)]
        [TestCase("no-cache", 0)]
        [TestCase(null, 0)]
        public void ParseMaxAge_ReadsSecondsFromCacheControl(string cacheControl, int expected)
        {
            Assert.AreEqual(expected, GameConfigProvider.ParseMaxAge(cacheControl));
        }

        [Test]
        public void BundledCopy_IsLoadableFromResources()
        {
            TextAsset copy = Resources.Load<TextAsset>(GameConfigProvider.BundledResourcePath);

            Assert.IsNotNull(copy, "Missing Assets/Resources/GameConfigs.json.");
            Assert.DoesNotThrow(() => GameConfigs.Parse(copy.text));
        }
    }
}
