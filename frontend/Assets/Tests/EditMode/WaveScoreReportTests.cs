using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using ResortIsland.Analytics;
using ResortIsland.Config;
using ResortIsland.Waves;

namespace ResortIsland.Tests
{
    public class WaveScoreReportTests
    {
        private GameStateMachine machine;
        private WaveBuildings buildings;
        private WaveWaits waits;
        private FakeReporter reporter;
        private WaveScoreReport report;

        [SetUp]
        public void SetUp()
        {
            GameConfigs configs = GameConfigs.Parse(File.ReadAllText(Path.Combine(Application.dataPath, "Tests/EditMode/Fixtures/configs_response.json")));
            machine = new GameStateMachine();
            buildings = new WaveBuildings(new[] { new BuildingService("drinks", 1, new ServiceRules(12, 1, 0.33)) { Staffed = true } });
            waits = new WaveWaits(machine, WaveRating.From(configs), buildings);
            reporter = new FakeReporter();
            report = new WaveScoreReport(machine, waits, reporter);
        }

        [TearDown]
        public void TearDown()
        {
            report.Dispose();
            waits.Dispose();
        }

        [Test]
        public void DuringTheWave_NothingIsSent()
        {
            StartWave();
            buildings.Arrive("drinks");
            machine.TryTransitionTo(GameState.WaveResolving);

            CollectionAssert.IsEmpty(reporter.Sent);
        }

        [Test]
        public void CompleteWave_SendsItsScore()
        {
            StartWave();
            buildings.Arrive("drinks");
            buildings.Arrive("drinks");
            buildings.Tick(12.0);

            EndWave();

            Assert.AreEqual(1, reporter.Sent.Count);
            Assert.AreSame(waits.Current, reporter.Sent[0]);
            Assert.AreEqual(2, reporter.Sent[0].Units);
        }

        [Test]
        public void EveryWave_SendsItsOwnScore()
        {
            StartWave();
            EndWave();
            machine.TryTransitionTo(GameState.Upgrade);
            machine.TryTransitionTo(GameState.AwaitingWave);

            StartWave();
            EndWave();

            CollectionAssert.AreEqual(new[] { 1, 2 }, reporter.Sent.Select(score => score.Wave));
        }

        [Test]
        public void Dispose_StopsSending()
        {
            StartWave();

            report.Dispose();
            EndWave();

            CollectionAssert.IsEmpty(reporter.Sent);
        }

        [Test]
        public void WaveResult_IsTheRatingOfTheScore()
        {
            StartWave();
            buildings.Arrive("drinks");
            buildings.Arrive("drinks");
            buildings.Tick(60.0);
            EndWave();

            Assert.AreEqual(waits.Current.Rating, waits.Rating);
            Assert.AreEqual(waits.Current.Satisfaction, waits.Satisfaction);
            Assert.Less(waits.Satisfaction, 100f);
        }

        [Test]
        public void CompleteWave_LogsItsRating()
        {
            StartWave();
            buildings.Arrive("drinks");
            EndWave();
            Dictionary<string, object> payload = new Dictionary<string, object>();

            new WaveRatingMetric(waits).AddTo(payload);

            Assert.AreEqual(WaveRating.Perfect, payload[WaveRatingMetric.RatingKey]);
            Assert.AreEqual(100.0, (double)payload[WaveRatingMetric.SatisfactionKey], 1e-6);
        }

        private void StartWave()
        {
            machine.TryStartWave();
            machine.TryTransitionTo(GameState.WaveActive);
        }

        private void EndWave()
        {
            machine.TryTransitionTo(GameState.WaveResolving);
            machine.TryTransitionTo(GameState.WaveComplete);
        }

        private class FakeReporter : IWaveReporter
        {
            public readonly List<WaveScore> Sent = new List<WaveScore>();

            public void Report(WaveScore score) => Sent.Add(score);
        }
    }
}
