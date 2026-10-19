using UnityEngine;

namespace ResortIsland.Config
{
    [CreateAssetMenu(fileName = "PlayerEconomyConfig", menuName = "ResortIsland/Config/PlayerEconomyConfig")]
    public class PlayerEconomyConfig : ScriptableObject
    {
        [Header("Starting Capital")]
        [SerializeField] private int startingCoins = 100;
        [SerializeField] private int startingGems = 10;

        public int StartingCoins => startingCoins;
        public int StartingGems => startingGems;
    }
}
