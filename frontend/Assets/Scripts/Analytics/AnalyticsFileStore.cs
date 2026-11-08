using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace ResortIsland.Analytics
{
    public class AnalyticsFileStore : IAnalyticsStore
    {
        public const string FileName = "analytics_queue.json";

        private readonly string directory;

        public string FilePath => Path.Combine(directory, FileName);
        private string TempPath => FilePath + ".tmp";

        public AnalyticsFileStore(string directory)
        {
            this.directory = directory;
        }

        public static AnalyticsFileStore CreateDefault() => new AnalyticsFileStore(Application.persistentDataPath);

        public List<AnalyticsEvent> Load()
        {
            if (!File.Exists(FilePath))
                return new List<AnalyticsEvent>();

            try
            {
                return JsonConvert.DeserializeObject<List<AnalyticsEvent>>(File.ReadAllText(FilePath)) ?? new List<AnalyticsEvent>();
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is JsonException)
            {
                Debug.LogWarning($"[Analytics] Pending events are unreadable: {e.Message}");
                return new List<AnalyticsEvent>();
            }
        }

        public void Save(IReadOnlyList<AnalyticsEvent> events)
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(TempPath, JsonConvert.SerializeObject(events), new UTF8Encoding(false));

            if (File.Exists(FilePath))
                File.Delete(FilePath);
            File.Move(TempPath, FilePath);
        }
    }
}
