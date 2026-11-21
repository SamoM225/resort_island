using UnityEngine;
using ResortIsland.Analytics;
using ResortIsland.Backend;
using ResortIsland.Characters;
using ResortIsland.Config;
using ResortIsland.Resort;
using ResortIsland.UI;

namespace ResortIsland.Waves
{
    public class WaveController : MonoBehaviour
    {
        [SerializeField] private StartWaveButton startWaveButton;
        [SerializeField] private WaveForecastPanel waveForecast;
        [SerializeField] private FerryArrival ferryArrival;
        [SerializeField] private GuestSpawner guestSpawner;

        private readonly GameStateMachine stateMachine = new GameStateMachine();
        private StartWavePresenter startWavePresenter;
        private WaveForecastPresenter waveForecastPresenter;
        private WaveCompletionTracker waveCompletionTracker;
        private GuestArrivalPresenter guestArrivalPresenter;

        public IGameState State => stateMachine;
        public IWavePlanner Planner { get; private set; }
        public WaveArrivalsPresenter Arrivals { get; private set; }

        private void Start()
        {
            BindStartWave();
            BindForecast();
            BindAnalytics();
            BindArrival();
            BindWaveArrivals();
        }

        private void Update()
        {
            Arrivals?.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            startWavePresenter?.Dispose();
            waveForecastPresenter?.Dispose();
            waveCompletionTracker?.Dispose();
            guestArrivalPresenter?.Dispose();
            Arrivals?.Dispose();
        }

        /// Moves the Start Wave button and the forecast to other views (the bottom bar of the HUD).
        /// The game rules stay in the presenters; the views assigned in the scene are hidden.
        public void UseViews(IStartWaveView startView, IWaveForecastView forecastView)
        {
            if (startView != null)
            {
                startWavePresenter?.Dispose();
                startWavePresenter = new StartWavePresenter(stateMachine, stateMachine, startView);
                if (startWaveButton != null && !ReferenceEquals(startView, startWaveButton))
                    startWaveButton.gameObject.SetActive(false);
            }

            if (forecastView != null)
            {
                waveForecastPresenter?.Dispose();
                waveForecastPresenter = Planner != null ? new WaveForecastPresenter(stateMachine, Planner, forecastView) : null;
                if (waveForecastPresenter == null) forecastView.Hide();
                if (waveForecast != null && !ReferenceEquals(forecastView, waveForecast))
                    waveForecast.gameObject.SetActive(false);
            }
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

        private void BindArrival()
        {
            if (Planner == null)
                return;

            if (ferryArrival == null || guestSpawner == null)
            {
                Debug.LogError("[WaveController] Ferry arrival or guest spawner is not assigned! Please assign them in the inspector.");
                return;
            }

            guestArrivalPresenter = new GuestArrivalPresenter(stateMachine, stateMachine, Planner, ferryArrival, guestSpawner);
        }

        private void BindWaveArrivals()
        {
            GameConfigs configs = GameConfigService.Current;
            if (Planner == null || configs?.WaveSystem == null)
                return;

            Arrivals = new WaveArrivalsPresenter(stateMachine, Planner, configs.WaveSystem.WaveTimerS);
        }
    }
}
