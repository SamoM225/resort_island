using UnityEngine;

namespace ResortIsland.Config
{
    [CreateAssetMenu(fileName = "PlayerEconomyConfig", menuName = "ResortIsland/Config/PlayerEconomyConfig")]
    public class PlayerEconomyConfig : ScriptableObject
    {
        [Header("Versioning")]
        [Tooltip("Bump when you change values here. Saves remember which config version they were created with.")]
        [SerializeField, Min(1)] private int configVersion = 1;

        [Header("Starting Capital")]
        [SerializeField] private int startingCoins = 1000;
        [SerializeField] private int startingGems = 10;

        [Header("Resort Status")]
        [SerializeField] private int startingEnergy = 82;
        [SerializeField] private int maxEnergy = 100;
        [SerializeField] private int startingResortValue = 24500;
        [SerializeField, Range(0, 5)] private int startingReputation = 4;

        public int ConfigVersion => configVersion;
        public int StartingCoins => startingCoins;
        public int StartingGems => startingGems;
        public int StartingEnergy => startingEnergy;
        public int MaxEnergy => maxEnergy;
        public int StartingResortValue => startingResortValue;
        public int StartingReputation => startingReputation;
    }
}
