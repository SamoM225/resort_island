using NUnit.Framework;
using ResortIsland.Backend;

namespace ResortIsland.Tests
{
    public class AnonymousPlayerAuthTests
    {
        private FakeBackendClient client;
        private InMemoryTokenStore store;
        private AnonymousPlayerAuth auth;

        [SetUp]
        public void SetUp()
        {
            client = new FakeBackendClient();
            store = new InMemoryTokenStore();
            auth = new AnonymousPlayerAuth(client, store);
        }

        private string RequestToken()
        {
            string received = null;
            auth.RequestToken(token => received = token);
            return received;
        }

        [Test]
        public void StoredToken_IsReusedWithoutRegistration()
        {
            store.Token = "1|stored";

            Assert.AreEqual("1|stored", RequestToken());
            Assert.AreEqual(0, client.Requests.Count);
        }

        [Test]
        public void FirstLaunch_RegistersAnonymousPlayerAndStoresToken()
        {
            client.Responses.Enqueue(new BackendResponse(201, "{\"data\":{\"id\":7,\"is_anonymous\":true},\"token\":\"7|fresh\"}"));

            Assert.AreEqual("7|fresh", RequestToken());
            Assert.AreEqual(AnonymousPlayerAuth.Endpoint, client.Requests[0].Path);
            Assert.IsNull(client.Requests[0].Token);
            Assert.AreEqual("7|fresh", store.Token);
        }

        [TestCase(0, null)]
        [TestCase(429, "{\"message\":\"Too Many Attempts.\"}")]
        [TestCase(201, "not json")]
        public void FailedRegistration_GivesNoToken(long statusCode, string body)
        {
            client.Responses.Enqueue(new BackendResponse(statusCode, body));

            Assert.IsNull(RequestToken());
            Assert.IsNull(store.Token);
        }

        [Test]
        public void Invalidate_ForgetsStoredToken()
        {
            store.Token = "1|revoked";

            auth.Invalidate();

            Assert.IsNull(store.Token);
        }
    }
}
