using System;

namespace ResortIsland.Waves
{
    public class WaveWaits : IWaveResult, IDisposable
    {
        private readonly IGameState state;
        private readonly WaveRating rating;
        private readonly WaveBuildings buildings;

        public WaveWaits(IGameState state, WaveRating rating, WaveBuildings buildings)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.rating = rating ?? throw new ArgumentNullException(nameof(rating));
            this.buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));

            state.Changed += OnStateChanged;
            buildings.UnitSeated += OnUnitSeated;
        }

        public WaveScore Current { get; private set; }

        public int Rating => Current?.Rating ?? 0;
        public float Satisfaction => Current?.Satisfaction ?? 0f;

        public void Dispose()
        {
            state.Changed -= OnStateChanged;
            buildings.UnitSeated -= OnUnitSeated;
        }

        private void OnStateChanged(GameState current)
        {
            if (current == GameState.WaveActive)
                Current = new WaveScore(rating, state.NextWave);
        }

        private void OnUnitSeated(string needId, double wait)
        {
            if (state.Current == GameState.WaveActive || state.Current == GameState.WaveResolving)
                Current?.RecordWait(needId, wait);
        }
    }
}
