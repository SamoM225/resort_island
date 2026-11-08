using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ResortIsland.Config
{
    public enum GameConfigSource
    {
        Bundled,
        Cache,
        Backend
    }

    public class GameConfigProvider
    {
        public const string BundledResourcePath = "GameConfigs";

        private static readonly Regex MaxAge = new Regex(@"max-age=(\d+)", RegexOptions.IgnoreCase);

        private readonly GameConfigCache cache;
        private CachedConfigs cached;

        public GameConfigs Current { get; private set; }
        public GameConfigSource Source { get; private set; }
        public string ETag => cached?.etag;

        public GameConfigProvider(GameConfigCache cache, string bundledResponse)
        {
            this.cache = cache;

            cached = cache.Load();
            if (cached != null && TryParse(cached.response, out GameConfigs fromCache))
            {
                Current = fromCache;
                Source = GameConfigSource.Cache;
                return;
            }

            cached = null;
            Current = GameConfigs.Parse(bundledResponse);
            Source = GameConfigSource.Bundled;
        }

        public bool NeedsRefresh(DateTime nowUtc) => cached == null || !cached.IsFresh(nowUtc);

        public bool ApplyDownloaded(string response, string etag, string cacheControl, DateTime nowUtc)
        {
            if (!TryParse(response, out GameConfigs configs))
                return false;

            cached = new CachedConfigs { etag = etag, fetchedAtUtc = nowUtc, maxAgeSeconds = ParseMaxAge(cacheControl), response = response };
            Current = configs;
            Source = GameConfigSource.Backend;
            TrySaveCache();
            return true;
        }

        public void ApplyNotModified(string cacheControl, DateTime nowUtc)
        {
            if (cached == null)
                return;

            cached.fetchedAtUtc = nowUtc;
            cached.maxAgeSeconds = ParseMaxAge(cacheControl);
            TrySaveCache();
        }

        public static int ParseMaxAge(string cacheControl)
        {
            if (string.IsNullOrEmpty(cacheControl))
                return 0;

            Match match = MaxAge.Match(cacheControl);
            return match.Success && int.TryParse(match.Groups[1].Value, out int seconds) ? seconds : 0;
        }

        private static bool TryParse(string response, out GameConfigs configs)
        {
            try
            {
                configs = GameConfigs.Parse(response);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameConfigs] Invalid configs: {e.Message}");
                configs = null;
                return false;
            }
        }

        private void TrySaveCache()
        {
            try
            {
                cache.Save(cached);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[GameConfigs] Saving the config cache failed: {e.Message}");
            }
        }
    }
}
