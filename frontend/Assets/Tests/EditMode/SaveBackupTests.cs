using System;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using ResortIsland.Backend;
using ResortIsland.Save;

namespace ResortIsland.Tests
{
    public class SaveBackupTests
    {
        private FakeBackendClient client;
        private InMemoryTokenStore tokens;
        private FakeClock clock;
        private SaveBackup backup;

        [SetUp]
        public void SetUp()
        {
            client = new FakeBackendClient();
            tokens = new InMemoryTokenStore { Token = "7|token" };
            clock = new FakeClock();
            backup = new SaveBackup(client, new AnonymousPlayerAuth(client, tokens), clock);
        }

        private static SaveData Save(int coins)
        {
            SaveData data = new SaveData { saveVersion = 1, savedAtUtc = "2026-11-11T10:00:00.0000000Z" };
            data.economy.coins = coins;
            return data;
        }

        [Test]
        public void Upload_PutsTheSaveWithItsVersionAndTime()
        {
            client.Responses.Enqueue(new BackendResponse(200, "{}"));

            backup.Upload(Save(1250));

            Assert.AreEqual("PUT", client.Methods[0]);
            Assert.AreEqual(SaveBackup.Endpoint, client.Requests[0].Path);
            Assert.AreEqual("7|token", client.Requests[0].Token);
            JObject body = JObject.Parse(client.Requests[0].Json);
            Assert.AreEqual(1, body.Value<int>("save_version"));
            StringAssert.Contains("\"saved_at\":\"2026-11-11T10:00:00.0000000Z\"", client.Requests[0].Json);
            Assert.AreEqual(1250, body["save"]["economy"].Value<int>("coins"));
            Assert.IsFalse(backup.HasPending);
        }

        [Test]
        public void Upload_IsThrottled_TheLatestSaveGoesLater()
        {
            client.Responses.Enqueue(new BackendResponse(200, "{}"));
            backup.Upload(Save(1));

            clock.UtcNow += TimeSpan.FromSeconds(5);
            backup.Upload(Save(2));
            backup.Upload(Save(3));

            Assert.AreEqual(1, client.Requests.Count);
            Assert.IsTrue(backup.HasPending);

            clock.UtcNow += SaveBackup.MinUploadInterval;
            backup.Upload(Save(4));

            Assert.AreEqual(2, client.Requests.Count);
            Assert.AreEqual(4, JObject.Parse(client.Requests[1].Json)["save"]["economy"].Value<int>("coins"));
        }

        [Test]
        public void UploadNow_IgnoresTheThrottle()
        {
            backup.Upload(Save(1));
            backup.Upload(Save(2), now: true);

            Assert.AreEqual(2, client.Requests.Count);
        }

        [Test]
        public void Offline_KeepsTheSaveForTheNextFlush()
        {
            client.Responses.Enqueue(new BackendResponse(0, null));

            backup.Upload(Save(1));

            Assert.IsTrue(backup.HasPending);
            backup.Flush();
            Assert.AreEqual(2, client.Requests.Count);
        }

        [Test]
        public void ExpiredToken_IsForgottenAndSaveKept()
        {
            client.Responses.Enqueue(new BackendResponse(401, "{}"));

            backup.Upload(Save(1));

            Assert.IsNull(tokens.Token);
            Assert.IsTrue(backup.HasPending);
        }

        [Test]
        public void NewerSaveOnAnotherDevice_IsNotRetried()
        {
            client.Responses.Enqueue(new BackendResponse(409, "{}"));

            backup.Upload(Save(1));

            Assert.IsFalse(backup.HasPending);
        }

        [Test]
        public void Download_ReturnsTheBackedUpSave()
        {
            client.Responses.Enqueue(new BackendResponse(200, "{\"data\":{\"save_version\":1,\"save\":{\"saveVersion\":1,\"economy\":{\"coins\":4000}}}}"));
            bool answered = false;
            string json = null;

            backup.Download((a, j) => { answered = a; json = j; });

            Assert.AreEqual("GET", client.Methods[0]);
            Assert.IsTrue(answered);
            Assert.AreEqual(4000, JObject.Parse(json)["economy"].Value<int>("coins"));
        }

        [TestCase(404, true)]
        [TestCase(0, false)]
        [TestCase(500, false)]
        public void Download_WithoutBackup_TellsWhetherTheBackendAnswered(long statusCode, bool expectedAnswered)
        {
            client.Responses.Enqueue(new BackendResponse(statusCode, "{}"));
            bool answered = !expectedAnswered;
            string json = "not called";

            backup.Download((a, j) => { answered = a; json = j; });

            Assert.AreEqual(expectedAnswered, answered);
            Assert.IsNull(json);
        }
    }
}
