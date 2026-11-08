using System;

namespace ResortIsland.Waves
{
    public class StartWavePresenter : IDisposable
    {
        private readonly IGameState state;
        private readonly IWaveStarter starter;
        private readonly IStartWaveView view;

        public StartWavePresenter(IGameState state, IWaveStarter starter, IStartWaveView view)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.starter = starter ?? throw new ArgumentNullException(nameof(starter));
            this.view = view ?? throw new ArgumentNullException(nameof(view));

            view.Clicked += OnClicked;
            state.Changed += OnStateChanged;
            Refresh();
        }

        public void Dispose()
        {
            view.Clicked -= OnClicked;
            state.Changed -= OnStateChanged;
        }

        private void OnClicked() => starter.TryStartWave();

        private void OnStateChanged(GameState _) => Refresh();

        private void Refresh() => view.SetInteractable(starter.CanStartWave);
    }
}
