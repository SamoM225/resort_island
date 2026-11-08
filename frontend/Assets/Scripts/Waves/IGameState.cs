using System;

namespace ResortIsland.Waves
{
    public interface IGameState
    {
        GameState Current { get; }
        int NextWave { get; }
        event Action<GameState> Changed;
    }
}
