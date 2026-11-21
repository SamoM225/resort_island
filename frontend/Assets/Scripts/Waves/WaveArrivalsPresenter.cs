using System;

namespace ResortIsland.Waves
{
    public class WaveArrivalsPresenter : IDisposable
    {
        private readonly IGameState state;
        private readonly IWavePlanner planner;
        private readonly double waveTimer;

        public WaveArrivalsPresenter(IGameState state, IWavePlanner planner, double waveTimer)
        {
            if (waveTimer <= 0 || double.IsNaN(waveTimer))
                throw new ArgumentOutOfRangeException(nameof(waveTimer), "The wave timer must be above 0.");

            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.planner = planner ?? throw new ArgumentNullException(nameof(planner));
            this.waveTimer = waveTimer;

            state.Changed += OnStateChanged;
        }

        public event Action<string> UnitArrived;

        public WaveArrivals Current { get; private set; }

        public void Dispose()
        {
            state.Changed -= OnStateChanged;
            Release();
        }

        public void Tick(double seconds)
        {
            if (state.Current == GameState.WaveActive)
                Current?.Tick(seconds);
        }

        private void OnStateChanged(GameState current)
        {
            if (current != GameState.WaveActive)
                return;

            Release();
            WavePlan plan = planner.Plan(state.NextWave) ?? new WavePlan(state.NextWave, 0, Array.Empty<NeedDemand>());
            Current = new WaveArrivals(plan, waveTimer);
            Current.UnitArrived += OnUnitArrived;
        }

        private void Release()
        {
            if (Current != null)
                Current.UnitArrived -= OnUnitArrived;
            Current = null;
        }

        private void OnUnitArrived(string needId) => UnitArrived?.Invoke(needId);
    }
}
