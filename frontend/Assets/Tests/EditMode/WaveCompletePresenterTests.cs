using System;
using NUnit.Framework;
using ResortIsland.Waves;

namespace ResortIsland.Tests
{
    public class WaveCompletePresenterTests
    {
        private GameStateMachine machine;
        private FakeWaveCompleteView view;
        private FakeWaveResult result;
        private WaveCompletePresenter presenter;
        private int coinsAdded;
        private bool resultAvailable;

        [SetUp]
        public void SetUp()
        {
            machine = new GameStateMachine();
            view = new FakeWaveCompleteView();
            result = new FakeWaveResult { Rating = 4, Satisfaction = 0.88f };
            coinsAdded = 0;
            resultAvailable = true;

            presenter = new WaveCompletePresenter(
                machine,
                machine,
                () => resultAvailable ? result : null,
                _ => 120,
                amount => coinsAdded += amount,
                view);
        }

        [TearDown]
        public void TearDown()
        {
            presenter.Dispose();
        }

        [Test]
        public void Panel_IsHiddenUntilWaveComplete()
        {
            Assert.IsFalse(view.IsShown);

            machine.TryStartWave();
            machine.TryTransitionTo(GameState.WaveActive);
            machine.TryTransitionTo(GameState.WaveResolving);

            Assert.IsFalse(view.IsShown);
        }

        [Test]
        public void WaveComplete_ShowsRatingSatisfactionAndReward()
        {
            GoToWaveComplete();

            Assert.IsTrue(view.IsShown);
            Assert.AreEqual(4, view.Stars);
            Assert.IsFalse(view.Perfect);
            Assert.AreEqual(88, view.Percent);
            Assert.AreEqual(120, view.Coins);
        }

        [Test]
        public void FiveStars_ShowPerfect()
        {
            result.Rating = 5;
            result.Satisfaction = 1f;

            GoToWaveComplete();

            Assert.AreEqual(5, view.Stars);
            Assert.IsTrue(view.Perfect);
            Assert.AreEqual(100, view.Percent);
        }

        [Test]
        public void Rating_IsClampedToOneThroughFive()
        {
            result.Rating = 0;
            GoToWaveComplete();
            Assert.AreEqual(1, view.Stars);
        }

        [Test]
        public void RatingAboveFive_IsClampedToFive()
        {
            result.Rating = 9;
            GoToWaveComplete();
            Assert.AreEqual(5, view.Stars);
        }

        [Test]
        public void Satisfaction_IsRoundedToWholePercent()
        {
            result.Satisfaction = 0.884f;

            GoToWaveComplete();

            Assert.AreEqual(88, view.Percent);
        }

        [Test]
        public void Claim_AddsCoinsHidesPanelAndReturnsToAwaitingWave()
        {
            GoToWaveComplete();

            view.Click();

            Assert.AreEqual(120, coinsAdded);
            Assert.IsFalse(view.IsShown);
            Assert.AreEqual(GameState.AwaitingWave, machine.Current);
        }

        [Test]
        public void Claim_StartsNextWaveNumber()
        {
            GoToWaveComplete();
            int waveBefore = machine.NextWave;

            view.Click();

            Assert.AreEqual(waveBefore + 1, machine.NextWave);
        }

        [Test]
        public void Claim_TwiceInARow_AddsCoinsOnlyOnce()
        {
            GoToWaveComplete();

            view.Click();
            view.Click();

            Assert.AreEqual(120, coinsAdded);
        }

        [Test]
        public void Claim_WithoutShownResult_DoesNothing()
        {
            view.Click();

            Assert.AreEqual(0, coinsAdded);
            Assert.AreEqual(GameState.AwaitingWave, machine.Current);
        }

        [Test]
        public void MissingResult_KeepsPanelHidden()
        {
            resultAvailable = false;

            GoToWaveComplete();

            Assert.IsFalse(view.IsShown);
            Assert.AreEqual(GameState.WaveComplete, machine.Current);
        }

        [Test]
        public void RestoredOnWaveComplete_ShowsTheResultAgainAndClaimPays()
        {
            presenter.Dispose();
            GameStateMachine restored = new GameStateMachine();
            restored.Restore(4, GameState.WaveComplete);
            var restoredView = new FakeWaveCompleteView();

            using (new WaveCompletePresenter(restored, restored, () => result, _ => 120, amount => coinsAdded += amount, restoredView))
            {
                Assert.IsTrue(restoredView.IsShown);
                Assert.AreEqual(4, restoredView.Stars);

                restoredView.Click();
            }

            Assert.AreEqual(120, coinsAdded);
            Assert.AreEqual(GameState.AwaitingWave, restored.Current);
            Assert.AreEqual(5, restored.NextWave);
        }

        [Test]
        public void LeavingWaveComplete_HidesPanel()
        {
            GoToWaveComplete();

            machine.TryTransitionTo(GameState.Upgrade);

            Assert.IsFalse(view.IsShown);
        }

        [Test]
        public void Dispose_StopsReactingToStateAndClicks()
        {
            presenter.Dispose();

            GoToWaveComplete();
            view.Click();

            Assert.IsFalse(view.IsShown);
            Assert.AreEqual(0, coinsAdded);
        }

        private void GoToWaveComplete()
        {
            machine.TryStartWave();
            machine.TryTransitionTo(GameState.WaveActive);
            machine.TryTransitionTo(GameState.WaveResolving);
            machine.TryTransitionTo(GameState.WaveComplete);
        }

        private class FakeWaveCompleteView : IWaveCompleteView
        {
            public bool IsShown { get; private set; }
            public int Stars { get; private set; }
            public bool Perfect { get; private set; }
            public int Percent { get; private set; }
            public int Coins { get; private set; }

            public event Action Claimed;

            public void Show(int stars, bool perfect, int satisfactionPercent, int coins)
            {
                IsShown = true;
                Stars = stars;
                Perfect = perfect;
                Percent = satisfactionPercent;
                Coins = coins;
            }

            public void Hide() => IsShown = false;

            public void Click() => Claimed?.Invoke();
        }
    }
}