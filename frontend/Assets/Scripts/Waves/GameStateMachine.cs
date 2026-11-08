using System;
using System.Collections.Generic;

namespace ResortIsland.Waves
{
    public class GameStateMachine : IGameState, IWaveStarter, IGameStateTransitions
    {
        private static readonly IReadOnlyDictionary<GameState, GameState> NextState = new Dictionary<GameState, GameState>
        {
            [GameState.AwaitingWave] = GameState.WaveIncoming,
            [GameState.WaveIncoming] = GameState.WaveActive,
            [GameState.WaveActive] = GameState.WaveResolving,
            [GameState.WaveResolving] = GameState.WaveComplete,
            [GameState.WaveComplete] = GameState.Upgrade,
            [GameState.Upgrade] = GameState.AwaitingWave
        };

        public GameState Current { get; private set; } = GameState.AwaitingWave;

        public int NextWave { get; private set; } = 1;

        public event Action<GameState> Changed;

        public bool CanStartWave => CanTransitionTo(GameState.WaveIncoming);

        public bool TryStartWave() => TryTransitionTo(GameState.WaveIncoming);

        public bool CanTransitionTo(GameState next) => NextState[Current] == next;

        public bool TryTransitionTo(GameState next)
        {
            if (!CanTransitionTo(next))
                return false;

            if (next == GameState.AwaitingWave)
                NextWave++;

            Current = next;
            Changed?.Invoke(next);
            return true;
        }
    }
}
