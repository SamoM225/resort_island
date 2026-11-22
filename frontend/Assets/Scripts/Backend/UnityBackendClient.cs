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
            => Send(UnityWebRequest.kHttpVerbPOST, path, json, token, done);

        public void PutJson(string path, string json, string token, Action<BackendResponse> done)
            => Send(UnityWebRequest.kHttpVerbPUT, path, json, token, done);

        public void GetJson(string path, string token, Action<BackendResponse> done)
            => Send(UnityWebRequest.kHttpVerbGET, path, null, token, done);

        private static void Send(string method, string path, string json, string token, Action<BackendResponse> done)
        {
            if (!TryGetUrl(path, out string url))
            {
                done(new BackendResponse(0, null));
                return;
            }

            UnityWebRequest request = new UnityWebRequest(url, method)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = TimeoutSeconds
            };
            if (json != null)
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                request.SetRequestHeader("Content-Type", "application/json");
            }
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
