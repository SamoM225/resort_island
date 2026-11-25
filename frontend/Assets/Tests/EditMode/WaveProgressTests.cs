using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using ResortIsland.Config;
using ResortIsland.Save;
using ResortIsland.Waves;

namespace ResortIsland.Tests
{
    public class WaveProgressTests
    {
        private GameStateMachine machine;
        private WaveRating rating;
        private WaveScore score;
        private List<WaveSaveData> saves;
        private WaveProgress progress;

        [SetUp]
        public void SetUp()
        {
            GameConfigs configs = GameConfigs.Parse(File.ReadAllText(Path.Combine(Application.dataPath, "Tests/EditMode/Fixtures/configs_response.json")));
            machine = new GameStateMachine();
            rating = WaveRating.From(configs);
            score = null;
            saves = new List<WaveSaveData>();
            progress = new WaveProgress(machine, () => score, saves.Add);
        }

        [TearDown]
        public void TearDown()
        {
            progress.Dispose();
        }

        private WaveSaveData Last => saves[saves.Count - 1];

        [Test]
        public void EveryStateChange_SavesWaveNumberAndState()
        {
            machine.TryStartWave();
            Assert.AreEqual(1, Last.nextWave);
            Assert.AreEqual("WaveIncoming", Last.state);

            machine.TryTransitionTo(GameState.WaveActive);
            Assert.AreEqual("WaveActive", Last.state);
            Assert.IsNull(Last.unclaimedWaits);
        }

        [Test]
        public void WaveComplete_SavesTheWaitsUntilClaim()
        {
            PlayWave(waits: new[] { 0.0, 15.0 });

            Assert.AreEqual("WaveComplete", Last.state);
            CollectionAssert.AreEqual(new[] { 0.0, 15.0 }, Last.unclaimedWaits["drinks"]);

            Claim();

            Assert.AreEqual(2, Last.nextWave);
            Assert.AreEqual("AwaitingWave", Last.state);
            Assert.IsNull(Last.unclaimedWaits);
        }

        [TestCase("AwaitingWave", 3, 3)]
        [TestCase("WaveIncoming", 3, 3)]
        [TestCase("WaveActive", 3, 3)]
        [TestCase("WaveResolving", 3, 3)]
        [TestCase("Upgrade", 3, 4)]
        [TestCase("not a state", 3, 3)]
        public void Resume_KilledWaveStartsAgain_ClaimedWaveGoesOn(string savedState, int savedWave, int expectedWave)
        {
            var (wave, state) = WaveProgress.Resume(new WaveSaveData { nextWave = savedWave, state = savedState });

            Assert.AreEqual(expectedWave, wave);
            Assert.AreEqual(GameState.AwaitingWave, state);
        }

        [Test]
        public void Resume_UnclaimedWaveComplete_ComesBack()
        {
            var saved = new WaveSaveData { nextWave = 5, state = "WaveComplete", unclaimedWaits = new Dictionary<string, List<double>> { ["rooms"] = new List<double> { 0 } } };

            Assert.AreEqual((5, GameState.WaveComplete), WaveProgress.Resume(saved));
        }

        [Test]
        public void Resume_WaveCompleteWithoutWaits_PlaysTheWaveAgain()
        {
            Assert.AreEqual((5, GameState.AwaitingWave), WaveProgress.Resume(new WaveSaveData { nextWave = 5, state = "WaveComplete" }));
        }

        [Test]
        public void Resume_NoSave_StartsAtWaveOne()
        {
            Assert.AreEqual((1, GameState.AwaitingWave), WaveProgress.Resume(null));
        }

        [Test]
        public void RestoreScore_RatesTheSavedWaitsLikeTheWaveDid()
        {
            PlayWave(waits: new[] { 0.0, 15.0, 40.0 });
            WaveScore played = score;

            WaveScore restored = WaveProgress.RestoreScore(rating, Last);

            Assert.AreEqual(played.Wave, restored.Wave);
            Assert.AreEqual(played.Units, restored.Units);
            Assert.AreEqual(played.Rating, restored.Rating);
            Assert.AreEqual(played.Coins, restored.Coins);
            Assert.AreEqual(played.SatisfactionRatio, restored.SatisfactionRatio, 1e-12);
        }

        [Test]
        public void RestoreScore_SkipsNeedsTheConfigsNoLongerHave()
        {
            var saved = new WaveSaveData
            {
                nextWave = 2,
                state = "WaveComplete",
                unclaimedWaits = new Dictionary<string, List<double>> { ["rooms"] = new List<double> { 1 }, ["spa"] = new List<double> { 2 } }
            };

            Assert.AreEqual(1, WaveProgress.RestoreScore(rating, saved).Units);
        }

        private void PlayWave(double[] waits)
        {
            machine.TryStartWave();
            machine.TryTransitionTo(GameState.WaveActive);
            score = new WaveScore(rating, machine.NextWave);
            foreach (double wait in waits)
                score.RecordWait("drinks", wait);
            machine.TryTransitionTo(GameState.WaveResolving);
            machine.TryTransitionTo(GameState.WaveComplete);
        }

        private void Claim()
        {
            machine.TryTransitionTo(GameState.Upgrade);
            machine.TryTransitionTo(GameState.AwaitingWave);
        }
    }
}
