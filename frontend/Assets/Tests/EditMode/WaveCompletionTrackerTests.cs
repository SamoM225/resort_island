using System.Collections.Generic;
using NUnit.Framework;
using ResortIsland.Analytics;
using ResortIsland.Waves;

namespace ResortIsland.Tests
{
    public class WaveCompletionTrackerTests
    {
        private GameStateMachine machine;
        private FakeAnalytics analytics;
        private WaveCompletionTracker tracker;

        [SetUp]
        public void SetUp()
        {
            machine = new GameStateMachine();
            analytics = new FakeAnalytics();
            tracker = new WaveCompletionTracker(machine, analytics);
        }

        [TearDown]
        public void TearDown()
        {
            tracker.Dispose();
        }

        private void PlayWaveUntil(GameState last)
        {
            foreach (GameState next in new[] { GameState.WaveIncoming, GameState.WaveActive, GameState.WaveResolving, GameState.WaveComplete, GameState.Upgrade, GameState.AwaitingWave })
            {
                machine.TryTransitionTo(next);
                if (next == last)
                    return;
            }
        }

        [Test]
        public void StartWave_LogsWaveStartWithWaveNumber()
        {
            machine.TryStartWave();

            Assert.AreEqual(1, analytics.Events.Count);
            Assert.AreEqual(WaveCompletionTracker.StartEvent, analytics.Events[0].Name);
            Assert.AreEqual(1, analytics.Events[0].Payload[WaveCompletionTracker.WaveKey]);
        }

        [Test]
        public void ReachingResult_LogsWaveCompleteOfSameWave()
        {
            PlayWaveUntil(GameState.WaveComplete);

            Assert.AreEqual(2, analytics.Events.Count);
            Assert.AreEqual(WaveCompletionTracker.CompleteEvent, analytics.Events[1].Name);
            Assert.AreEqual(1, analytics.Events[1].Payload[WaveCompletionTracker.WaveKey]);
        }

        [Test]
        public void AbandonedWave_LogsOnlyStart()
        {
            PlayWaveUntil(GameState.WaveResolving);

            Assert.AreEqual(1, analytics.Events.Count);
            Assert.AreEqual(WaveCompletionTracker.StartEvent, analytics.Events[0].Name);
        }

        [Test]
        public void FullCycle_LogsOneStartAndOneComplete()
        {
            PlayWaveUntil(GameState.AwaitingWave);

            CollectionAssert.AreEqual(
                new[] { WaveCompletionTracker.StartEvent, WaveCompletionTracker.CompleteEvent },
                analytics.Events.ConvertAll(e => e.Name));
        }

        [Test]
        public void NextWave_IsLoggedWithItsNumber()
        {
            PlayWaveUntil(GameState.AwaitingWave);

            machine.TryStartWave();

            Assert.AreEqual(2, analytics.Events[2].Payload[WaveCompletionTracker.WaveKey]);
        }

        [Test]
        public void Dispose_StopsLogging()
        {
            tracker.Dispose();

            machine.TryStartWave();

            Assert.AreEqual(0, analytics.Events.Count);
        }

        [Test]
        public void WaveComplete_CarriesAttachedMetrics()
        {
            tracker.Dispose();
            tracker = new WaveCompletionTracker(machine, analytics, new FakeMetric("rating", 4), new FakeMetric("worker_moves_in_wave", 2));

            PlayWaveUntil(GameState.WaveComplete);

            Dictionary<string, object> complete = analytics.Events[1].Payload;
            Assert.AreEqual(1, complete[WaveCompletionTracker.WaveKey]);
            Assert.AreEqual(4, complete["rating"]);
            Assert.AreEqual(2, complete["worker_moves_in_wave"]);
            CollectionAssert.AreEquivalent(new[] { WaveCompletionTracker.WaveKey }, analytics.Events[0].Payload.Keys);
        }

        [Test]
        public void WaveComplete_ReportsRatingAndWorkerMovesAsInReadme()
        {
            FakeWorkerMoves moves = new FakeWorkerMoves();
            WorkerMoveMetric workerMoves = new WorkerMoveMetric(machine, moves);
            tracker.Dispose();
            tracker = new WaveCompletionTracker(machine, analytics, new WaveRatingMetric(new FakeWaveResult { Rating = 4, Satisfaction = 86.04f }), workerMoves);

            moves.Move();
            machine.TryStartWave();
            moves.Move();
            moves.Move();
            PlayWaveUntil(GameState.WaveComplete);
            workerMoves.Dispose();

            Dictionary<string, object> complete = analytics.Events[1].Payload;
            Assert.AreEqual(WaveCompletionTracker.CompleteEvent, analytics.Events[1].Name);
            Assert.AreEqual(1, complete["wave"]);
            Assert.AreEqual(4, complete["rating"]);
            Assert.AreEqual(86.0, (double)complete["satisfaction"], 0.0001);
            Assert.AreEqual(1, complete["worker_moves_pre_wave"]);
            Assert.AreEqual(2, complete["worker_moves_in_wave"]);
        }

        private class FakeAnalytics : IAnalytics
        {
            public readonly List<(string Name, Dictionary<string, object> Payload)> Events = new List<(string, Dictionary<string, object>)>();

            public void Track(string name, Dictionary<string, object> payload = null) => Events.Add((name, payload));
        }

        private class FakeMetric : IWaveMetric
        {
            private readonly string key;
            private readonly object value;

            public FakeMetric(string key, object value)
            {
                this.key = key;
                this.value = value;
            }

            public void AddTo(Dictionary<string, object> payload) => payload[key] = value;
        }
    }
}
