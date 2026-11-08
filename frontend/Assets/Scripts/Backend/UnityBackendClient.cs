using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ResortIsland.Backend
{
    public class UnityBackendClient : IBackendClient
    {
        private const int TimeoutSeconds = 10;

        public void PostJson(string path, string json, string token, Action<BackendResponse> done)
        {
            if (!TryGetUrl(path, out string url))
            {
                done(new BackendResponse(0, null));
                return;
            }

            UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = TimeoutSeconds
            };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");
            if (token != null)
                request.SetRequestHeader("Authorization", "Bearer " + token);

            request.SendWebRequest().completed += _ =>
            {
                BackendResponse response = new BackendResponse(request.responseCode, request.downloadHandler.text);
                request.Dispose();
                done(response);
            };
        }

        private static bool TryGetUrl(string path, out string url)
        {
            try
            {
                url = BackendEnvironment.BaseUrl + path;
                return true;
            }
            catch (InvalidOperationException e)
            {
                Debug.LogWarning($"[Backend] No backend configured ({e.Message}).");
                url = null;
                return false;
            }
        }
    }
}
