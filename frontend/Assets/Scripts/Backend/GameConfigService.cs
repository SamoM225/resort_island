using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using ResortIsland.Config;

namespace ResortIsland.Backend
{
    public class GameConfigService : MonoBehaviour
    {
        public const string Endpoint = "/api/v1/configs";
        private const int TimeoutSeconds = 10;

        private static GameConfigService instance;
        private GameConfigProvider provider;

        public static GameConfigs Current => instance != null ? instance.provider.Current : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateInstance()
        {
            GameObject go = new GameObject("Game Config Service");
            DontDestroyOnLoad(go);
            go.AddComponent<GameConfigService>();
        }

        private void Awake()
        {
            instance = this;
            TextAsset bundled = Resources.Load<TextAsset>(GameConfigProvider.BundledResourcePath);
            provider = new GameConfigProvider(GameConfigCache.CreateDefault(), bundled.text);
            Debug.Log($"[GameConfigs] Using {provider.Source} configs.");
            ApplyFrameRate(provider.Current);
        }

        /// <summary>graphics.target_frame_rate; also after a download, so a change in the admin panel applies at once.</summary>
        private static void ApplyFrameRate(GameConfigs configs)
        {
            int frameRate = configs?.Graphics?.TargetFrameRate ?? 0;
            if (frameRate <= 0)
            {
                Debug.LogError($"[GameConfigs] graphics.target_frame_rate must be above 0, got {frameRate}; the frame rate stays as it is.");
                return;
            }

            // With vSync on, Unity ignores targetFrameRate on desktop and in the Editor (mobile ignores vSync).
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = frameRate;
            Debug.Log($"[GameConfigs] Target frame rate {frameRate} fps.");
        }

        private void Start()
        {
            if (provider.NeedsRefresh(DateTime.UtcNow))
                StartCoroutine(Download());
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private IEnumerator Download()
        {
            if (!TryGetUrl(out string url))
                yield break;

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = TimeoutSeconds;
                request.SetRequestHeader("Accept", "application/json");
                if (provider.ETag != null)
                    request.SetRequestHeader("If-None-Match", provider.ETag);

                yield return request.SendWebRequest();

                string cacheControl = request.GetResponseHeader("Cache-Control");
                if (request.responseCode == 304)
                {
                    provider.ApplyNotModified(cacheControl, DateTime.UtcNow);
                    Debug.Log("[GameConfigs] Cached configs are up to date.");
                }
                else if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[GameConfigs] Download failed ({request.error}), keeping {provider.Source} configs.");
                }
                else if (provider.ApplyDownloaded(request.downloadHandler.text, request.GetResponseHeader("ETag"), cacheControl, DateTime.UtcNow))
                {
                    Debug.Log("[GameConfigs] Downloaded configs from the backend.");
                    ApplyFrameRate(provider.Current);
                }
            }
        }

        private bool TryGetUrl(out string url)
        {
            try
            {
                url = BackendEnvironment.BaseUrl + Endpoint;
                return true;
            }
            catch (InvalidOperationException e)
            {
                Debug.LogWarning($"[GameConfigs] No backend configured ({e.Message}), keeping {provider.Source} configs.");
                url = null;
                return false;
            }
        }
    }
}
