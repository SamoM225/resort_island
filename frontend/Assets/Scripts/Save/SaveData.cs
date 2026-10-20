using System;
using UnityEngine;
using ResortIsland.Config;

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
        public const int CurrentVersion = 1;

        public int saveVersion = CurrentVersion;
        public int configVersion;
        public string savedAtUtc;
        public EconomySaveData economy = new EconomySaveData();

        public static SaveData CreateNew(PlayerEconomyConfig config)
        {
            return new SaveData
            {
                configVersion = config.ConfigVersion,
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

            economy.coins = Mathf.Max(0, economy.coins);
            economy.gems = Mathf.Max(0, economy.gems);
            economy.energy = Mathf.Clamp(economy.energy, 0, config.MaxEnergy);
            economy.resortValue = Mathf.Max(0, economy.resortValue);
            economy.reputation = Mathf.Clamp(economy.reputation, 0, 5);
        }
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
