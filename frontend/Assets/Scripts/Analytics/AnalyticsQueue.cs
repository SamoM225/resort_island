using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ResortIsland.Analytics
{
    public class AnalyticsQueue
    {
        private readonly IAnalyticsStore store;
        private readonly List<AnalyticsEvent> pending;

        public int Count => pending.Count;

        public AnalyticsQueue(IAnalyticsStore store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            pending = store.Load();
        }

        public void Enqueue(AnalyticsEvent analyticsEvent)
        {
            if (analyticsEvent == null)
                throw new ArgumentNullException(nameof(analyticsEvent));

            pending.Add(analyticsEvent);
            TryPersist();
        }

        public IReadOnlyList<AnalyticsEvent> Peek(int max) => pending.GetRange(0, Math.Min(max, pending.Count));

        public void Remove(int count)
        {
            pending.RemoveRange(0, Math.Min(count, pending.Count));
            TryPersist();
        }

        private void TryPersist()
        {
            try
            {
                store.Save(pending);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Analytics] Saving pending events failed: {e.Message}");
            }
        }
    }
}
