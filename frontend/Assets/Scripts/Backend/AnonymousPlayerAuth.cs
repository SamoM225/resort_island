using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ResortIsland.Backend
{
    public class AnonymousPlayerAuth : IPlayerAuth
    {
        public const string Endpoint = "/api/v1/auth/anonymous";

        private readonly IBackendClient client;
        private readonly IPlayerTokenStore store;

        public AnonymousPlayerAuth(IBackendClient client, IPlayerTokenStore store)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public void RequestToken(Action<string> done)
        {
            string stored = store.Load();
            if (stored != null)
            {
                done(stored);
                return;
            }

            client.PostJson(Endpoint, "{}", null, response =>
            {
                string token = response.IsSuccess ? ParseToken(response.Body) : null;
                if (token != null)
                    store.Save(token);
                else
                    Debug.LogWarning($"[Auth] Anonymous registration failed ({response.StatusCode}).");

                done(token);
            });
        }

        public void Invalidate() => store.Clear();

        private static string ParseToken(string body)
        {
            try
            {
                return JObject.Parse(body).Value<string>("token");
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
