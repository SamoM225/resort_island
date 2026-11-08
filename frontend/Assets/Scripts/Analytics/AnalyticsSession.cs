using System;

namespace ResortIsland.Analytics
{
    public class AnalyticsSession
    {
        public string Id { get; private set; }
        public DateTime StartedAtUtc { get; private set; }

        public bool IsActive => Id != null;

        public void Begin(DateTime nowUtc)
        {
            Id = Guid.NewGuid().ToString();
            StartedAtUtc = nowUtc;
        }

        public void Close() => Id = null;
    }
}
