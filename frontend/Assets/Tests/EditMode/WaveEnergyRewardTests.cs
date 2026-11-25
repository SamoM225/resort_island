using System.IO;
using NUnit.Framework;
using UnityEngine;
using ResortIsland.Config;
using ResortIsland.Waves;

namespace ResortIsland.Tests
{
    public class WaveEnergyRewardTests
    {
        private GameStateMachine machine;
        private WaveRating rating;
        private WaveScore score;
        private WaveEnergyReward reward;
        private int energy;

        [SetUp]
        public void SetUp()
        {
            GameConfigs configs = GameConfigs.Parse(File.ReadAllText(Path.Combine(Application.dataPath, "Tests/EditMode/Fixtures/configs_response.json")));
            machine = new GameStateMachine();
            rating = WaveRating.From(configs);
            energy = 0;
            score = null;
            reward = new WaveEnergyReward(machine, () => score, amount => energy += amount);
        }

        [TearDown]
        public void TearDown()
        {
            reward.Dispose();
        }

        [Test]
        public void Claim_GivesTheEnergyOfTheRating()
        {
            FinishWave(waitSeconds: 15);
            Assert.AreEqual(0, energy, "nothing before Claim");

            Claim();

            Assert.AreEqual(4, score.Rating);
            Assert.AreEqual(3, energy, "drop_by_rating[4 stars]");
        }

        [Test]
        public void Perfect_GetsTheFiveStarEnergy()
        {
            FinishWave(waitSeconds: 0);
            Claim();

            Assert.AreEqual(WaveRating.Perfect, score.Rating);
            Assert.AreEqual(rating.EnergyFor(WaveRating.FiveStars), energy);
            Assert.AreEqual(5, energy);
        }

        [Test]
        public void EnergyIsGivenOncePerWave()
        {
            FinishWave(waitSeconds: 0);
            Claim();
            FinishWave(waitSeconds: 0);
            Claim();

            Assert.AreEqual(10, energy);
        }

        [Test]
        public void RewardCreatedOnWaveComplete_StillPaysOnClaim()
        {
            // e.g. Wave Complete restored from the save after a restart: nothing saw the wave finish
            reward.Dispose();
            FinishWave(waitSeconds: 0);
            reward = new WaveEnergyReward(machine, () => score, amount => energy += amount);

            Claim();

            Assert.AreEqual(5, energy);
        }

        [Test]
        public void WaveWithoutScore_GivesNothing()
        {
            machine.TryStartWave();
            machine.TryTransitionTo(GameState.WaveActive);
            machine.TryTransitionTo(GameState.WaveResolving);
            machine.TryTransitionTo(GameState.WaveComplete);
            Claim();

            Assert.AreEqual(0, energy);
        }

        private void FinishWave(double waitSeconds)
        {
            machine.TryStartWave();
            machine.TryTransitionTo(GameState.WaveActive);
            score = new WaveScore(rating, machine.NextWave);
            score.RecordWait("drinks", waitSeconds);
            score.RecordWait("rooms", waitSeconds);
            machine.TryTransitionTo(GameState.WaveResolving);
            machine.TryTransitionTo(GameState.WaveComplete);
        }

        // what WaveCompletePresenter does on Claim
        private void Claim()
        {
            machine.TryTransitionTo(GameState.Upgrade);
            machine.TryTransitionTo(GameState.AwaitingWave);
        }
    }
}
