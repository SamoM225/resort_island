using System.Collections.Generic;
using UnityEngine;
using ResortIsland.Analytics;
using ResortIsland.Backend;
using ResortIsland.Characters;
using ResortIsland.Config;
using ResortIsland.Resort;
using ResortIsland.Save;
using ResortIsland.UI;

namespace ResortIsland.Waves
{
    /// Runs before the binders (Wave Complete, needs panel), so they find the wave progress already restored.
    [DefaultExecutionOrder(-50)]
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
        private WaveDrain waveDrain;
        private WaveTicker waveTicker;
        private WaveWaits waveWaits;
        private WaveScoreReport waveScoreReport;
        private WaveProgress waveProgress;

        public IGameState State => stateMachine;
        public IWavePlanner Planner { get; private set; }
        public WaveArrivalsPresenter Arrivals { get; private set; }
        public List<IBuildingQueue> BuildingQueues { get; } = new List<IBuildingQueue>();
        public WaveBuildings Buildings { get; private set; }
        public IWaveResult Result => waveWaits;
        public WaveScore Score => waveWaits?.Current;

        private void Start()
        {
            WaveScore unclaimed = RestoreProgress();
            BindStartWave();
            BindForecast();
            BindWaveArrivals();
            if (unclaimed != null) waveWaits?.Restore(unclaimed);
            BindAnalytics();
            BindArrival();
            waveProgress = new WaveProgress(stateMachine, () => waveWaits?.Current, SaveProgress);
        }

        private void Update()
        {
            waveTicker?.Tick(Time.deltaTime);
            waveDrain?.Check();
        }

        private void OnDestroy()
        {
            startWavePresenter?.Dispose();
            waveForecastPresenter?.Dispose();
            waveCompletionTracker?.Dispose();
            guestArrivalPresenter?.Dispose();
            Arrivals?.Dispose();
            waveWaits?.Dispose();
            waveScoreReport?.Dispose();
            waveProgress?.Dispose();
        }

        /// The wave and state from the save, before anything is bound: a wave killed midway starts again,
        /// Wave Complete not claimed yet comes back with its result (returned, rated again from the saved waits).
        private WaveScore RestoreProgress()
        {
            SaveStore store = SaveStore.Shared;
            WaveSaveData saved = store.IsLoaded ? store.Data.wave : null;
            (int nextWave, GameState state) = WaveProgress.Resume(saved);

            WaveScore unclaimed = null;
            if (state == GameState.WaveComplete)
            {
                GameConfigs configs = GameConfigService.Current;
                unclaimed = configs != null ? WaveProgress.RestoreScore(WaveRating.From(configs), saved) : null;
                if (unclaimed == null)
                {
                    Debug.LogWarning("[WaveController] The unclaimed wave could not be restored, it is played again.");
                    state = GameState.AwaitingWave;
                }
            }

            stateMachine.Restore(nextWave, state);
            if (saved != null && (saved.nextWave != nextWave || saved.state != state.ToString()))
                Debug.Log($"[WaveController] The game was closed in wave {saved.nextWave} ({saved.state}); going on with wave {nextWave} ({state}).");
            return unclaimed;
        }

        private static void SaveProgress(WaveSaveData progress)
        {
            SaveStore store = SaveStore.Shared;
            if (!store.IsLoaded)
                return;

            store.Data.wave = progress;
            try
            {
                store.Save();
            }
            catch (System.Exception e)
            {
                Debug.LogError("[WaveController] Saving the wave progress failed: " + e.Message);
            }
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

            waveCompletionTracker = waveWaits != null
                ? new WaveCompletionTracker(stateMachine, analytics, new WaveRatingMetric(waveWaits))
                : new WaveCompletionTracker(stateMachine, analytics);
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
            Buildings = WaveBuildings.From(configs);
            BuildingQueues.AddRange(Buildings.Queues);
            Arrivals.UnitArrived += Buildings.Arrive;
            waveTicker = new WaveTicker(Arrivals, Buildings);
            waveWaits = new WaveWaits(stateMachine, WaveRating.From(configs), Buildings);
            IBackendClient client = new UnityBackendClient();
            waveScoreReport = new WaveScoreReport(stateMachine, waveWaits, new WaveResultReporter(client, new AnonymousPlayerAuth(client, new PlayerPrefsTokenStore())));
            waveDrain = new WaveDrain(stateMachine, stateMachine, Arrivals, () => BuildingQueues);
        }
    }
}
