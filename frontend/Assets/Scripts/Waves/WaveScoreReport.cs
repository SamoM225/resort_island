using System;

namespace ResortIsland.Waves
{
    public class WaveScoreReport : IDisposable
    {
        private readonly IGameState state;
        private readonly WaveWaits waits;
        private readonly IWaveReporter reporter;

        public WaveScoreReport(IGameState state, WaveWaits waits, IWaveReporter reporter)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.waits = waits ?? throw new ArgumentNullException(nameof(waits));
            this.reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));

            state.Changed += OnStateChanged;
        }

        public void Dispose()
        {
            state.Changed -= OnStateChanged;
        }

        private void OnStateChanged(GameState current)
        {
            if (current == GameState.WaveComplete && waits.Current != null)
                reporter.Report(waits.Current);
        }
    }
}
