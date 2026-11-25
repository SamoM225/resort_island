using System;

namespace ResortIsland.Waves
{
    /// <summary>
    /// Gives the energy of a finished wave (energy.drop_by_rating, Perfect = the 5-star drop) when the player
    /// claims it on Wave Complete. Claim is the only way from WaveComplete to Upgrade, so the reward is paid on
    /// that step, once per wave, from the score of the claimed wave (also one restored from the save after a restart).
    /// The economy caps it at max_energy.
    /// </summary>
    public sealed class WaveEnergyReward : IDisposable
    {
        private readonly IGameState state;
        private readonly Func<WaveScore> scoreProvider;
        private readonly Action<int> addEnergy;

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
            if (current != GameState.Upgrade)
                return;

            int energy = scoreProvider()?.Energy ?? 0;
            if (energy > 0)
                addEnergy(energy);
        }
    }
}
