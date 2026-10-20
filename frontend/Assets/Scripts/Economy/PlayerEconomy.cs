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
        public int Energy { get; private set; }
        public int MaxEnergy { get; private set; }
        public int ResortValue { get; private set; }
        public int Reputation { get; private set; }

        public event Action<int> OnCoinsChanged;
        public event Action<int> OnGemsChanged;
        public event Action<int> OnEnergyChanged;
        public event Action<int> OnResortValueChanged;
        public event Action<int> OnReputationChanged;

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
            Energy = config.StartingEnergy;
            MaxEnergy = config.MaxEnergy;
            ResortValue = config.StartingResortValue;
            Reputation = config.StartingReputation;

            OnCoinsChanged?.Invoke(Coins);
            OnGemsChanged?.Invoke(Gems);
            OnEnergyChanged?.Invoke(Energy);
            OnResortValueChanged?.Invoke(ResortValue);
            OnReputationChanged?.Invoke(Reputation);

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

        public bool TrySpendEnergy(int amount)
        {
            if (amount <= 0 || Energy < amount) return false;
            Energy -= amount;
            OnEnergyChanged?.Invoke(Energy);
            return true;
        }

        public void RestoreEnergy(int amount)
        {
            if (amount <= 0) return;
            Energy = Mathf.Min(Energy + amount, MaxEnergy);
            OnEnergyChanged?.Invoke(Energy);
        }

        public void AddResortValue(int amount)
        {
            if (amount <= 0) return;
            ResortValue += amount;
            OnResortValueChanged?.Invoke(ResortValue);
        }

        public void SetReputation(int value)
        {
            int nextValue = Mathf.Clamp(value, 0, 5);
            if (nextValue == Reputation) return;
            Reputation = nextValue;
            OnReputationChanged?.Invoke(Reputation);
        }
    }
}
