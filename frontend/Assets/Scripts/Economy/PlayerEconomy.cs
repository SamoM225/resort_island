using System;
using UnityEngine;
using ResortIsland.Config;

namespace ResortIsland.Economy
{
    public class PlayerEconomy : MonoBehaviour
    {
        [SerializeField] private PlayerEconomyConfig config;

        public int Coins { get; private set; }
        public int Gems { get; private set; }

        public event Action<int> OnCoinsChanged;
        public event Action<int> OnGemsChanged;

        private void Awake()
        {
            InitializeNewPlayer();
        }

        public void InitializeNewPlayer()
        {
            if (config == null)
            {
                Debug.LogError("[PlayerEconomy] Config is not assigned! Please assign a PlayerEconomyConfig asset in the inspector.");
                return;
            }

            Coins = config.StartingCoins;
            Gems = config.StartingGems;

            OnCoinsChanged?.Invoke(Coins);
            OnGemsChanged?.Invoke(Gems);

            Debug.Log($"[PlayerEconomy] New player initialized with {Coins} Coins and {Gems} Gems (from {config.name}).");
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            Coins += amount;
            OnCoinsChanged?.Invoke(Coins);
        }

        public bool TrySpendCoins(int amount)
        {
            if (amount <= 0 || Coins < amount) return false;
            Coins -= amount;
            OnCoinsChanged?.Invoke(Coins);
            return true;
        }

        public void AddGems(int amount)
        {
            if (amount <= 0) return;
            Gems += amount;
            OnGemsChanged?.Invoke(Gems);
        }

        public bool TrySpendGems(int amount)
        {
            if (amount <= 0 || Gems < amount) return false;
            Gems -= amount;
            OnGemsChanged?.Invoke(Gems);
            return true;
        }
    }
}
