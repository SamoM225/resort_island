using System;
using System.Collections.Generic;
using ResortIsland.Waves;

namespace ResortIsland.Analytics
{
    public class WaveCompletionTracker : IDisposable
    {
        public const string StartEvent = "wave_start";
        public const string CompleteEvent = "wave_complete";
        public const string WaveKey = "wave";

        private readonly IGameState state;
        private readonly IAnalytics analytics;

        public WaveCompletionTracker(IGameState state, IAnalytics analytics)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));

            state.Changed += OnStateChanged;
        }

        public void Dispose()
        {
            state.Changed -= OnStateChanged;
        }

        private void OnStateChanged(GameState current)
        {
            if (current == GameState.WaveIncoming)
                Track(StartEvent);
            else if (current == GameState.WaveComplete)
                Track(CompleteEvent);
        }

        private void Track(string name) => analytics.Track(name, new Dictionary<string, object> { [WaveKey] = state.NextWave });
    }
}
