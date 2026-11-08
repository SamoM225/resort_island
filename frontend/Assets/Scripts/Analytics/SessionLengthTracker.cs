using System;
using System.Collections.Generic;

namespace ResortIsland.Analytics
{
    public class SessionLengthTracker
    {
        public const string StartEvent = "session_start";
        public const string EndEvent = "session_end";
        public const string DurationKey = "duration_s";

        private readonly AnalyticsSession session;
        private readonly IAnalytics analytics;
        private readonly IClock clock;

        public SessionLengthTracker(AnalyticsSession session, IAnalytics analytics, IClock clock)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public void Start()
        {
            if (session.IsActive)
                return;

            session.Begin(clock.UtcNow);
            analytics.Track(StartEvent);
        }

        public void End()
        {
            if (!session.IsActive)
                return;

            double seconds = Math.Max(0, (clock.UtcNow - session.StartedAtUtc).TotalSeconds);
            analytics.Track(EndEvent, new Dictionary<string, object> { [DurationKey] = Math.Round(seconds, 1) });
            session.Close();
        }
    }
}
