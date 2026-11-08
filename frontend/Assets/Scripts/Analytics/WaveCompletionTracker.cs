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
        private readonly IWaveMetric[] metrics;

        public WaveCompletionTracker(IGameState state, IAnalytics analytics, params IWaveMetric[] metrics)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
            this.metrics = metrics ?? Array.Empty<IWaveMetric>();

            state.Changed += OnStateChanged;
        }

        public void Dispose()
        {
            state.Changed -= OnStateChanged;
        }

        private void OnStateChanged(GameState current)
        {
            if (current == GameState.WaveIncoming)
                analytics.Track(StartEvent, WavePayload());
            else if (current == GameState.WaveComplete)
                analytics.Track(CompleteEvent, CompletePayload());
        }

        private Dictionary<string, object> WavePayload() => new Dictionary<string, object> { [WaveKey] = state.NextWave };

        private Dictionary<string, object> CompletePayload()
        {
            Dictionary<string, object> payload = WavePayload();
            foreach (IWaveMetric metric in metrics)
                metric.AddTo(payload);
            return payload;
        }
    }
}
