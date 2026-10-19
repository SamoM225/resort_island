using UnityEngine;

namespace ResortIsland.Config
{
    [CreateAssetMenu(fileName = "PlayerEconomyConfig", menuName = "ResortIsland/Config/PlayerEconomyConfig")]
    public class PlayerEconomyConfig : ScriptableObject
    {
        private const int StartingCoinsValue = 1000;
        private const int StartingGemsValue = 10;
        private const int StartingEnergyValue = 82;
        private const int MaxEnergyValue = 100;
        private const int StartingResortValueValue = 24500;
        private const int StartingReputationValue = 4;

        public int StartingCoins => StartingCoinsValue;
        public int StartingGems => StartingGemsValue;
        public int StartingEnergy => StartingEnergyValue;
        public int MaxEnergy => MaxEnergyValue;
        public int StartingResortValue => StartingResortValueValue;
        public int StartingReputation => StartingReputationValue;
    }
}
