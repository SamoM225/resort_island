using System;
using System.Collections.Generic;
using UnityEngine;
using ResortIsland.Config;
using ResortIsland.Resort;

namespace ResortIsland.Save
{
    /// <summary>
    /// Everything that is written to the save file. Field names are the JSON keys,
    /// so renaming or removing a field requires a migration (see SaveMigrations).
    /// Adding a new field is safe: older saves get the value from CreateNew.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>Bump this together with adding a migration to SaveMigrations.</summary>
        public const int CurrentVersion = 2;

        public int saveVersion = CurrentVersion;
        public int configVersion;
        public string savedAtUtc;
        public EconomySaveData economy = new EconomySaveData();
        /// <summary>The island map: placed buildings and unlocks. Saves from before it was added load with an empty map.</summary>
        public ResortSave resort = new ResortSave();
        /// <summary>Wave progress: which wave comes next and where in the wave loop the game was (v2).</summary>
        public WaveSaveData wave = new WaveSaveData();
        /// <summary>The building layout of SAVE LAYOUT / LOAD LAYOUT in the build menu; null = none saved (v2).</summary>
        public BuildingLayoutSave buildingLayout;
        /// <summary>
        /// True for a game that started from scratch until the backend said whether this player has a backed up save.
        /// While true the save is not uploaded, so a fresh game never overwrites progress that could be restored.
        /// </summary>
        public bool backupCheckPending;

        public static SaveData CreateNew(PlayerEconomyConfig config)
        {
            return new SaveData
            {
                configVersion = config.ConfigVersion,
                backupCheckPending = true,
                economy = new EconomySaveData
                {
                    coins = config.StartingCoins,
                    gems = config.StartingGems,
                    energy = config.StartingEnergy,
                    resortValue = config.StartingResortValue,
                    reputation = config.StartingReputation
                }
            };
        }

        /// <summary>Clamps values a broken or hand-edited save could put out of range.</summary>
        public void Sanitize(PlayerEconomyConfig config)
        {
            if (economy == null)
                economy = CreateNew(config).economy;
            if (resort == null)
                resort = new ResortSave();
            if (resort.buildings == null)
                resort.buildings = new List<BuildingSave>();
            if (resort.unlocks == null)
                resort.unlocks = new List<string>();
            if (wave == null)
                wave = new WaveSaveData();
            wave.nextWave = Mathf.Max(1, wave.nextWave);
            if (string.IsNullOrEmpty(wave.state))
                wave.state = WaveSaveData.AwaitingWave;

            economy.coins = Mathf.Max(0, economy.coins);
            economy.gems = Mathf.Max(0, economy.gems);
            economy.energy = Mathf.Clamp(economy.energy, 0, config.MaxEnergy);
            economy.resortValue = Mathf.Max(0, economy.resortValue);
            economy.reputation = Mathf.Clamp(economy.reputation, 0, 5);
        }
    }

    [Serializable]
    public class WaveSaveData
    {
        public const string AwaitingWave = "AwaitingWave";

        /// <summary>The wave that is played next, or the one being played / waiting for Claim.</summary>
        public int nextWave = 1;
        /// <summary>Name of the GameState the game was in (ResortIsland.Waves.GameState).</summary>
        public string state = AwaitingWave;
        /// <summary>Waits of a finished wave that was not claimed yet (need id -> seconds), so Wave Complete can show it again.</summary>
        public Dictionary<string, List<double>> unclaimedWaits;
    }

    [Serializable]
    public class EconomySaveData
    {
        public int coins;
        public int gems;
        public int energy;
        public int resortValue;
        public int reputation;
    }
}
