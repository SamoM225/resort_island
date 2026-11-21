using System;
using System.Collections.Generic;
using NUnit.Framework;
using ResortIsland.Waves;

namespace ResortIsland.Tests
{
    public class WaveArrivalsPresenterTests
    {
        private GameStateMachine machine;
        private Dictionary<int, WavePlan> plans;
        private WaveArrivalsPresenter presenter;
        private List<string> arrived;

        [SetUp]
        public void SetUp()
        {
            machine = new GameStateMachine();
            plans = new Dictionary<int, WavePlan>
            {
                [1] = new WavePlan(1, 4, new[] { new NeedDemand("rooms", 2), new NeedDemand("drinks", 3) }),
                [2] = new WavePlan(2, 6, new[] { new NeedDemand("rooms", 4) })
            };
            presenter = new WaveArrivalsPresenter(machine, new FakePlanner(plans), 90.0);
            arrived = new List<string>();
            presenter.UnitArrived += arrived.Add;
        }

        [TearDown]
        public void TearDown()
        {
            presenter.Dispose();
        }

        [Test]
        public void BeforeTheWave_NobodyArrives()
        {
            presenter.Tick(90.0);
            machine.TryStartWave();
            presenter.Tick(90.0);

            Assert.IsNull(presenter.Current);
            CollectionAssert.IsEmpty(arrived);
        }

        [Test]
        public void ActiveWave_BringsTheUnitsOfItsPlan()
        {
            StartWave();

            presenter.Tick(90.0);

            Assert.AreEqual(5, arrived.Count);
            Assert.IsTrue(presenter.Current.AllArrived);
        }

        [Test]
        public void Arrivals_StopWhenTheWaveIsNoLongerActive()
        {
            StartWave();
            presenter.Tick(30.0);
            int before = arrived.Count;

            machine.TryTransitionTo(GameState.WaveResolving);
            presenter.Tick(60.0);

            Assert.AreEqual(before, arrived.Count);
        }

        [Test]
        public void NextWave_ArrivesByItsOwnPlan()
        {
            StartWave();
            presenter.Tick(90.0);
            machine.TryTransitionTo(GameState.WaveResolving);
            machine.TryTransitionTo(GameState.WaveComplete);
            machine.TryTransitionTo(GameState.Upgrade);
            machine.TryTransitionTo(GameState.AwaitingWave);
            arrived.Clear();

            StartWave();
            presenter.Tick(90.0);

            CollectionAssert.AreEqual(new[] { "rooms", "rooms", "rooms", "rooms" }, arrived);
        }

        [Test]
        public void WaveWithoutPlan_BringsNobody()
        {
            plans.Clear();

            StartWave();
            presenter.Tick(90.0);

            Assert.AreEqual(0, presenter.Current.Total);
            Assert.IsTrue(presenter.Current.TimerEnded);
            CollectionAssert.IsEmpty(arrived);
        }

        [Test]
        public void Dispose_StopsTheArrivals()
        {
            StartWave();

            presenter.Dispose();
            presenter.Tick(90.0);

            CollectionAssert.IsEmpty(arrived);
        }

        [Test]
        public void WaveTimer_MustBeAboveZero()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new WaveArrivalsPresenter(machine, new FakePlanner(plans), 0));
        }

        private void StartWave()
        {
            machine.TryStartWave();
            machine.TryTransitionTo(GameState.WaveActive);
        }

        private class FakePlanner : IWavePlanner
        {
            private readonly Dictionary<int, WavePlan> plans;

            public FakePlanner(Dictionary<int, WavePlan> plans) => this.plans = plans;

            public WavePlan Plan(int wave) => plans.TryGetValue(wave, out WavePlan plan) ? plan : null;
        }
    }
}
