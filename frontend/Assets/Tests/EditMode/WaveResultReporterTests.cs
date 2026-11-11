using System.Collections.Generic;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using ResortIsland.Backend;
using ResortIsland.Config;
using ResortIsland.Waves;

namespace ResortIsland.Tests
{
    public class WaveResultReporterTests
    {
        private FakeBackendClient client;
        private InMemoryTokenStore tokens;
        private WaveResultReporter reporter;

        [SetUp]
        public void SetUp()
        {
            client = new FakeBackendClient();
            tokens = new InMemoryTokenStore { Token = "7|token" };
            reporter = new WaveResultReporter(client, new AnonymousPlayerAuth(client, tokens));
        }

        private static WaveScore Score(int wave)
        {
            WaveRating rating = new WaveRating(
                new WaveSystemConfig { PatienceGraceS = 10, PatienceFloorAfterS = 30, UnitFloor = 0.25f, WaveFloor = 0.4f, RatingThresholds = new[] { 0.4f, 0.55f, 0.7f, 0.8f, 0.9f, 1f } },
                new EconomyConfig { CoinsPerUnit = 16, CoinsRatingMult = new[] { 0.4f, 0.6f, 0.8f, 1f, 1.2f, 1.3f } },
                new List<NeedDefinition> { new NeedDefinition { Id = "drinks", SatisfactionWeight = 1f } });
            WaveScore score = new WaveScore(rating, wave);
            score.RecordWait("drinks", 0);
            score.RecordWait("drinks", 12.5);
            return score;
        }

        [Test]
        public void Report_PostsTheWaitsAndTheGamesResult()
        {
            client.Responses.Enqueue(new BackendResponse(201, "{}"));

            reporter.Report(Score(3));

            Assert.AreEqual(WaveResultReporter.Endpoint, client.Requests[0].Path);
            Assert.AreEqual("7|token", client.Requests[0].Token);
            JObject body = JObject.Parse(client.Requests[0].Json);
            Assert.AreEqual(3, body.Value<int>("wave"));
            CollectionAssert.AreEqual(new[] { 0.0, 12.5 }, body["waits"]["drinks"].Values<double>());
            Assert.AreEqual(5, body["client"].Value<int>("rating"));
            Assert.AreEqual(38, body["client"].Value<int>("coins"));
            Assert.AreEqual(0, reporter.Pending);
        }

        [Test]
        public void Offline_KeepsReportsInOrderForTheNextFlush()
        {
            reporter.Report(Score(1));
            reporter.Report(Score(2));
            Assert.AreEqual(2, reporter.Pending);

            client.Responses.Enqueue(new BackendResponse(201, "{}"));
            client.Responses.Enqueue(new BackendResponse(201, "{}"));
            client.Requests.Clear();
            reporter.Flush();

            Assert.AreEqual(0, reporter.Pending);
            Assert.AreEqual(1, JObject.Parse(client.Requests[0].Json).Value<int>("wave"));
            Assert.AreEqual(2, JObject.Parse(client.Requests[1].Json).Value<int>("wave"));
        }

        [Test]
        public void RejectedReport_IsDropped()
        {
            client.Responses.Enqueue(new BackendResponse(422, "{}"));

            reporter.Report(Score(1));

            Assert.AreEqual(0, reporter.Pending);
        }
    }
}
