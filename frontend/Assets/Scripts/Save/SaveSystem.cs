using System;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using ResortIsland.Config;

namespace ResortIsland.Save
{
    public enum LoadStatus
    {
        /// <summary>No save exists, the player starts with config defaults.</summary>
        NewGame,
        Loaded,
        /// <summary>Save came from an older version and was upgraded.</summary>
        Migrated,
        /// <summary>Main save was unreadable, the previous save (.bak) was used.</summary>
        RestoredFromBackup,
        /// <summary>Nothing readable, the unreadable files were kept aside and a new game started.</summary>
        Corrupted,
        /// <summary>Save is from a newer game version. It is never overwritten.</summary>
        NewerVersion
    }

    public class LoadResult
    {
        public LoadStatus Status { get; }
        public SaveData Data { get; }
        public int OriginalVersion { get; }
        public string Message { get; }

        public bool CanSave => Status != LoadStatus.NewerVersion;

        public LoadResult(LoadStatus status, SaveData data, int originalVersion, string message)
        {
            Status = status;
            Data = data;
            OriginalVersion = originalVersion;
            Message = message;
        }
    }

    /// <summary>
    /// Reads and writes the save file. Writes go to a temp file first and then replace
    /// the save, keeping the previous one as .bak, so a crash mid-write never loses progress.
    /// </summary>
    public class SaveSystem
    {
        public const string FileName = "save.json";

        private readonly string directory;
        private readonly SaveMigrator migrator;

        public string SavePath => Path.Combine(directory, FileName);
        public string BackupPath => SavePath + ".bak";
        private string TempPath => SavePath + ".tmp";

        public SaveSystem(string directory, SaveMigrator migrator)
        {
            this.directory = directory;
            this.migrator = migrator;
        }

        public static SaveSystem CreateDefault() => new SaveSystem(Application.persistentDataPath, SaveMigrations.CreateDefault());

        public LoadResult Load(PlayerEconomyConfig config)
        {
            if (!File.Exists(SavePath) && !File.Exists(BackupPath))
                return new LoadResult(LoadStatus.NewGame, SaveData.CreateNew(config), 0, "No save found.");

            if (TryRead(SavePath, config, out LoadResult result, out string error))
                return result;

            if (File.Exists(BackupPath) && TryRead(BackupPath, config, out LoadResult backup, out string backupError))
            {
                if (backup.Status == LoadStatus.NewerVersion)
                    return backup;
                // Move the broken file away, otherwise the next save would rotate it into .bak.
                KeepCorruptedFiles(includeBackup: false);
                return new LoadResult(LoadStatus.RestoredFromBackup, backup.Data, backup.OriginalVersion,
                    $"Save was unreadable ({error}), restored the previous save.");
            }

            KeepCorruptedFiles(includeBackup: true);
            return new LoadResult(LoadStatus.Corrupted, SaveData.CreateNew(config), 0,
                $"Save was unreadable ({error}), started a new game. The broken file was kept next to the save.");
        }

        public void Save(SaveData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            data.saveVersion = migrator.CurrentVersion;
            data.savedAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);

            Directory.CreateDirectory(directory);
            using (FileStream stream = new FileStream(TempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] bytes = new UTF8Encoding(false).GetBytes(json);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }

            if (!File.Exists(SavePath))
            {
                File.Move(TempPath, SavePath);
                return;
            }

            try
            {
                File.Replace(TempPath, SavePath, BackupPath);
            }
            catch (Exception e) when (e is IOException || e is PlatformNotSupportedException)
            {
                // Some filesystems do not support Replace. Same result, just not in one step.
                File.Copy(SavePath, BackupPath, true);
                File.Delete(SavePath);
                File.Move(TempPath, SavePath);
            }
        }

        private bool TryRead(string path, PlayerEconomyConfig config, out LoadResult result, out string error)
        {
            result = null;
            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                error = e.Message;
                return false;
            }

            JObject root;
            try
            {
                root = JObject.Parse(json);
            }
            catch (JsonException e)
            {
                error = "invalid JSON: " + e.Message;
                return false;
            }

            JToken versionToken = root["saveVersion"];
            if (versionToken == null || versionToken.Type != JTokenType.Integer || versionToken.Value<int>() < 1)
            {
                error = "missing or invalid saveVersion";
                return false;
            }

            int version = versionToken.Value<int>();
            if (version > migrator.CurrentVersion)
            {
                error = null;
                result = new LoadResult(LoadStatus.NewerVersion, SaveData.CreateNew(config), version,
                    $"Save is from a newer game version (v{version}, this build supports v{migrator.CurrentVersion}). Update the game; the save will not be overwritten.");
                return true;
            }

            LoadStatus status = LoadStatus.Loaded;
            if (version < migrator.CurrentVersion)
            {
                try
                {
                    File.Copy(path, Path.Combine(directory, $"save.v{version}.bak"), true);
                    migrator.Migrate(root, version);
                }
                catch (Exception e)
                {
                    error = $"migration from v{version} failed: {e.Message}";
                    return false;
                }
                status = LoadStatus.Migrated;
            }

            SaveData data = SaveData.CreateNew(config);
            int savedConfigVersion;
            try
            {
                using (JsonReader reader = root.CreateReader())
                    JsonSerializer.CreateDefault().Populate(reader, data);
                savedConfigVersion = data.configVersion;
            }
            catch (JsonException e)
            {
                error = "unexpected data: " + e.Message;
                return false;
            }

            data.saveVersion = migrator.CurrentVersion;
            data.configVersion = config.ConfigVersion;
            data.Sanitize(config);

            string message = status == LoadStatus.Migrated
                ? $"Save migrated v{version} -> v{migrator.CurrentVersion}."
                : $"Save v{version} loaded.";
            if (savedConfigVersion != config.ConfigVersion)
                message += $" Config v{savedConfigVersion} -> v{config.ConfigVersion}.";

            error = null;
            result = new LoadResult(status, data, version, message);
            return true;
        }

        private void KeepCorruptedFiles(bool includeBackup)
        {
            string prefix = Path.Combine(directory, "save.corrupt-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture));
            if (File.Exists(SavePath))
                File.Move(SavePath, prefix + ".json");
            if (includeBackup && File.Exists(BackupPath))
                File.Move(BackupPath, prefix + ".json.bak");
        }
    }
}
