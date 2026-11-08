using System;

namespace ResortIsland.Waves
{
    public class GameStateMachine : IGameState, IWaveStarter
    {
        public GameState Current { get; private set; } = GameState.AwaitingWave;

        public int NextWave { get; } = 1;

        public event Action<GameState> Changed;

        public bool CanStartWave => Current == GameState.AwaitingWave;

        public bool TryStartWave()
        {
            if (!CanStartWave)
                return false;

            TransitionTo(GameState.WaveIncoming);
            return true;
        }

        private void TransitionTo(GameState next)
        {
            Current = next;
            Changed?.Invoke(next);
        }
    }
}
