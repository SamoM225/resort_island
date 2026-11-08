using System;

namespace ResortIsland.Waves
{
    public class WaveForecastPresenter : IDisposable
    {
        private readonly IGameState state;
        private readonly IWavePlanner planner;
        private readonly IWaveForecastView view;

        public WaveForecastPresenter(IGameState state, IWavePlanner planner, IWaveForecastView view)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.planner = planner ?? throw new ArgumentNullException(nameof(planner));
            this.view = view ?? throw new ArgumentNullException(nameof(view));

            state.Changed += OnStateChanged;
            Refresh();
        }

        public void Dispose()
        {
            state.Changed -= OnStateChanged;
        }

        private void OnStateChanged(GameState _) => Refresh();

        private void Refresh()
        {
            WavePlan plan = state.Current == GameState.AwaitingWave ? planner.Plan(state.NextWave) : null;
            if (plan == null)
                view.Hide();
            else
                view.Show(plan);
        }
    }
}
