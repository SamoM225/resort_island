using UnityEngine;
using ResortIsland.Economy;
using ResortIsland.UI;

namespace ResortIsland.Waves
{
    public class WaveCompleteBinder : MonoBehaviour
    {
        [SerializeField] private WaveController waveController;
        [SerializeField] private WaveCompleteView view;
        [SerializeField] private PlayerEconomy economy;

        private WaveCompletePresenter presenter;
        private WaveEnergyReward energyReward;

        private void Start()
        {
            if (waveController == null || view == null || economy == null)
            {
                Debug.LogError("[WaveCompleteBinder] Priraď Wave Controller, View a Player Economy v Inspectore.");
                return;
            }

            // energy of the wave on Claim (energy.drop_by_rating), capped at max_energy by the economy
            energyReward = new WaveEnergyReward(waveController.State, () => waveController.Score, economy.RestoreEnergy);

            IGameState state = waveController.State;
            IGameStateTransitions transitions = state as IGameStateTransitions;
            if (transitions == null)
            {
                Debug.LogError("[WaveCompleteBinder] Stav hry nepodporuje prechody.");
                return;
            }

            presenter = new WaveCompletePresenter(state, transitions, FindResult, CalculateCoins, economy.AddCoins, view);
        }

        private void OnDestroy()
        {
            presenter?.Dispose();
            energyReward?.Dispose();
        }

        private IWaveResult FindResult() => waveController.Result;

        private int CalculateCoins(IWaveResult result) => waveController.Score?.Coins ?? 0;
    }
}