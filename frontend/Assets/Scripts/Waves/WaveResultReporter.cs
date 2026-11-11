using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using ResortIsland.Backend;

namespace ResortIsland.Waves
{
    /// <summary>
    /// Sends finished waves to the backend (POST /api/v1/me/waves), which rates them again from the same waits
    /// and keeps the result to check the coins in the player's save. The game never waits for it:
    /// reports are queued and the ones that could not be sent go with the next report or flush.
    /// </summary>
    public class WaveResultReporter
    {
        public const string Endpoint = "/api/v1/me/waves";

        private readonly IBackendClient client;
        private readonly IPlayerAuth auth;
        private readonly Queue<string> queue = new Queue<string>();

        public bool IsSending { get; private set; }
        public int Pending => queue.Count;

        public WaveResultReporter(IBackendClient client, IPlayerAuth auth)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.auth = auth ?? throw new ArgumentNullException(nameof(auth));
        }

        public void Report(WaveScore score)
        {
            if (score == null)
                throw new ArgumentNullException(nameof(score));

            queue.Enqueue(JsonConvert.SerializeObject(new
            {
                wave = score.Wave,
                waits = score.Waits,
                client = new { satisfaction = score.SatisfactionRatio, rating = score.Rating, coins = score.Coins }
            }));
            Flush();
        }

        public void Flush()
        {
            if (IsSending || queue.Count == 0)
                return;

            IsSending = true;
            auth.RequestToken(token =>
            {
                if (token == null)
                {
                    IsSending = false;
                    return;
                }

                client.PostJson(Endpoint, queue.Peek(), token, response =>
                {
                    IsSending = false;
                    if (Handle(response))
                    {
                        queue.Dequeue();
                        Flush();
                    }
                });
            });
        }

        /// <returns>True when the report is done with (accepted or rejected for good), false to retry later.</returns>
        private bool Handle(BackendResponse response)
        {
            if (response.IsSuccess)
                return true;

            if (response.StatusCode == 401)
            {
                auth.Invalidate();
                return false;
            }

            if (response.StatusCode >= 400 && response.StatusCode < 500 && response.StatusCode != 408 && response.StatusCode != 429)
            {
                Debug.LogWarning($"[WaveResults] Backend rejected a wave result ({response.StatusCode}).");
                return true;
            }

            return false;
        }
    }
}
