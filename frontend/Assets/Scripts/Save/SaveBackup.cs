using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using ResortIsland.Analytics;
using ResortIsland.Backend;

namespace ResortIsland.Save
{
    /// <summary>
    /// Backs the local save up on the backend (PUT /api/v1/me/save) and fetches it back for a new install.
    /// The game never waits for it: uploads are throttled and a failed one is retried with the next save.
    /// </summary>
    public class SaveBackup
    {
        public const string Endpoint = "/api/v1/me/save";
        public static readonly TimeSpan MinUploadInterval = TimeSpan.FromSeconds(30);

        private readonly IBackendClient client;
        private readonly IPlayerAuth auth;
        private readonly IClock clock;

        private string pending;
        private DateTime lastUploadAt = DateTime.MinValue;

        public bool IsUploading { get; private set; }
        public bool HasPending => pending != null;

        public SaveBackup(IBackendClient client, IPlayerAuth auth, IClock clock)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.auth = auth ?? throw new ArgumentNullException(nameof(auth));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public static SaveBackup CreateDefault()
        {
            IBackendClient client = new UnityBackendClient();
            return new SaveBackup(client, new AnonymousPlayerAuth(client, new PlayerPrefsTokenStore()), new SystemClock());
        }

        /// <summary>Queues the save for upload; sends it now if the last upload is old enough or <paramref name="now"/> is set.</summary>
        public void Upload(SaveData data, bool now = false)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            pending = JsonConvert.SerializeObject(new { save_version = data.saveVersion, saved_at = data.savedAtUtc, save = data });
            Flush(now);
        }

        /// <summary>Sends a queued save, e.g. when the app goes to the background.</summary>
        public void Flush(bool now = true)
        {
            if (pending == null || IsUploading)
                return;
            if (!now && clock.UtcNow - lastUploadAt < MinUploadInterval)
                return;

            string json = pending;
            pending = null;
            IsUploading = true;
            lastUploadAt = clock.UtcNow;

            auth.RequestToken(token =>
            {
                if (token == null)
                {
                    Retry(json);
                    return;
                }

                client.PutJson(Endpoint, json, token, response =>
                {
                    IsUploading = false;
                    Handle(response, json);
                });
            });
        }

        /// <summary>
        /// Looks up the backed up save. <c>done(answered, json)</c>: answered is false when the backend could not be asked
        /// (offline, no token, server error); json is the save in the local save format, or null when there is no backup.
        /// </summary>
        public void Download(Action<bool, string> done)
        {
            auth.RequestToken(token =>
            {
                if (token == null)
                {
                    done(false, null);
                    return;
                }

                client.GetJson(Endpoint, token, response =>
                {
                    if (response.StatusCode == 401)
                        auth.Invalidate();

                    if (response.StatusCode == 404)
                    {
                        done(true, null);
                        return;
                    }

                    string save = response.IsSuccess ? ExtractSave(response.Body) : null;
                    done(save != null, save);
                });
            });
        }

        private void Handle(BackendResponse response, string json)
        {
            if (response.IsSuccess)
                return;

            if (response.StatusCode == 401)
            {
                auth.Invalidate();
                Retry(json);
            }
            else if (response.StatusCode == 409)
            {
                Debug.LogWarning("[SaveBackup] Another device backed up newer progress; this device's save stays local.");
            }
            else if (response.StatusCode >= 400 && response.StatusCode < 500 && response.StatusCode != 408 && response.StatusCode != 429)
            {
                Debug.LogWarning($"[SaveBackup] Backend rejected the save ({response.StatusCode}).");
            }
            else
            {
                Retry(json);
            }
        }

        private void Retry(string json)
        {
            IsUploading = false;
            // A newer save queued meanwhile wins over the failed one.
            if (pending == null)
                pending = json;
        }

        private static string ExtractSave(string body)
        {
            try
            {
                JToken save = JObject.Parse(body)["data"]?["save"];
                return save != null && save.Type == JTokenType.Object ? save.ToString(Formatting.Indented) : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
