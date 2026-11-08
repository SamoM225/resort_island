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

        private class FakeAnalytics : IAnalytics
        {
            public readonly List<(string Name, Dictionary<string, object> Payload)> Events = new List<(string, Dictionary<string, object>)>();

            public void Track(string name, Dictionary<string, object> payload = null) => Events.Add((name, payload));
        }
    }
}
