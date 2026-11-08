using System;
using System.Collections.Generic;
using NUnit.Framework;
using ResortIsland.Waves;

namespace ResortIsland.Tests
{
    public class GuestArrivalPresenterTests
    {
        private GameStateMachine machine;
        private FakePlanner planner;
        private FakeArrival arrival;
        private FakeSpawner spawner;
        private GuestArrivalPresenter presenter;

        [SetUp]
        public void SetUp()
        {
            machine = new GameStateMachine();
            planner = new FakePlanner();
            arrival = new FakeArrival();
            spawner = new FakeSpawner();
            presenter = new GuestArrivalPresenter(machine, machine, planner, arrival, spawner);
        }

        [TearDown]
        public void TearDown()
        {
            presenter.Dispose();
        }

        [Test]
        public void StartWave_PlaysArrivalAndWaitsForIt()
        {
            machine.TryStartWave();

            Assert.AreEqual(1, arrival.Played);
            Assert.AreEqual(GameState.WaveIncoming, machine.Current);
            CollectionAssert.IsEmpty(spawner.Spawned);
        }

        [Test]
        public void AfterArrival_WaveGuestsAppearAndWaveStarts()
        {
            machine.TryStartWave();

            arrival.Dock();

            CollectionAssert.AreEqual(new[] { 12 }, spawner.Spawned);
            Assert.AreEqual(GameState.WaveActive, machine.Current);
        }

        [Test]
        public void NextWave_BringsItsOwnGuests()
        {
            machine.TryStartWave();
            arrival.Dock();
            machine.TryTransitionTo(GameState.WaveResolving);
            machine.TryTransitionTo(GameState.WaveComplete);
            machine.TryTransitionTo(GameState.Upgrade);
            machine.TryTransitionTo(GameState.AwaitingWave);

            machine.TryStartWave();
            arrival.Dock();

            CollectionAssert.AreEqual(new[] { 12, 18 }, spawner.Spawned);
        }

        [Test]
        public void WaveWithoutPlan_StartsWithoutGuests()
        {
            planner.Guests.Clear();
            machine.TryStartWave();

            arrival.Dock();

            CollectionAssert.AreEqual(new[] { 0 }, spawner.Spawned);
            Assert.AreEqual(GameState.WaveActive, machine.Current);
        }

        [Test]
        public void ArrivalFinishedAfterWaveMovedOn_IsIgnored()
        {
            machine.TryStartWave();
            machine.TryTransitionTo(GameState.WaveActive);

            arrival.Dock();

            CollectionAssert.IsEmpty(spawner.Spawned);
            Assert.AreEqual(GameState.WaveActive, machine.Current);
        }

        [Test]
        public void Dispose_StopsReactingToWaves()
        {
            presenter.Dispose();

            machine.TryStartWave();

            Assert.AreEqual(0, arrival.Played);
        }

        private class FakePlanner : IWavePlanner
        {
            public readonly Dictionary<int, int> Guests = new Dictionary<int, int> { [1] = 12, [2] = 18 };

            public WavePlan Plan(int wave) => Guests.TryGetValue(wave, out int guests) ? new WavePlan(wave, guests, new NeedDemand[0]) : null;
        }

        private class FakeArrival : IArrivalView
        {
            private Action docked;

            public int Played { get; private set; }

            public void PlayArrival(Action onDocked)
            {
                Played++;
                docked = onDocked;
            }

            public void Dock() => docked?.Invoke();
        }

        private class FakeSpawner : IGuestSpawner
        {
            public readonly List<int> Spawned = new List<int>();

            public void Spawn(int count) => Spawned.Add(count);
        }
    }
}
