using System;
using System.Collections.Generic;
using NUnit.Framework;
using ResortIsland.Waves;

namespace ResortIsland.Tests
{
    public class WaveTickerTests
    {
        private GameStateMachine machine;
        private WaveArrivalsPresenter arrivals;
        private WaveBuildings buildings;
        private WaveTicker ticker;
        private List<double> waits;

        [SetUp]
        public void SetUp()
        {
            machine = new GameStateMachine();
            arrivals = new WaveArrivalsPresenter(machine, new OnePlan(new WavePlan(1, 8, new[] { new NeedDemand("drinks", 8) })), 90.0);
            buildings = new WaveBuildings(new[] { new BuildingService("drinks", 1, new ServiceRules(12, 1, 0.33)) { Staffed = true } });
            arrivals.UnitArrived += buildings.Arrive;
            ticker = new WaveTicker(arrivals, buildings);
            waits = new List<double>();
            buildings.UnitSeated += (needId, wait) => waits.Add(wait);
        }

        [TearDown]
        public void TearDown()
        {
            arrivals.Dispose();
        }

        [Test]
        public void Waits_FollowTheQueue()
        {
            StartWave();

            ticker.Tick(200.0);

            Assert.AreEqual(8, waits.Count);
            for (int unit = 0; unit < 8; unit++)
                Assert.AreEqual(0.75 * unit, waits[unit], 1e-6);
        }

        [Test]
        public void Waits_DoNotDependOnFrameLength()
        {
            StartWave();

            for (int frame = 0; frame < 12000; frame++)
                ticker.Tick(1.0 / 60.0);

            Assert.AreEqual(8, waits.Count);
            for (int unit = 0; unit < 8; unit++)
                Assert.AreEqual(0.75 * unit, waits[unit], 1e-6);
        }

        [Test]
        public void BeforeTheWave_NobodyArrives()
        {
            ticker.Tick(200.0);

            CollectionAssert.IsEmpty(waits);
        }

        [Test]
        public void Tick_CannotGoBackInTime()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ticker.Tick(-1.0));
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
