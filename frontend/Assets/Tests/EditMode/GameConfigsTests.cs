using System.IO;
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using ResortIsland.Config;

namespace ResortIsland.Tests
{
    public class GameConfigsTests
    {
        private static string ResponseFixture => File.ReadAllText(Path.Combine(Application.dataPath, "Tests/EditMode/Fixtures/configs_response.json"));

        private static string ResponseWithout(string key)
        {
            JObject response = JObject.Parse(ResponseFixture);
            ((JArray)response["data"]).Where(item => item.Value<string>("key") == key).ToList().ForEach(item => item.Remove());
            return response.ToString();
        }

        [Test]
        public void Parse_ReadsEveryConfig()
        {
            GameConfigs configs = GameConfigs.Parse(ResponseFixture);

            Assert.AreEqual(1000, configs.Economy.StartingCoins);
            Assert.AreEqual(6, configs.Economy.CoinsRatingMult.Length);
            Assert.AreEqual(12f, configs.WaveSystem.ServiceTimeS);
            Assert.AreEqual(0.33f, configs.WaveSystem.UtilizationEmpty);
            Assert.AreEqual(2, configs.WaveSystem.Rush.Wave);
            Assert.AreEqual(3, configs.Energy.BoostCost);
            Assert.AreEqual(1.5f, configs.Needs.Needs.Single(need => need.Id == "ocean_view").SatisfactionWeight);
            CollectionAssert.AreEqual(new[] { 3, 5, 7 }, configs.Buildings.Levels.Select(level => level.Capacity));
            Assert.AreEqual("drinks", configs.Buildings.Buildings.Single(building => building.Id == "beachbar").Need);
            Assert.IsNull(configs.Buildings.Buildings.Single(building => building.Id == "reception").Unlock);
            Assert.AreEqual(3, configs.Unlocks.Unlocks.Single(unlock => unlock.Id == "pool").AfterWave);
            Assert.AreEqual(8, configs.Waves.Waves.Count);
            Assert.AreEqual(8, configs.Waves.Waves[0].Demand["rooms"]);
        }

        [Test]
        public void Parse_MissingConfig_Throws()
        {
            Assert.Catch<JsonException>(() => GameConfigs.Parse(ResponseWithout("waves")));
        }

        [Test]
        public void Parse_ResponseWithoutData_Throws()
        {
            Assert.Catch<JsonException>(() => GameConfigs.Parse("{ \"message\": \"Server Error\" }"));
        }

        [Test]
        public void Parse_InvalidJson_Throws()
        {
            Assert.Catch<JsonException>(() => GameConfigs.Parse("<html>Bad Gateway</html>"));
        }
    }
}
