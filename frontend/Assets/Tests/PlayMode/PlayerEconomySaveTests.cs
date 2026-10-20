using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.TestTools;
using ResortIsland.Config;
using ResortIsland.Economy;
using ResortIsland.Save;

namespace ResortIsland.Tests
{
    /// <summary>
    /// Runs PlayerEconomy against the real persistentDataPath. Any existing save is moved
    /// aside before each test and put back afterwards.
    /// </summary>
    public class PlayerEconomySaveTests
    {
        private string backupDirectory;
        private PlayerEconomyConfig config;

        private static string SavePath => Path.Combine(Application.persistentDataPath, SaveSystem.FileName);

        [SetUp]
        public void SetUp()
        {
            backupDirectory = Path.Combine(Path.GetTempPath(), "ResortIslandSaveBackup", System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(backupDirectory);
            Directory.CreateDirectory(Application.persistentDataPath);
            foreach (string file in Directory.GetFiles(Application.persistentDataPath, "save.*"))
                File.Move(file, Path.Combine(backupDirectory, Path.GetFileName(file)));

            config = ScriptableObject.CreateInstance<PlayerEconomyConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (PlayerEconomy economy in Object.FindObjectsByType<PlayerEconomy>(FindObjectsSortMode.None))
                Object.DestroyImmediate(economy.gameObject);
            Object.DestroyImmediate(config);

            foreach (string file in Directory.GetFiles(Application.persistentDataPath, "save.*"))
                File.Delete(file);
            foreach (string file in Directory.GetFiles(backupDirectory))
                File.Move(file, Path.Combine(Application.persistentDataPath, Path.GetFileName(file)));
            Directory.Delete(backupDirectory, true);
        }

        private PlayerEconomy StartGame()
        {
            GameObject go = new GameObject("Player Economy (test)");
            go.SetActive(false);
            PlayerEconomy economy = go.AddComponent<PlayerEconomy>();
            typeof(PlayerEconomy).GetField("config", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(economy, config);
            go.SetActive(true);
            return economy;
        }

        private static int SavedCoins() => JObject.Parse(File.ReadAllText(SavePath))["economy"]["coins"].Value<int>();

        [UnityTest]
        public IEnumerator NewPlayer_IsSavedImmediately()
        {
            PlayerEconomy economy = StartGame();
            yield return null;

            Assert.AreEqual(config.StartingCoins, economy.Coins);
            Assert.IsTrue(File.Exists(SavePath));
            Assert.AreEqual(config.StartingCoins, SavedCoins());
        }

        [UnityTest]
        public IEnumerator Changes_AreAutosaved_AndSurviveRestart()
        {
            PlayerEconomy economy = StartGame();
            economy.AddCoins(250);
            economy.AddGems(5);
            economy.TrySpendEnergy(10);
            economy.SetReputation(1);

            yield return new WaitForSecondsRealtime(1.5f);
            Assert.AreEqual(config.StartingCoins + 250, SavedCoins(), "Autosave should have written the change.");

            Object.Destroy(economy.gameObject);
            yield return null;

            PlayerEconomy restarted = StartGame();
            yield return null;

            Assert.AreEqual(config.StartingCoins + 250, restarted.Coins);
            Assert.AreEqual(config.StartingGems + 5, restarted.Gems);
            Assert.AreEqual(config.StartingEnergy - 10, restarted.Energy);
            Assert.AreEqual(1, restarted.Reputation);
        }

        [UnityTest]
        public IEnumerator AppPause_SavesWithoutWaitingForAutosave()
        {
            PlayerEconomy economy = StartGame();
            yield return null;

            economy.AddCoins(1);
            economy.SendMessage("OnApplicationPause", true);

            Assert.AreEqual(config.StartingCoins + 1, SavedCoins());
        }

        [UnityTest]
        public IEnumerator SaveFromNewerGame_IsNeverOverwritten()
        {
            const string newer = "{ \"saveVersion\": 999, \"economy\": { \"coins\": 7 } }";
            File.WriteAllText(SavePath, newer);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("newer game version"));

            PlayerEconomy economy = StartGame();
            economy.AddCoins(100);
            economy.SendMessage("OnApplicationPause", true);
            yield return new WaitForSecondsRealtime(1.5f);

            Assert.AreEqual(newer, File.ReadAllText(SavePath));
        }
    }
}
