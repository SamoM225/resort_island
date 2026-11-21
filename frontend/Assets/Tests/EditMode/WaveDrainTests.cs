using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using ResortIsland.Config;
using ResortIsland.Waves;

namespace ResortIsland.Tests
{
    public class WaveDrainTests
    {
        private GameStateMachine machine;
        private WaveArrivalsPresenter arrivals;
        private List<IBuildingQueue> queues;
        private WaveDrain drain;
        private BuildingService bar;

        [SetUp]
        public void SetUp()
        {
            GameConfigs configs = GameConfigs.Parse(File.ReadAllText(Path.Combine(Application.dataPath, "Tests/EditMode/Fixtures/configs_response.json")));
            machine = new GameStateMachine();
            arrivals = new WaveArrivalsPresenter(machine, new OnePlan(new WavePlan(1, 8, new[] { new NeedDemand("drinks", 8) })), configs.WaveSystem.WaveTimerS);
            bar = new BuildingService("drinks", BuildingSeats.For(configs.Buildings, 1), ServiceRules.From(configs.WaveSystem)) { Staffed = true };
            queues = new List<IBuildingQueue> { bar };
            drain = new WaveDrain(machine, machine, arrivals, () => queues);
        }

        [TearDown]
        public void TearDown()
        {
            arrivals.Dispose();
        }

        [Test]
        public void WaitingForTheWave_NothingHappens()
        {
            drain.Check();

            Assert.AreEqual(GameState.AwaitingWave, machine.Current);
        }

        [Test]
        public void Wave_StaysActiveUntilTheTimerEnds()
        {
            StartWave();

            arrivals.Tick(89.9);
            drain.Check();

            Assert.AreEqual(GameState.WaveActive, machine.Current);
        }

        [Test]
        public void AfterTheTimer_GuestsStillInQueuesAreDrained()
        {
            StartWave();
            bar.Arrive();
            bar.Arrive();
            bar.Arrive();
            bar.Arrive();

            arrivals.Tick(90.0);
            drain.Check();

            Assert.AreEqual(GameState.WaveResolving, machine.Current);
            Assert.IsFalse(bar.IsClear);
        }

        [Test]
        public void Wave_EndsWhenEveryQueueIsClear()
        {
            StartWave();
            bar.Arrive();
            bar.Arrive();
            bar.Arrive();
            bar.Arrive();
            arrivals.Tick(90.0);
            drain.Check();

            bar.Tick(12.0);
            drain.Check();
            Assert.AreEqual(GameState.WaveResolving, machine.Current);

            bar.Tick(12.0);
            drain.Check();
            Assert.AreEqual(GameState.WaveComplete, machine.Current);
        }

        [Test]
        public void OneBusyBuilding_KeepsTheWaveDraining()
        {
            BuildingService pool = new BuildingService("relax", 3, new ServiceRules(12, 1, 0.33));
            queues.Add(pool);
            StartWave();
            pool.Arrive();

            arrivals.Tick(90.0);
            drain.Check();

            Assert.AreEqual(GameState.WaveResolving, machine.Current);
        }

        [Test]
        public void ClearQueues_EndTheWaveRightAfterTheTimer()
        {
            StartWave();

            arrivals.Tick(90.0);
            drain.Check();

            Assert.AreEqual(GameState.WaveComplete, machine.Current);
        }

        private void StartWave()
        {
            machine.TryStartWave();
            machine.TryTransitionTo(GameState.WaveActive);
        }

        private class OnePlan : IWavePlanner
        {
            private readonly WavePlan plan;

            public OnePlan(WavePlan plan) => this.plan = plan;

            public WavePlan Plan(int wave) => plan;
        }
    }
}
