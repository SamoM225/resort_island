using System;
using System.Collections.Generic;
using ResortIsland.Waves;

namespace ResortIsland.Analytics
{
    public class WorkerMoveMetric : IWaveMetric, IDisposable
    {
        public const string PreWaveKey = "worker_moves_pre_wave";
        public const string InWaveKey = "worker_moves_in_wave";

        private readonly IGameState state;
        private readonly IWorkerMoves moves;
        private int movesBeforeNextWave;
        private int movesBeforeWave;
        private int movesInWave;

        public WorkerMoveMetric(IGameState state, IWorkerMoves moves)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.moves = moves ?? throw new ArgumentNullException(nameof(moves));

            state.Changed += OnStateChanged;
            moves.Moved += OnMoved;
        }

        public void Dispose()
        {
            state.Changed -= OnStateChanged;
            moves.Moved -= OnMoved;
        }

        public void AddTo(Dictionary<string, object> payload)
        {
            payload[PreWaveKey] = movesBeforeWave;
            payload[InWaveKey] = movesInWave;
        }

        private void OnStateChanged(GameState current)
        {
            if (current != GameState.WaveIncoming)
                return;

            movesBeforeWave = movesBeforeNextWave;
            movesBeforeNextWave = 0;
            movesInWave = 0;
        }

        private void OnMoved()
        {
            if (IsWaveRunning(state.Current))
                movesInWave++;
            else
                movesBeforeNextWave++;
        }

        private static bool IsWaveRunning(GameState current) =>
            current == GameState.WaveIncoming || current == GameState.WaveActive || current == GameState.WaveResolving;
    }
}
