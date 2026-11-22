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

            presenter = new WaveCompletePresenter(state, transitions, FindResult, CalculateCoins, economy.AddCoins, view);
        }

        private void OnDestroy()
        {
            presenter?.Dispose();
        }

        private IWaveResult FindResult()
        {
            foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (behaviour is IWaveResult found)
                    return found;
            }

            return null;
        }

        private int CalculateCoins(IWaveResult result)
        {
            return 0;
        }
    }
}