using System;
using UnityEngine;

namespace ResortIsland.Waves
{
    public class WaveCompletePresenter : IDisposable
    {
        public const int MaxStars = 5;

        private readonly IGameState state;
        private readonly IGameStateTransitions transitions;
        private readonly Func<IWaveResult> resultProvider;
        private readonly Func<IWaveResult, int> coinReward;
        private readonly Action<int> addCoins;
        private readonly IWaveCompleteView view;

        private bool isShown;
        private int pendingCoins;

        public WaveCompletePresenter(
            IGameState state,
            IGameStateTransitions transitions,
            Func<IWaveResult> resultProvider,
            Func<IWaveResult, int> coinReward,
            Action<int> addCoins,
            IWaveCompleteView view)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.transitions = transitions ?? throw new ArgumentNullException(nameof(transitions));
            this.resultProvider = resultProvider ?? throw new ArgumentNullException(nameof(resultProvider));
            this.coinReward = coinReward ?? throw new ArgumentNullException(nameof(coinReward));
            this.addCoins = addCoins ?? throw new ArgumentNullException(nameof(addCoins));
            this.view = view ?? throw new ArgumentNullException(nameof(view));

            view.Claimed += OnClaimed;
            state.Changed += OnStateChanged;
        }

        public void Dispose()
        {
            view.Claimed -= OnClaimed;
            state.Changed -= OnStateChanged;
        }

        private void OnStateChanged(GameState current)
        {
            if (current == GameState.WaveComplete)
            {
                ShowResult();
            }
            else if (isShown)
            {
                isShown = false;
                view.Hide();
            }
        }

        private void ShowResult()
        {
            IWaveResult result = resultProvider();
            if (result == null)
            {
                Debug.LogWarning("[WaveCompletePresenter] Výsledok vlny (IWaveResult) sa nenašiel, obrazovka sa nezobrazí.");
                return;
            }

            int stars = Math.Clamp(result.Rating, 1, MaxStars);
            int percent = (int)Math.Round(Math.Clamp(result.Satisfaction, 0f, 1f) * 100f);
            pendingCoins = Math.Max(0, coinReward(result));

            isShown = true;
            view.Show(stars, stars == MaxStars, percent, pendingCoins);
        }

        private void OnClaimed()
        {
            if (!isShown)
                return;

            isShown = false;
            addCoins(pendingCoins);
            view.Hide();

            transitions.TryTransitionTo(GameState.Upgrade);
            transitions.TryTransitionTo(GameState.AwaitingWave);
        }
    }
}