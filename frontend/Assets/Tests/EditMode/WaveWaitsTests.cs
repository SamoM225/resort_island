using System.IO;
using NUnit.Framework;
using UnityEngine;
using ResortIsland.Config;
using ResortIsland.Waves;

namespace ResortIsland.Tests
{
    public class WaveWaitsTests
    {
        private GameStateMachine machine;
        private WaveBuildings buildings;
        private WaveWaits waits;

        [SetUp]
        public void SetUp()
        {
            GameConfigs configs = GameConfigs.Parse(File.ReadAllText(Path.Combine(Application.dataPath, "Tests/EditMode/Fixtures/configs_response.json")));
            machine = new GameStateMachine();
            buildings = new WaveBuildings(new[] { new BuildingService("drinks", 1, new ServiceRules(12, 1, 0.33)) { Staffed = true } });
            waits = new WaveWaits(machine, WaveRating.From(configs), buildings);
        }

        [TearDown]
        public void TearDown()
        {
            waits.Dispose();
        }

        [Test]
        public void BeforeTheFirstWave_ThereIsNoScore()
        {
            Assert.IsNull(waits.Current);
        }

        [Test]
        public void ActiveWave_StartsItsScore()
        {
            StartWave();

            Assert.AreEqual(1, waits.Current.Wave);
            Assert.AreEqual(0, waits.Current.Units);
        }

        [Test]
        public void SeatedUnits_AreRecordedWithTheirWait()
        {
            StartWave();

            buildings.Arrive("drinks");
            buildings.Arrive("drinks");
            buildings.Tick(12.0);

            Assert.AreEqual(2, waits.Current.Units);
            Assert.AreEqual(0.0, waits.Current.Waits["drinks"][0]);
            Assert.AreEqual(12.0, waits.Current.Waits["drinks"][1], 1e-6);
        }

        [Test]
        public void UnitsSeatedWhileTheWaveDrains_AreRecorded()
        {
            StartWave();
            buildings.Arrive("drinks");
            buildings.Arrive("drinks");

            machine.TryTransitionTo(GameState.WaveResolving);
            buildings.Tick(12.0);

            Assert.AreEqual(2, waits.Current.Units);
        }

        [Test]
        public void UnitsAfterTheWave_AreNotRecorded()
        {
            StartWave();
            machine.TryTransitionTo(GameState.WaveResolving);
            machine.TryTransitionTo(GameState.WaveComplete);

            buildings.Arrive("drinks");

            Assert.AreEqual(0, waits.Current.Units);
        }

        [Test]
        public void NextWave_StartsANewScore()
        {
            StartWave();
            buildings.Arrive("drinks");
            machine.TryTransitionTo(GameState.WaveResolving);
            machine.TryTransitionTo(GameState.WaveComplete);
            machine.TryTransitionTo(GameState.Upgrade);
            machine.TryTransitionTo(GameState.AwaitingWave);

            StartWave();

            Assert.AreEqual(2, waits.Current.Wave);
            Assert.AreEqual(0, waits.Current.Units);
        }

        [Test]
        public void Dispose_StopsRecording()
        {
            StartWave();

            waits.Dispose();
            buildings.Arrive("drinks");

            Assert.AreEqual(0, waits.Current.Units);
        }

        private void StartWave()
        {
            machine.TryStartWave();
            machine.TryTransitionTo(GameState.WaveActive);
        }
    }
}
