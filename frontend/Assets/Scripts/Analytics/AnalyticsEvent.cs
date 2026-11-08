using System.Collections.Generic;
using Newtonsoft.Json;

namespace ResortIsland.Analytics
{
    public class AnalyticsEvent
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("occurred_at")] public string OccurredAt { get; set; }
        [JsonProperty("session_id")] public string SessionId { get; set; }
        [JsonProperty("payload")] public Dictionary<string, object> Payload { get; set; }
    }
}
