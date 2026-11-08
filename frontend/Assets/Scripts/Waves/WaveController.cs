using UnityEngine;
using ResortIsland.Analytics;
using ResortIsland.Backend;
using ResortIsland.Config;
using ResortIsland.UI;

namespace ResortIsland.Waves
{
    public class WaveController : MonoBehaviour
    {
        [SerializeField] private StartWaveButton startWaveButton;
        [SerializeField] private WaveForecastPanel waveForecast;

        private readonly GameStateMachine stateMachine = new GameStateMachine();
        private StartWavePresenter startWavePresenter;
        private WaveForecastPresenter waveForecastPresenter;
        private WaveCompletionTracker waveCompletionTracker;

        public IGameState State => stateMachine;
        public IWavePlanner Planner { get; private set; }

        private void Start()
        {
            BindStartWave();
            BindForecast();
            BindAnalytics();
        }

        private void OnDestroy()
        {
            startWavePresenter?.Dispose();
            waveForecastPresenter?.Dispose();
            waveCompletionTracker?.Dispose();
        }

        private void BindStartWave()
        {
            if (startWaveButton == null)
            {
                Debug.LogError("[WaveController] Start Wave button is not assigned! Please assign it in the inspector.");
                return;
            }

            startWavePresenter = new StartWavePresenter(stateMachine, stateMachine, startWaveButton);
        }

        private void BindForecast()
        {
            GameConfigs configs = GameConfigService.Current;
            if (configs == null)
            {
                Debug.LogError("[WaveController] Game configs are not loaded, the next wave cannot be planned.");
                return;
            }

            Planner = new ScriptedWavePlanner(configs.Waves, StartingNeeds.From(configs.Buildings));

            if (waveForecast == null)
            {
                Debug.LogError("[WaveController] Wave forecast is not assigned! Please assign it in the inspector.");
                return;
            }

            waveForecastPresenter = new WaveForecastPresenter(stateMachine, Planner, waveForecast);
        }

        private void BindAnalytics()
        {
            IAnalytics analytics = AnalyticsService.Current;
            if (analytics == null)
            {
                Debug.LogWarning("[WaveController] Analytics service is not running, waves will not be logged.");
                return;
            }

            waveCompletionTracker = new WaveCompletionTracker(stateMachine, analytics);
        }
    }
}
