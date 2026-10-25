using System;
using NUnit.Framework;
using ResortIsland.Waves;

namespace ResortIsland.Tests
{
    public class StartWavePresenterTests
    {
        private GameStateMachine machine;
        private FakeStartWaveView view;
        private StartWavePresenter presenter;

        [SetUp]
        public void SetUp()
        {
            machine = new GameStateMachine();
            view = new FakeStartWaveView();
            presenter = new StartWavePresenter(machine, machine, view);
        }

        [TearDown]
        public void TearDown()
        {
            presenter.Dispose();
        }

        [Test]
        public void Button_IsActiveWhileAwaitingWave()
        {
            Assert.IsTrue(view.Interactable);
        }

        [Test]
        public void Click_StartsWaveAndDeactivatesButton()
        {
            view.Click();

            Assert.AreEqual(GameState.WaveIncoming, machine.Current);
            Assert.IsFalse(view.Interactable);
        }

        [Test]
        public void Button_IsInactiveDuringWave()
        {
            machine.TryStartWave();

            Assert.IsFalse(view.Interactable);
        }

        [Test]
        public void Dispose_StopsReactingToClicks()
        {
            presenter.Dispose();

            view.Click();

            Assert.AreEqual(GameState.AwaitingWave, machine.Current);
        }

        private class FakeStartWaveView : IStartWaveView
        {
            public bool Interactable { get; private set; }

            public event Action Clicked;

            public void SetInteractable(bool interactable) => Interactable = interactable;

            public void Click() => Clicked?.Invoke();
        }
    }
}
