using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ResortIsland.Analytics;

namespace ResortIsland.Tests
{
    public class AnalyticsQueueTests
    {
        private string directory;
        private AnalyticsFileStore store;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "ResortIslandAnalyticsTests", Guid.NewGuid().ToString("N"));
            store = new AnalyticsFileStore(directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }

        private static AnalyticsEvent Event(string name) => new AnalyticsEvent
        {
            Name = name,
            OccurredAt = "2026-11-08T12:00:00.000Z",
            SessionId = "7f3c2a8e-1d4b-4c6a-9e2f-0a1b2c3d4e5f",
            Payload = new Dictionary<string, object> { ["wave"] = 3 }
        };

        [Test]
        public void PendingEvents_SurviveAppRestart()
        {
            AnalyticsQueue queue = new AnalyticsQueue(store);
            queue.Enqueue(Event("session_start"));
            queue.Enqueue(Event("session_end"));

            AnalyticsQueue restarted = new AnalyticsQueue(new AnalyticsFileStore(directory));

            Assert.AreEqual(2, restarted.Count);
            AnalyticsEvent first = restarted.Peek(1)[0];
            Assert.AreEqual("session_start", first.Name);
            Assert.AreEqual("2026-11-08T12:00:00.000Z", first.OccurredAt);
            Assert.AreEqual("7f3c2a8e-1d4b-4c6a-9e2f-0a1b2c3d4e5f", first.SessionId);
            Assert.AreEqual(3, Convert.ToInt32(first.Payload["wave"]));
        }

        [Test]
        public void Peek_ReturnsOldestEventsUpToMax()
        {
            AnalyticsQueue queue = new AnalyticsQueue(store);
            queue.Enqueue(Event("a"));
            queue.Enqueue(Event("b"));
            queue.Enqueue(Event("c"));

            IReadOnlyList<AnalyticsEvent> batch = queue.Peek(2);

            Assert.AreEqual(2, batch.Count);
            Assert.AreEqual("a", batch[0].Name);
            Assert.AreEqual("b", batch[1].Name);
            Assert.AreEqual(3, queue.Count);
        }

        [Test]
        public void Remove_DropsSentEventsAlsoFromDisk()
        {
            AnalyticsQueue queue = new AnalyticsQueue(store);
            queue.Enqueue(Event("a"));
            queue.Enqueue(Event("b"));

            queue.Remove(1);

            AnalyticsQueue restarted = new AnalyticsQueue(new AnalyticsFileStore(directory));
            Assert.AreEqual(1, restarted.Count);
            Assert.AreEqual("b", restarted.Peek(1)[0].Name);
        }

        [Test]
        public void UnreadableFile_StartsWithEmptyQueue()
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(store.FilePath, "{ not json");

            AnalyticsQueue queue = new AnalyticsQueue(store);

            Assert.AreEqual(0, queue.Count);
        }
    }
}
