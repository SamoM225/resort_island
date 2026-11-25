using System;
using System.Collections.Generic;
using ResortIsland.Save;

namespace ResortIsland.Waves
{
    /// <summary>
    /// Keeps the wave progress in the save: on every state change it writes the wave number and the state,
    /// and on Wave Complete also the waits of the wave, so a reward not claimed yet survives the app being killed.
    /// On start, Resume says where the game goes on.
    /// </summary>
    public sealed class WaveProgress : IDisposable
    {
        private readonly IGameState state;
        private readonly Func<WaveScore> scoreProvider;
        private readonly Action<WaveSaveData> save;

        public WaveProgress(IGameState state, Func<WaveScore> scoreProvider, Action<WaveSaveData> save)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.scoreProvider = scoreProvider ?? throw new ArgumentNullException(nameof(scoreProvider));
            this.save = save ?? throw new ArgumentNullException(nameof(save));

            state.Changed += OnStateChanged;
        }

        public void Dispose()
        {
            state.Changed -= OnStateChanged;
        }

        private void OnStateChanged(GameState current) => save(Snapshot(state, current == GameState.WaveComplete ? scoreProvider() : null));

        public static WaveSaveData Snapshot(IGameState state, WaveScore unclaimed)
        {
            var data = new WaveSaveData { nextWave = state.NextWave, state = state.Current.ToString() };
            if (state.Current == GameState.WaveComplete && unclaimed != null)
            {
                data.unclaimedWaits = new Dictionary<string, List<double>>();
                foreach (KeyValuePair<string, List<double>> need in unclaimed.Waits)
                    data.unclaimedWaits[need.Key] = new List<double>(need.Value);
            }
            return data;
        }

        /// <summary>
        /// Where the game goes on after a restart:
        /// - waiting for a wave: the same;
        /// - in a wave (incoming, running, draining): the same wave from the beginning, waiting to be started;
        /// - Wave Complete with its waits: Wave Complete again with the same result (Claim pays the reward);
        /// - Wave Complete without waits: the same wave again (the result is gone, the player plays it once more);
        /// - Upgrade (reached only by Claim, the reward is paid): the next wave.
        /// </summary>
        public static (int NextWave, GameState State) Resume(WaveSaveData saved)
        {
            if (saved == null)
                return (1, GameState.AwaitingWave);

            int wave = Math.Max(1, saved.nextWave);
            if (!Enum.TryParse(saved.state, out GameState state))
                return (wave, GameState.AwaitingWave);

            switch (state)
            {
                case GameState.WaveComplete:
                    return saved.unclaimedWaits != null ? (wave, GameState.WaveComplete) : (wave, GameState.AwaitingWave);
                case GameState.Upgrade:
                    return (wave + 1, GameState.AwaitingWave);
                default:
                    return (wave, GameState.AwaitingWave);
            }
        }

        /// <summary>The score of the unclaimed wave, rated again from its waits with the current configs.</summary>
        public static WaveScore RestoreScore(WaveRating rating, WaveSaveData saved)
        {
            if (rating == null) throw new ArgumentNullException(nameof(rating));
            if (saved?.unclaimedWaits == null) return null;

            var score = new WaveScore(rating, Math.Max(1, saved.nextWave));
            foreach (KeyValuePair<string, List<double>> need in saved.unclaimedWaits)
            {
                if (need.Value == null || !rating.HasNeed(need.Key)) continue;
                foreach (double wait in need.Value)
                    if (wait >= 0 && !double.IsNaN(wait)) score.RecordWait(need.Key, wait);
            }
            return score;
        }
    }
}
