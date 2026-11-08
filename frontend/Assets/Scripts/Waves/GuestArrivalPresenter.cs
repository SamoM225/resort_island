using System;

namespace ResortIsland.Waves
{
    public class GuestArrivalPresenter : IDisposable
    {
        private readonly IGameState state;
        private readonly IGameStateTransitions transitions;
        private readonly IWavePlanner planner;
        private readonly IArrivalView arrival;
        private readonly IGuestSpawner guests;

        public GuestArrivalPresenter(IGameState state, IGameStateTransitions transitions, IWavePlanner planner, IArrivalView arrival, IGuestSpawner guests)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.transitions = transitions ?? throw new ArgumentNullException(nameof(transitions));
            this.planner = planner ?? throw new ArgumentNullException(nameof(planner));
            this.arrival = arrival ?? throw new ArgumentNullException(nameof(arrival));
            this.guests = guests ?? throw new ArgumentNullException(nameof(guests));

            state.Changed += OnStateChanged;
        }

        public void Dispose()
        {
            state.Changed -= OnStateChanged;
        }

        private void OnStateChanged(GameState current)
        {
            if (current == GameState.WaveIncoming)
                arrival.PlayArrival(OnDocked);
        }

        private void OnDocked()
        {
            if (state.Current != GameState.WaveIncoming)
                return;

            guests.Spawn(planner.Plan(state.NextWave)?.Guests ?? 0);
            transitions.TryTransitionTo(GameState.WaveActive);
        }
    }
}
