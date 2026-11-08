using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using ResortIsland.Backend;

namespace ResortIsland.Analytics
{
    public class AnalyticsDispatcher
    {
        public const string Endpoint = "/api/v1/analytics/events";
        public const int MaxBatchSize = 100;

        private readonly AnalyticsQueue queue;
        private readonly IPlayerAuth auth;
        private readonly IBackendClient client;
        private readonly string appVersion;
        private readonly string platform;

        public bool IsSending { get; private set; }

        public AnalyticsDispatcher(AnalyticsQueue queue, IPlayerAuth auth, IBackendClient client, string appVersion, string platform)
        {
            this.queue = queue ?? throw new ArgumentNullException(nameof(queue));
            this.auth = auth ?? throw new ArgumentNullException(nameof(auth));
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.appVersion = appVersion;
            this.platform = platform;
        }

        public void Flush()
        {
            if (IsSending || queue.Count == 0)
                return;

            IsSending = true;
            auth.RequestToken(token =>
            {
                if (token == null)
                    IsSending = false;
                else
                    Send(token);
            });
        }

        private void Send(string token)
        {
            IReadOnlyList<AnalyticsEvent> batch = queue.Peek(MaxBatchSize);
            string json = JsonConvert.SerializeObject(new { app_version = appVersion, platform, events = batch });

            client.PostJson(Endpoint, json, token, response =>
            {
                Handle(response, batch.Count);
                IsSending = false;
            });
        }

        private void Handle(BackendResponse response, int sent)
        {
            if (response.StatusCode == 401)
            {
                auth.Invalidate();
                return;
            }

            if (response.IsSuccess)
            {
                queue.Remove(sent);
            }
            else if (IsRejected(response.StatusCode))
            {
                Debug.LogWarning($"[Analytics] Backend rejected {sent} events ({response.StatusCode}).");
                queue.Remove(sent);
            }
        }

        private static bool IsRejected(long statusCode) => statusCode >= 400 && statusCode < 500 && statusCode != 408 && statusCode != 429;
    }
}
