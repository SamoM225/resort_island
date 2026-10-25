using System;

namespace ResortIsland.Waves
{
    public interface IGameState
    {
        GameState Current { get; }
        event Action<GameState> Changed;
    }
}
