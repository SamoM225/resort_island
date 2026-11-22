using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using ResortIsland.Config;

namespace ResortIsland.Save
{
    /// <summary>
    /// The one save of the game, shared by everything that persists player data: PlayerEconomy writes
    /// the economy, ResortManager the map. Both change the same SaveData and write it as one file,
    /// so neither overwrites the other. After writing, the save is backed up on the backend.
    /// </summary>
    public class SaveStore
    {
        private static SaveStore shared;

        private readonly SaveSystem saveSystem;
        private readonly SaveBackup backup;
        private readonly Action onRestored;
        private bool retired;
        private bool checkingBackup;

        public static SaveStore Shared => shared ?? (shared = new SaveStore(SaveSystem.CreateDefault(), SaveBackup.CreateDefault(), ReloadScene));

        public SaveData Data { get; private set; }
        public LoadResult LastLoad { get; private set; }
        public bool IsLoaded => Data != null;
        public bool CanSave => LastLoad != null && LastLoad.CanSave && !retired;

        public SaveStore(SaveSystem saveSystem, SaveBackup backup, Action onRestored)
        {
            this.saveSystem = saveSystem ?? throw new ArgumentNullException(nameof(saveSystem));
            this.backup = backup;
            this.onRestored = onRestored;
        }

        /// <summary>Replaces the shared store, e.g. with one without a backend backup in tests. Null starts over on next use.</summary>
        public static void ResetShared(SaveStore replacement = null) => shared = replacement;

        /// <summary>
        /// Loads the save once; later calls return the same result. A game that started from scratch (new install,
        /// unreadable save) asks the backend for a backup; if there is one, it replaces the new game and the scene reloads.
        /// </summary>
        public LoadResult Load(PlayerEconomyConfig config)
        {
            if (LastLoad != null)
                return LastLoad;

            LastLoad = saveSystem.Load(config);
            Data = LastLoad.Data;

            if (backup != null && Data.backupCheckPending && LastLoad.CanSave)
            {
                // Until the backend answers, the new game is not uploaded: it would overwrite the progress being restored.
                checkingBackup = true;
                backup.Download(OnBackupChecked);
            }

            return LastLoad;
        }

        /// <summary>Writes the save to disk and queues the backup. Throws when writing fails, so the caller can retry.</summary>
        public void Save(bool uploadNow = false)
        {
            if (!IsLoaded || !CanSave)
                return;

            saveSystem.Save(Data);
            if (!checkingBackup)
                backup?.Upload(Data, uploadNow);
        }

        /// <summary>Sends a backup that is still queued, e.g. when the app goes to the background.</summary>
        public void FlushBackup()
        {
            if (!checkingBackup)
                backup?.Flush();
        }

        private void OnBackupChecked(bool answered, string json)
        {
            // No answer (offline): keep holding uploads; the next launch asks again.
            if (!answered || retired)
                return;

            checkingBackup = false;
            if (json != null)
            {
                RestoreFromBackup(json);
                return;
            }

            // Nothing to restore: a new player. From now on the save is backed up.
            Data.backupCheckPending = false;
            try
            {
                Save();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveStore] Saving failed: {e.Message}");
            }
        }

        private void RestoreFromBackup(string json)
        {
            try
            {
                saveSystem.Import(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveStore] Restoring the backed up save failed: {e.Message}");
                return;
            }

            // This store holds the new game; it must not write it over the restored save.
            retired = true;
            if (shared == this)
                shared = null;

            Debug.Log("[SaveStore] Restored progress from the backend backup.");
            onRestored?.Invoke();
        }

        private static void ReloadScene() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
