using System;
using System.Collections.Generic;
using NUnit.Framework;
using ResortIsland.Analytics;

namespace ResortIsland.Tests
{
    public class SessionLengthTrackerTests
    {
        private FakeClock clock;
        private AnalyticsQueue queue;
        private AnalyticsSession session;
        private AnalyticsTracker tracker;
        private SessionLengthTracker sessionLength;

        [SetUp]
        public void SetUp()
        {
            clock = new FakeClock();
            queue = new AnalyticsQueue(new InMemoryAnalyticsStore());
            session = new AnalyticsSession();
            tracker = new AnalyticsTracker(queue, session, clock);
            sessionLength = new SessionLengthTracker(session, tracker, clock);
        }

        private IReadOnlyList<AnalyticsEvent> Events => queue.Peek(int.MaxValue);

        [Test]
        public void Start_LogsSessionStartWithNewSessionId()
        {
            sessionLength.Start();

            Assert.AreEqual(1, Events.Count);
            Assert.AreEqual(SessionLengthTracker.StartEvent, Events[0].Name);
            Assert.AreEqual("2026-11-08T12:00:00.000Z", Events[0].OccurredAt);
            Assert.IsTrue(Guid.TryParse(Events[0].SessionId, out _));
        }

        [Test]
        public void End_LogsSessionLengthInSeconds()
        {
            sessionLength.Start();
            clock.UtcNow = clock.UtcNow.AddSeconds(754.32);

            sessionLength.End();

            AnalyticsEvent end = Events[1];
            Assert.AreEqual(SessionLengthTracker.EndEvent, end.Name);
            Assert.AreEqual(Events[0].SessionId, end.SessionId);
            Assert.AreEqual(754.3, Convert.ToDouble(end.Payload[SessionLengthTracker.DurationKey]), 0.0001);
        }

        [Test]
        public void EventsDuringSession_CarryItsId()
        {
            sessionLength.Start();

            tracker.Track("wave_started", new Dictionary<string, object> { ["wave"] = 1 });

            Assert.AreEqual(Events[0].SessionId, Events[1].SessionId);
            Assert.AreEqual(1, Events[1].Payload["wave"]);
        }

        [Test]
        public void RepeatedStart_KeepsRunningSession()
        {
            sessionLength.Start();
            sessionLength.Start();

            Assert.AreEqual(1, Events.Count);
        }

        [Test]
        public void EndWithoutRunningSession_LogsNothing()
        {
            sessionLength.End();
            sessionLength.Start();
            sessionLength.End();
            sessionLength.End();

            Assert.AreEqual(2, Events.Count);
        }

        [Test]
        public void ReturnFromBackground_StartsNewSession()
        {
            sessionLength.Start();
            sessionLength.End();

            sessionLength.Start();

            Assert.AreEqual(SessionLengthTracker.StartEvent, Events[2].Name);
            Assert.AreNotEqual(Events[0].SessionId, Events[2].SessionId);
            Assert.IsTrue(session.IsActive);
        }
    }
}
