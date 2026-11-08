using Newtonsoft.Json.Linq;
using NUnit.Framework;
using ResortIsland.Analytics;
using ResortIsland.Backend;

namespace ResortIsland.Tests
{
    public class AnalyticsDispatcherTests
    {
        private FakeBackendClient client;
        private InMemoryTokenStore tokens;
        private AnalyticsQueue queue;
        private AnalyticsTracker tracker;
        private AnalyticsDispatcher dispatcher;

        [SetUp]
        public void SetUp()
        {
            client = new FakeBackendClient();
            tokens = new InMemoryTokenStore { Token = "5|player" };
            queue = new AnalyticsQueue(new InMemoryAnalyticsStore());
            tracker = new AnalyticsTracker(queue, new AnalyticsSession(), new FakeClock());
            dispatcher = new AnalyticsDispatcher(queue, new AnonymousPlayerAuth(client, tokens), client, "0.3.0", "android");
        }

        private void TrackEvents(int count)
        {
            for (int i = 0; i < count; i++)
                tracker.Track("session_start");
        }

        [Test]
        public void Flush_SendsPendingEventsWithPlayerToken()
        {
            TrackEvents(2);
            client.Responses.Enqueue(new BackendResponse(202, "{\"accepted\":2}"));

            dispatcher.Flush();

            Assert.AreEqual(1, client.Requests.Count);
            Assert.AreEqual(AnalyticsDispatcher.Endpoint, client.Requests[0].Path);
            Assert.AreEqual("5|player", client.Requests[0].Token);
            JObject body = JObject.Parse(client.Requests[0].Json);
            Assert.AreEqual("0.3.0", body.Value<string>("app_version"));
            Assert.AreEqual("android", body.Value<string>("platform"));
            Assert.AreEqual(2, ((JArray)body["events"]).Count);
            Assert.AreEqual("session_start", body["events"][0].Value<string>("name"));
            Assert.AreEqual(0, queue.Count);
            Assert.IsFalse(dispatcher.IsSending);
        }

        [Test]
        public void Flush_WithoutToken_RegistersPlayerFirst()
        {
            tokens.Token = null;
            TrackEvents(1);
            client.Responses.Enqueue(new BackendResponse(201, "{\"token\":\"9|new\"}"));
            client.Responses.Enqueue(new BackendResponse(202, "{\"accepted\":1}"));

            dispatcher.Flush();

            Assert.AreEqual(AnonymousPlayerAuth.Endpoint, client.Requests[0].Path);
            Assert.AreEqual(AnalyticsDispatcher.Endpoint, client.Requests[1].Path);
            Assert.AreEqual("9|new", client.Requests[1].Token);
            Assert.AreEqual(0, queue.Count);
        }

        [Test]
        public void Flush_SendsAtMostOneBatchLimit()
        {
            TrackEvents(AnalyticsDispatcher.MaxBatchSize + 50);
            client.Responses.Enqueue(new BackendResponse(202, null));

            dispatcher.Flush();

            JObject body = JObject.Parse(client.Requests[0].Json);
            Assert.AreEqual(AnalyticsDispatcher.MaxBatchSize, ((JArray)body["events"]).Count);
            Assert.AreEqual(50, queue.Count);
        }

        [Test]
        public void EmptyQueue_SendsNothing()
        {
            dispatcher.Flush();

            Assert.AreEqual(0, client.Requests.Count);
        }

        [Test]
        public void FailedRegistration_KeepsEvents()
        {
            tokens.Token = null;
            TrackEvents(1);
            client.Responses.Enqueue(new BackendResponse(0, null));

            dispatcher.Flush();

            Assert.AreEqual(1, client.Requests.Count);
            Assert.AreEqual(1, queue.Count);
            Assert.IsFalse(dispatcher.IsSending);
        }

        [Test]
        public void Unauthorized_ForgetsTokenAndKeepsEvents()
        {
            TrackEvents(1);
            client.Responses.Enqueue(new BackendResponse(401, "{\"message\":\"Unauthenticated.\"}"));

            dispatcher.Flush();

            Assert.IsNull(tokens.Token);
            Assert.AreEqual(1, queue.Count);
        }

        [TestCase(0)]
        [TestCase(408)]
        [TestCase(429)]
        [TestCase(500)]
        public void TemporaryFailure_KeepsEventsForNextFlush(long statusCode)
        {
            TrackEvents(1);
            client.Responses.Enqueue(new BackendResponse(statusCode, null));

            dispatcher.Flush();

            Assert.AreEqual(1, queue.Count);
            Assert.AreEqual("5|player", tokens.Token);
            Assert.IsFalse(dispatcher.IsSending);
        }

        [TestCase(403)]
        [TestCase(422)]
        public void RejectedBatch_IsDropped(long statusCode)
        {
            TrackEvents(1);
            client.Responses.Enqueue(new BackendResponse(statusCode, null));

            dispatcher.Flush();

            Assert.AreEqual(0, queue.Count);
        }
    }
}
