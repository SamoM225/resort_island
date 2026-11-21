using System;
using System.Collections.Generic;

namespace ResortIsland.Waves
{
    public class NeedsPresenter : IDisposable
    {
        private readonly IGameState state;
        private readonly IWavePlanner planner;
        private readonly INeedsProgress progress;
        private readonly INeedsView view;

        public NeedsPresenter(IGameState state, IWavePlanner planner, INeedsProgress progress, INeedsView view)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.planner = planner ?? throw new ArgumentNullException(nameof(planner));
            this.progress = progress ?? throw new ArgumentNullException(nameof(progress));
            this.view = view ?? throw new ArgumentNullException(nameof(view));

            state.Changed += OnStateChanged;
            progress.Changed += Refresh;
            Refresh();
        }

        public void Dispose()
        {
            state.Changed -= OnStateChanged;
            progress.Changed -= Refresh;
        }

        private void OnStateChanged(GameState _) => Refresh();

        private void Refresh()
        {
            var rows = new List<NeedRowState>();
            WavePlan plan = planner.Plan(state.NextWave);

            if (plan != null && plan.Needs != null)
            {
                foreach (NeedDemand need in plan.Needs)
                {
                    rows.Add(new NeedRowState(
                        DisplayName(need.NeedId),
                        progress.Served(need.NeedId),
                        need.Units,
                        progress.Queue(need.NeedId)));
                }
            }

            view.Show(rows);
        }

        private static string DisplayName(string needId)
        {
            if (string.IsNullOrEmpty(needId))
                return string.Empty;

            return char.ToUpperInvariant(needId[0]) + needId.Substring(1);
        }
    }
}