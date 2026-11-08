using System;
using System.Collections.Generic;
using ResortIsland.Analytics;
using ResortIsland.Backend;

namespace ResortIsland.Tests
{
    internal class FakeClock : IClock
    {
        public DateTime UtcNow { get; set; } = new DateTime(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc);
    }

    internal class FakeBackendClient : IBackendClient
    {
        public readonly List<(string Path, string Json, string Token)> Requests = new List<(string, string, string)>();
        public readonly Queue<BackendResponse> Responses = new Queue<BackendResponse>();

        public void PostJson(string path, string json, string token, Action<BackendResponse> done)
        {
            Requests.Add((path, json, token));
            done(Responses.Count > 0 ? Responses.Dequeue() : new BackendResponse(0, null));
        }
    }

    internal class InMemoryTokenStore : IPlayerTokenStore
    {
        public string Token;

        public string Load() => Token;
        public void Save(string token) => Token = token;
        public void Clear() => Token = null;
    }

    internal class InMemoryAnalyticsStore : IAnalyticsStore
    {
        public List<AnalyticsEvent> Saved = new List<AnalyticsEvent>();

        public List<AnalyticsEvent> Load() => new List<AnalyticsEvent>(Saved);
        public void Save(IReadOnlyList<AnalyticsEvent> events) => Saved = new List<AnalyticsEvent>(events);
    }
}
