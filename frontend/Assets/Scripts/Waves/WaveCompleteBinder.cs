using UnityEngine;
using ResortIsland.Backend;
using ResortIsland.Config;
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

        private void Start()
        {
            if (waveController == null || view == null || economy == null)
            {
                Debug.LogError("[WaveCompleteBinder] Priraď Wave Controller, View a Player Economy v Inspectore.");
                return;
            }

            IGameState state = waveController.State;
            IGameStateTransitions transitions = state as IGameStateTransitions;
            if (transitions == null)
            {
                Debug.LogError("[WaveCompleteBinder] Stav hry nepodporuje prechody.");
                return;
            }

            presenter = new WaveCompletePresenter(state, transitions, FindResult, CalculateCoins, ClaimRewards, view);
        }

        private void OnDestroy()
        {
            presenter?.Dispose();
        }

        private IWaveResult FindResult() => waveController.Result;

        private int CalculateCoins(IWaveResult result) => waveController.Score?.Coins ?? 0;

        // Claim: the coins of the wave and the resort value of its rating (economy.resort_value_by_rating).
        private void ClaimRewards(int coins)
        {
            economy.AddCoins(coins);
            IWaveResult result = waveController.Result;
            if (result != null)
                economy.AddResortValue(new GameTuning(GameConfigService.Current).RatingValue(result.Rating));
        }
    }
}