using System;

namespace ResortIsland.Waves
{
    /// <summary>
    /// Gives the energy of a finished wave (energy.drop_by_rating, Perfect = the 5-star drop) when the player
    /// claims it on Wave Complete. Claim is the only way from WaveComplete to Upgrade, so the reward is paid on
    /// that step, once per wave. The economy caps it at max_energy.
    /// </summary>
    public sealed class WaveEnergyReward : IDisposable
    {
        private readonly IGameState state;
        private readonly Func<WaveScore> scoreProvider;
        private readonly Action<int> addEnergy;

        private int pendingEnergy;

        public WaveEnergyReward(IGameState state, Func<WaveScore> scoreProvider, Action<int> addEnergy)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.scoreProvider = scoreProvider ?? throw new ArgumentNullException(nameof(scoreProvider));
            this.addEnergy = addEnergy ?? throw new ArgumentNullException(nameof(addEnergy));

            state.Changed += OnStateChanged;
        }

        public void Dispose()
        {
            state.Changed -= OnStateChanged;
        }

        private void OnStateChanged(GameState current)
        {
            if (current == GameState.WaveComplete)
            {
                pendingEnergy = scoreProvider()?.Energy ?? 0;
            }
            else if (current == GameState.Upgrade)
            {
                int energy = pendingEnergy;
                pendingEnergy = 0;
                if (energy > 0)
                    addEnergy(energy);
            }
        }
    }
}
