using NUnit.Framework;
using ResortIsland.Waves;

namespace ResortIsland.Tests
{
    public class WaveForecastPresenterTests
    {
        private GameStateMachine machine;
        private FakePlanner planner;
        private FakeForecastView view;

        [SetUp]
        public void SetUp()
        {
            machine = new GameStateMachine();
            planner = new FakePlanner();
            view = new FakeForecastView();
        }

        [Test]
        public void WhileAwaitingWave_ShowsPlanOfNextWave()
        {
            using (new WaveForecastPresenter(machine, planner, view))
            {
                Assert.AreEqual(machine.NextWave, planner.RequestedWave);
                Assert.AreSame(planner.Result, view.Shown);
            }
        }

        [Test]
        public void WaveStarted_HidesForecast()
        {
            using (new WaveForecastPresenter(machine, planner, view))
            {
                machine.TryStartWave();

                Assert.IsNull(view.Shown);
            }
        }

        [Test]
        public void NoPlanForNextWave_HidesForecast()
        {
            planner.Result = null;

            using (new WaveForecastPresenter(machine, planner, view))
                Assert.IsNull(view.Shown);
        }

        [Test]
        public void Dispose_StopsReactingToStateChanges()
        {
            new WaveForecastPresenter(machine, planner, view).Dispose();

            machine.TryStartWave();

            Assert.AreSame(planner.Result, view.Shown);
        }

        private class FakePlanner : IWavePlanner
        {
            public WavePlan Result = new WavePlan(1, new[] { new NeedDemand("rooms", 8) });
            public int RequestedWave;

            public WavePlan Plan(int wave)
            {
                RequestedWave = wave;
                return Result;
            }
        }

        private class FakeForecastView : IWaveForecastView
        {
            public WavePlan Shown { get; private set; }

            public void Show(WavePlan plan) => Shown = plan;

            public void Hide() => Shown = null;
        }
    }
}
