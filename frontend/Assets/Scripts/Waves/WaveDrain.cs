using System;
using System.Collections.Generic;
using System.Linq;

namespace ResortIsland.Waves
{
    public class WaveDrain
    {
        private readonly IGameState state;
        private readonly IGameStateTransitions transitions;
        private readonly WaveArrivalsPresenter arrivals;
        private readonly Func<IEnumerable<IBuildingQueue>> queues;

        public WaveDrain(IGameState state, IGameStateTransitions transitions, WaveArrivalsPresenter arrivals, Func<IEnumerable<IBuildingQueue>> queues)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.transitions = transitions ?? throw new ArgumentNullException(nameof(transitions));
            this.arrivals = arrivals ?? throw new ArgumentNullException(nameof(arrivals));
            this.queues = queues ?? throw new ArgumentNullException(nameof(queues));
        }

        public void Check()
        {
            if (state.Current == GameState.WaveActive && arrivals.Current != null && arrivals.Current.TimerEnded)
                transitions.TryTransitionTo(GameState.WaveResolving);

            if (state.Current == GameState.WaveResolving && AllClear())
                transitions.TryTransitionTo(GameState.WaveComplete);
        }

        private bool AllClear() => (queues() ?? Enumerable.Empty<IBuildingQueue>()).All(queue => queue.IsClear);
    }
}
