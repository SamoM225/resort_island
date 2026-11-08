using System;
using System.IO;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using ResortIsland.Config;
using ResortIsland.Save;

namespace ResortIsland.Tests
{
    public class SaveSystemTests
    {
        private string directory;
        private PlayerEconomyConfig config;

        private static string FixturesDirectory => Path.Combine(Application.dataPath, "Tests/EditMode/Fixtures");
        private string SavePath => Path.Combine(directory, SaveSystem.FileName);
        private string BackupPath => SavePath + ".bak";

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "ResortIslandSaveTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            config = ScriptableObject.CreateInstance<PlayerEconomyConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(config);
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }

        private SaveSystem CreateSystem() => new SaveSystem(directory, SaveMigrations.CreateDefault());

        private void SetConfigVersion(int version)
        {
            SerializedObject serialized = new SerializedObject(config);
            serialized.FindProperty("configVersion").intValue = version;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SaveData CreateData(int coins)
        {
            return new SaveData { economy = new EconomySaveData { coins = coins, gems = 2, energy = 30, resortValue = 500, reputation = 1 } };
        }

        [Test]
        public void Load_WithoutSave_StartsNewGameWithConfigDefaults()
        {
            LoadResult result = CreateSystem().Load(config);

            Assert.AreEqual(LoadStatus.NewGame, result.Status);
            Assert.AreEqual(config.StartingCoins, result.Data.economy.coins);
            Assert.AreEqual(config.StartingGems, result.Data.economy.gems);
            Assert.AreEqual(config.StartingEnergy, result.Data.economy.energy);
            Assert.AreEqual(config.StartingResortValue, result.Data.economy.resortValue);
            Assert.AreEqual(config.StartingReputation, result.Data.economy.reputation);
        }

        [Test]
        public void SaveThenLoad_KeepsAllValues()
        {
            SaveSystem system = CreateSystem();
            system.Save(new SaveData { economy = new EconomySaveData { coins = 1234, gems = 56, energy = 78, resortValue = 9000, reputation = 2 } });

            LoadResult result = system.Load(config);

            Assert.AreEqual(LoadStatus.Loaded, result.Status);
            Assert.AreEqual(1234, result.Data.economy.coins);
            Assert.AreEqual(56, result.Data.economy.gems);
            Assert.AreEqual(78, result.Data.economy.energy);
            Assert.AreEqual(9000, result.Data.economy.resortValue);
            Assert.AreEqual(2, result.Data.economy.reputation);
        }

        [Test]
        public void Save_WritesSaveAndConfigVersion()
        {
            SetConfigVersion(3);
            SaveData data = SaveData.CreateNew(config);

            CreateSystem().Save(data);

            JObject json = JObject.Parse(File.ReadAllText(SavePath));
            Assert.AreEqual(SaveData.CurrentVersion, json["saveVersion"].Value<int>());
            Assert.AreEqual(3, json["configVersion"].Value<int>());
            Assert.IsFalse(File.Exists(SavePath + ".tmp"), "Temp file must be gone after saving.");
        }

        [Test]
        public void Save_KeepsPreviousSaveAsBackup()
        {
            SaveSystem system = CreateSystem();
            system.Save(CreateData(111));
            system.Save(CreateData(222));

            Assert.AreEqual(222, JObject.Parse(File.ReadAllText(SavePath))["economy"]["coins"].Value<int>());
            Assert.AreEqual(111, JObject.Parse(File.ReadAllText(BackupPath))["economy"]["coins"].Value<int>());
        }

        [Test]
        public void Load_V1Fixture_LoadsAllValues()
        {
            File.Copy(Path.Combine(FixturesDirectory, "save_v1.json"), SavePath);

            LoadResult result = CreateSystem().Load(config);

            Assert.That(result.Status, Is.EqualTo(LoadStatus.Loaded).Or.EqualTo(LoadStatus.Migrated));
            Assert.AreEqual(1, result.OriginalVersion);
            Assert.AreEqual(4321, result.Data.economy.coins);
            Assert.AreEqual(37, result.Data.economy.gems);
            Assert.AreEqual(55, result.Data.economy.energy);
            Assert.AreEqual(31000, result.Data.economy.resortValue);
            Assert.AreEqual(3, result.Data.economy.reputation);
        }

        [Test]
        public void EveryShippedSaveVersion_HasFixtureThatLoads()
        {
            for (int version = 1; version <= SaveData.CurrentVersion; version++)
            {
                string fixture = Path.Combine(FixturesDirectory, $"save_v{version}.json");
                Assert.IsTrue(File.Exists(fixture), $"Add Tests/EditMode/Fixtures/save_v{version}.json when bumping SaveData.CurrentVersion.");

                File.Copy(fixture, SavePath, true);
                LoadResult result = CreateSystem().Load(config);

                Assert.That(result.Status, Is.EqualTo(LoadStatus.Loaded).Or.EqualTo(LoadStatus.Migrated), $"save_v{version}.json: {result.Message}");
                Assert.AreEqual(SaveData.CurrentVersion, result.Data.saveVersion);
            }
        }

        [Test]
        public void Load_SaveWithMissingFields_FillsThemFromConfig()
        {
            File.WriteAllText(SavePath, "{ \"saveVersion\": 1, \"economy\": { \"coins\": 5 } }");

            LoadResult result = CreateSystem().Load(config);

            Assert.AreEqual(LoadStatus.Loaded, result.Status);
            Assert.AreEqual(5, result.Data.economy.coins);
            Assert.AreEqual(config.StartingGems, result.Data.economy.gems);
            Assert.AreEqual(config.StartingEnergy, result.Data.economy.energy);
            Assert.AreEqual(config.StartingResortValue, result.Data.economy.resortValue);
            Assert.AreEqual(config.StartingReputation, result.Data.economy.reputation);
        }

        [Test]
        public void Load_SaveFromOlderConfig_UpdatesConfigVersion()
        {
            File.Copy(Path.Combine(FixturesDirectory, "save_v1.json"), SavePath);
            SetConfigVersion(2);

            LoadResult result = CreateSystem().Load(config);

            Assert.AreEqual(2, result.Data.configVersion);
            StringAssert.Contains("Config v1 -> v2", result.Message);
            Assert.AreEqual(4321, result.Data.economy.coins, "Progress must survive a config update.");
        }

        [Test]
        public void Load_OlderVersion_RunsEveryMigrationAndBacksUpOriginal()
        {
            const string original = "{ \"saveVersion\": 1, \"money\": 500 }";
            File.WriteAllText(SavePath, original);
            SaveSystem system = new SaveSystem(directory, new SaveMigrator(3, new ISaveMigration[] { new AddGemsField(), new MoneyToEconomyCoins() }));

            LoadResult result = system.Load(config);

            Assert.AreEqual(LoadStatus.Migrated, result.Status);
            Assert.AreEqual(1, result.OriginalVersion);
            Assert.AreEqual(3, result.Data.saveVersion);
            Assert.AreEqual(500, result.Data.economy.coins);
            Assert.AreEqual(99, result.Data.economy.gems);
            Assert.AreEqual(config.StartingEnergy, result.Data.economy.energy);
            Assert.AreEqual(original, File.ReadAllText(Path.Combine(directory, "save.v1.bak")));

            system.Save(result.Data);
            Assert.AreEqual(3, JObject.Parse(File.ReadAllText(SavePath))["saveVersion"].Value<int>());
        }

        [Test]
        public void Load_MiddleVersion_RunsOnlyRemainingMigrations()
        {
            File.WriteAllText(SavePath, "{ \"saveVersion\": 2, \"economy\": { \"coins\": 7 } }");
            SaveSystem system = new SaveSystem(directory, new SaveMigrator(3, new ISaveMigration[] { new MoneyToEconomyCoins(), new AddGemsField() }));

            LoadResult result = system.Load(config);

            Assert.AreEqual(LoadStatus.Migrated, result.Status);
            Assert.AreEqual(7, result.Data.economy.coins);
            Assert.AreEqual(99, result.Data.economy.gems);
        }

        [Test]
        public void Load_NewerVersion_IsNotOverwritten()
        {
            const string newer = "{ \"saveVersion\": 999, \"economy\": { \"coins\": 1 } }";
            File.WriteAllText(SavePath, newer);

            LoadResult result = CreateSystem().Load(config);

            Assert.AreEqual(LoadStatus.NewerVersion, result.Status);
            Assert.IsFalse(result.CanSave);
            Assert.AreEqual(newer, File.ReadAllText(SavePath));
        }

        [Test]
        public void Load_CorruptedSave_RestoresPreviousSave()
        {
            SaveSystem system = CreateSystem();
            system.Save(CreateData(111));
            system.Save(CreateData(222));
            File.WriteAllText(SavePath, "{ broken");

            LoadResult result = system.Load(config);

            Assert.AreEqual(LoadStatus.RestoredFromBackup, result.Status);
            Assert.AreEqual(111, result.Data.economy.coins);
            Assert.AreEqual(1, Directory.GetFiles(directory, "save.corrupt-*.json").Length, "Broken file must be kept aside.");

            system.Save(result.Data);
            Assert.AreEqual(111, JObject.Parse(File.ReadAllText(BackupPath))["economy"]["coins"].Value<int>(), "Good backup must not be replaced by the broken file.");
        }

        [Test]
        public void Load_CorruptedSaveWithoutBackup_StartsNewGameAndKeepsBrokenFile()
        {
            File.WriteAllText(SavePath, "not json at all");

            LoadResult result = CreateSystem().Load(config);

            Assert.AreEqual(LoadStatus.Corrupted, result.Status);
            Assert.IsTrue(result.CanSave);
            Assert.AreEqual(config.StartingCoins, result.Data.economy.coins);
            Assert.IsFalse(File.Exists(SavePath));
            Assert.AreEqual(1, Directory.GetFiles(directory, "save.corrupt-*.json").Length);
        }

        [Test]
        public void Load_SaveWithoutVersion_IsTreatedAsCorrupted()
        {
            File.WriteAllText(SavePath, "{ \"economy\": { \"coins\": 5 } }");

            Assert.AreEqual(LoadStatus.Corrupted, CreateSystem().Load(config).Status);
        }

        [Test]
        public void Load_OutOfRangeValues_AreClamped()
        {
            File.WriteAllText(SavePath, "{ \"saveVersion\": 1, \"economy\": { \"coins\": -5, \"energy\": 500, \"reputation\": 9 } }");

            LoadResult result = CreateSystem().Load(config);

            Assert.AreEqual(0, result.Data.economy.coins);
            Assert.AreEqual(config.MaxEnergy, result.Data.economy.energy);
            Assert.AreEqual(5, result.Data.economy.reputation);
        }

        [Test]
        public void DefaultMigrations_CoverEveryVersion()
        {
            Assert.DoesNotThrow(() => SaveMigrations.CreateDefault(), "Every bump of SaveData.CurrentVersion needs a migration in SaveMigrations.");
        }

        [Test]
        public void Migrator_MissingStep_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => new SaveMigrator(3, new ISaveMigration[] { new MoneyToEconomyCoins() }));
        }

        [Test]
        public void Migrator_DuplicateStep_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => new SaveMigrator(2, new ISaveMigration[] { new MoneyToEconomyCoins(), new MoneyToEconomyCoins() }));
        }

        private class MoneyToEconomyCoins : ISaveMigration
        {
            public int FromVersion => 1;

            public void Apply(JObject save)
            {
                save["economy"] = new JObject { ["coins"] = save["money"] };
                save.Remove("money");
            }
        }

        private class AddGemsField : ISaveMigration
        {
            public int FromVersion => 2;

            public void Apply(JObject save) => ((JObject)save["economy"])["gems"] = 99;
        }
    }
}
