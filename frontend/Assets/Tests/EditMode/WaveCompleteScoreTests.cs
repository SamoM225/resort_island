using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using ResortIsland.Config;
using ResortIsland.Waves;

namespace ResortIsland.Tests
{
    public class WaveCompleteScoreTests
    {
        private GameStateMachine machine;
        private WaveBuildings buildings;
        private WaveWaits waits;
        private FakeView view;
        private WaveCompletePresenter presenter;
        private int wallet;

        [SetUp]
        public void SetUp()
        {
            GameConfigs configs = GameConfigs.Parse(File.ReadAllText(Path.Combine(Application.dataPath, "Tests/EditMode/Fixtures/configs_response.json")));
            machine = new GameStateMachine();
            buildings = new WaveBuildings(new[] { new BuildingService("drinks", 1, new ServiceRules(12, 1, 0.33)) { Staffed = true } });
            waits = new WaveWaits(machine, WaveRating.From(configs), buildings);
            view = new FakeView();
            wallet = 0;
            presenter = new WaveCompletePresenter(machine, machine, () => waits, result => waits.Current?.Coins ?? 0, coins => wallet += coins, view);
        }

        [TearDown]
        public void TearDown()
        {
            presenter.Dispose();
            waits.Dispose();
        }

        [Test]
        public void WaveComplete_ShowsTheRatingOfTheWaits()
        {
            PlayWave();

            Assert.IsTrue(view.Shown);
            Assert.AreEqual(5, view.Stars);
        }

        [Test]
        public void WaveComplete_ShowsTheCoinsOfTheRating()
        {
            PlayWave();

            Assert.AreEqual(38, view.Coins);
        }

        [Test]
        public void Claim_AddsTheCoinsToTheWallet()
        {
            PlayWave();

            view.Claim();

            Assert.AreEqual(38, wallet);
        }

        private void PlayWave()
        {
            machine.TryStartWave();
            machine.TryTransitionTo(GameState.WaveActive);
            buildings.Arrive("drinks");
            buildings.Arrive("drinks");
            buildings.Tick(24.0);
            machine.TryTransitionTo(GameState.WaveResolving);
            machine.TryTransitionTo(GameState.WaveComplete);
        }

        private class FakeView : IWaveCompleteView
        {
            public event Action Claimed;

            public bool Shown { get; private set; }
            public int Stars { get; private set; }
            public int Coins { get; private set; }

            public void Show(int stars, bool perfect, int satisfactionPercent, int coins)
            {
                Shown = true;
                Stars = stars;
                Coins = coins;
            }

            public void Hide() => Shown = false;

            public void Claim() => Claimed?.Invoke();
        }
    }
}
