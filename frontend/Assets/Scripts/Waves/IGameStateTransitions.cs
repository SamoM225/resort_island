namespace ResortIsland.Waves
{
    public interface IGameStateTransitions
    {
        bool CanTransitionTo(GameState next);
        bool TryTransitionTo(GameState next);
    }
}
