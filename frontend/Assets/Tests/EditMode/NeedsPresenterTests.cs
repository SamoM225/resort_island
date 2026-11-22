using System;
using System.Collections.Generic;
using NUnit.Framework;
using ResortIsland.Waves;

namespace ResortIsland.Tests
{
    public class NeedsPresenterTests
    {
        private GameStateMachine machine;
        private FakePlanner planner;
        private FakeProgress progress;
        private FakeNeedsView view;
        private NeedsPresenter presenter;

        [SetUp]
        public void SetUp()
        {
            machine = new GameStateMachine();
            planner = new FakePlanner
            {
                Next = new WavePlan(1, 14, new List<NeedDemand> { new NeedDemand("rooms", 8), new NeedDemand("food", 6) })
            };
            progress = new FakeProgress();
            view = new FakeNeedsView();
            presenter = new NeedsPresenter(machine, planner, progress, view);
        }

        [TearDown]
        public void TearDown()
        {
            presenter.Dispose();
        }

        [Test]
        public void Rows_TakeNamesAndTotalsFromWavePlan()
        {
            Assert.AreEqual(2, view.Rows.Count);
            Assert.AreEqual("Rooms", view.Rows[0].Name);
            Assert.AreEqual(8, view.Rows[0].Total);
            Assert.AreEqual("Food", view.Rows[1].Name);
            Assert.AreEqual(6, view.Rows[1].Total);
        }

        [Test]
        public void WithoutProgressData_ServedAndQueueAreZero()
        {
            Assert.AreEqual(0, view.Rows[0].Served);
            Assert.AreEqual(0, view.Rows[0].Queue);
        }

        [Test]
        public void ProgressChange_UpdatesServedAndQueue()
        {
            progress.Set("rooms", served: 3, queue: 2);

            Assert.AreEqual(3, view.Rows[0].Served);
            Assert.AreEqual(2, view.Rows[0].Queue);
            Assert.AreEqual(0, view.Rows[1].Served);
        }

        [Test]
        public void StateChange_RefreshesRows()
        {
            view.Rows = null;

            machine.TryStartWave();

            Assert.IsNotNull(view.Rows);
        }

        [Test]
        public void PlanIsAskedForCurrentWaveNumber()
        {
            Assert.AreEqual(1, planner.LastWave);

            machine.TryStartWave();
            machine.TryTransitionTo(GameState.WaveActive);
            machine.TryTransitionTo(GameState.WaveResolving);
            machine.TryTransitionTo(GameState.WaveComplete);
            machine.TryTransitionTo(GameState.Upgrade);
            machine.TryTransitionTo(GameState.AwaitingWave);

            Assert.AreEqual(2, planner.LastWave);
        }

        [Test]
        public void MissingPlan_ShowsNoRows()
        {
            planner.Next = null;

            machine.TryStartWave();

            Assert.AreEqual(0, view.Rows.Count);
        }

        [Test]
        public void Dispose_StopsReacting()
        {
            presenter.Dispose();
            view.Rows = null;

            progress.Set("rooms", served: 5, queue: 1);
            machine.TryStartWave();

            Assert.IsNull(view.Rows);
        }

        private class FakePlanner : IWavePlanner
        {
            public WavePlan Next { get; set; }
            public int LastWave { get; private set; }

            public WavePlan Plan(int wave)
            {
                LastWave = wave;
                return Next;
            }
        }

        private class FakeProgress : INeedsProgress
        {
            private readonly Dictionary<string, int> served = new Dictionary<string, int>();
            private readonly Dictionary<string, int> queue = new Dictionary<string, int>();

            public event Action Changed;

            public int Served(string needId) => served.TryGetValue(needId, out int v) ? v : 0;

            public int Queue(string needId) => queue.TryGetValue(needId, out int v) ? v : 0;

            public void Set(string needId, int served, int queue)
            {
                this.served[needId] = served;
                this.queue[needId] = queue;
                Changed?.Invoke();
            }
        }

        private class FakeNeedsView : INeedsView
        {
            public IReadOnlyList<NeedRowState> Rows { get; set; }

            public void Show(IReadOnlyList<NeedRowState> needs) => Rows = needs;
        }
    }
}