using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using ResortIsland.Config;
using ResortIsland.Waves;

namespace ResortIsland.Tests
{
    public class ScriptedWavePlannerTests
    {
        private GameConfigs configs;

        [SetUp]
        public void SetUp()
        {
            configs = GameConfigs.Parse(File.ReadAllText(Path.Combine(Application.dataPath, "Tests/EditMode/Fixtures/configs_response.json")));
        }

        private ScriptedWavePlanner PlannerWith(params string[] activeNeeds) => new ScriptedWavePlanner(configs.Waves, activeNeeds);

        private static string Describe(WavePlan plan) => string.Join(", ", plan.Needs.Select(need => $"{need.NeedId} {need.Units}"));

        [Test]
        public void StartingNeeds_AreNeedsOfBuildingsWithoutUnlock()
        {
            CollectionAssert.AreEquivalent(new[] { "rooms", "food", "drinks" }, StartingNeeds.From(configs.Buildings));
        }

        [Test]
        public void Plan_ReturnsWaveTableDemand()
        {
            WavePlan plan = PlannerWith(StartingNeeds.From(configs.Buildings).ToArray()).Plan(1);

            Assert.AreEqual(1, plan.Wave);
            Assert.AreEqual("rooms 8, drinks 8, food 6", Describe(plan));
        }

        [Test]
        public void Plan_LeavesOutNeedsOfBuildingsNotBought()
        {
            WavePlan plan = PlannerWith("rooms", "food", "drinks").Plan(4);

            Assert.AreEqual("rooms 16, drinks 18, food 14", Describe(plan));
        }

        [Test]
        public void Plan_IncludesNeedOfBoughtBuilding()
        {
            WavePlan plan = PlannerWith("rooms", "food", "drinks", "relax").Plan(4);

            Assert.AreEqual("rooms 16, drinks 18, food 14, relax 8", Describe(plan));
        }

        [Test]
        public void Plan_WaveOutsideTable_IsNull()
        {
            Assert.IsNull(PlannerWith("rooms").Plan(9));
        }
    }
}
