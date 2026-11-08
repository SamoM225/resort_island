using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace ResortIsland.Config
{
    public class GameConfigCache
    {
        public const string FileName = "configs.json";

        private readonly string directory;

        public string CachePath => Path.Combine(directory, FileName);
        private string TempPath => CachePath + ".tmp";

        public GameConfigCache(string directory)
        {
            this.directory = directory;
        }

        public static GameConfigCache CreateDefault() => new GameConfigCache(Application.persistentDataPath);

        public CachedConfigs Load()
        {
            if (!File.Exists(CachePath))
                return null;

            try
            {
                return JsonConvert.DeserializeObject<CachedConfigs>(File.ReadAllText(CachePath));
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is JsonException)
            {
                Debug.LogWarning($"[GameConfigs] Cached configs are unreadable: {e.Message}");
                return null;
            }
        }

        public void Save(CachedConfigs entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            Directory.CreateDirectory(directory);
            File.WriteAllText(TempPath, JsonConvert.SerializeObject(entry, Formatting.Indented), new UTF8Encoding(false));

            if (File.Exists(CachePath))
                File.Delete(CachePath);
            File.Move(TempPath, CachePath);
        }
    }

    [Serializable]
    public class CachedConfigs
    {
        public string etag;
        public DateTime fetchedAtUtc;
        public int maxAgeSeconds;
        public string response;

        public bool IsFresh(DateTime nowUtc) => nowUtc < fetchedAtUtc.AddSeconds(maxAgeSeconds);
    }
}
