using System;
using System.Collections.Generic;
using System.Globalization;

namespace ResortIsland.Analytics
{
    public class AnalyticsTracker : IAnalytics
    {
        public const string TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";

        private readonly AnalyticsQueue queue;
        private readonly AnalyticsSession session;
        private readonly IClock clock;

        public AnalyticsTracker(AnalyticsQueue queue, AnalyticsSession session, IClock clock)
        {
            this.queue = queue ?? throw new ArgumentNullException(nameof(queue));
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public void Track(string name, Dictionary<string, object> payload = null)
        {
            queue.Enqueue(new AnalyticsEvent
            {
                Name = name,
                OccurredAt = clock.UtcNow.ToString(TimestampFormat, CultureInfo.InvariantCulture),
                SessionId = session.Id,
                Payload = payload
            });
        }
    }
}
