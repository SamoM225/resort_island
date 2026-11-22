using System;
using System.IO;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using UnityEngine;
using ResortIsland.Backend;
using ResortIsland.Config;
using ResortIsland.Resort;
using ResortIsland.Save;

namespace ResortIsland.Tests
{
    public class SaveStoreTests
    {
        private const string BackedUpSave = "{\"data\":{\"save_version\":1,\"save\":{\"saveVersion\":1,\"economy\":{\"coins\":4000},\"resort\":{\"buildings\":[],\"unlocks\":[\"pool\"]}}}}";

        private string directory;
        private PlayerEconomyConfig config;
        private FakeBackendClient client;
        private int restored;

        private string SavePath => Path.Combine(directory, SaveSystem.FileName);

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "ResortIslandSaveStoreTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            config = ScriptableObject.CreateInstance<PlayerEconomyConfig>();
            client = new FakeBackendClient();
            restored = 0;
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(config);
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }

        private SaveStore CreateStore()
        {
            SaveBackup backup = new SaveBackup(client, new AnonymousPlayerAuth(client, new InMemoryTokenStore { Token = "7|token" }), new FakeClock());
            return new SaveStore(new SaveSystem(directory, SaveMigrations.CreateDefault()), backup, () => restored++);
        }

        private int SavedCoins() => JObject.Parse(File.ReadAllText(SavePath))["economy"].Value<int>("coins");

        [Test]
        public void EconomyAndMap_AreSavedTogether()
        {
            client.Responses.Enqueue(new BackendResponse(404, "{}"));
            SaveStore store = CreateStore();
            store.Load(config);

            store.Data.economy.coins = 1500;
            store.Data.resort.buildings.Add(new BuildingSave { buildingId = "reception", cellX = 1, cellY = 2 });
            store.Save();

            JObject saved = JObject.Parse(File.ReadAllText(SavePath));
            Assert.AreEqual(1500, saved["economy"].Value<int>("coins"));
            Assert.AreEqual("reception", saved["resort"]["buildings"][0].Value<string>("buildingId"));
        }

        [Test]
        public void Load_HappensOnce()
        {
            client.Responses.Enqueue(new BackendResponse(404, "{}"));
            SaveStore store = CreateStore();

            LoadResult first = store.Load(config);
            LoadResult second = store.Load(config);

            Assert.AreSame(first, second);
        }

        [Test]
        public void NewGame_WithBackup_IsReplacedByTheBackupWithoutUploadingTheNewGame()
        {
            client.Responses.Enqueue(new BackendResponse(200, BackedUpSave));
            SaveStore store = CreateStore();

            store.Load(config);
            store.Save();

            Assert.AreEqual(1, restored);
            Assert.AreEqual(4000, SavedCoins(), "The new game must not be written over the restored save.");
            CollectionAssert.DoesNotContain(client.Methods, "PUT");
        }

        [Test]
        public void NewGame_WithoutBackup_IsBackedUp()
        {
            client.Responses.Enqueue(new BackendResponse(404, "{}"));
            SaveStore store = CreateStore();

            store.Load(config);

            Assert.AreEqual(0, restored);
            Assert.IsFalse(store.Data.backupCheckPending);
            Assert.AreEqual(new[] { "GET", "PUT" }, client.Methods.ToArray());
        }

        [Test]
        public void NewGame_Offline_IsNotUploadedUntilTheBackendAnswers()
        {
            client.Responses.Enqueue(new BackendResponse(0, null));
            SaveStore store = CreateStore();

            store.Load(config);
            store.Save();
            store.FlushBackup();

            Assert.AreEqual(new[] { "GET" }, client.Methods.ToArray());
            Assert.IsTrue(JObject.Parse(File.ReadAllText(SavePath)).Value<bool>("backupCheckPending"), "The next launch asks again.");
        }

        [Test]
        public void ExistingSave_IsNotReplacedByTheBackup()
        {
            client.Responses.Enqueue(new BackendResponse(404, "{}"));
            SaveStore first = CreateStore();
            first.Load(config);
            first.Data.economy.coins = 1200;
            first.Save();
            client.Methods.Clear();
            client.Responses.Enqueue(new BackendResponse(200, BackedUpSave));

            SaveStore restarted = CreateStore();
            restarted.Load(config);

            Assert.AreEqual(1200, restarted.Data.economy.coins);
            CollectionAssert.DoesNotContain(client.Methods, "GET");
        }
    }
}
