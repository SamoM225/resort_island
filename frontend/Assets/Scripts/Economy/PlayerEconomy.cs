using System;
using UnityEngine;
using ResortIsland.Config;
using ResortIsland.Save;

namespace ResortIsland.Economy
{
    public class PlayerEconomy : MonoBehaviour
    {
        [SerializeField] private PlayerEconomyConfig config;
        [Tooltip("Changes are written to disk at most this often. Pausing or quitting the app saves immediately.")]
        [SerializeField, Min(0f)] private float autosaveDelay = 1f;

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

        private SaveSystem saveSystem;
        private bool canSave;
        private bool isDirty;
        private float dirtySince;

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError("[PlayerEconomy] Config is not assigned! Please assign a PlayerEconomyConfig asset in the inspector.");
                return;
            }

            saveSystem = SaveSystem.CreateDefault();
            LoadGame();
        }

        private void LoadGame()
        {
            LoadResult result = saveSystem.Load(config);
            canSave = result.CanSave;
            ApplySaveData(result.Data);

            switch (result.Status)
            {
                case LoadStatus.Loaded:
                    Debug.Log($"[PlayerEconomy] {result.Message} {Coins} Coins, {Gems} Gems.");
                    break;
                case LoadStatus.NewGame:
                    Debug.Log($"[PlayerEconomy] New player initialized with {Coins} Coins and {Gems} Gems (from {config.name}).");
                    SaveNow();
                    break;
                case LoadStatus.Migrated:
                    Debug.Log($"[PlayerEconomy] {result.Message}");
                    SaveNow();
                    break;
                case LoadStatus.RestoredFromBackup:
                case LoadStatus.Corrupted:
                    Debug.LogWarning($"[PlayerEconomy] {result.Message}");
                    SaveNow();
                    break;
                case LoadStatus.NewerVersion:
                    Debug.LogError($"[PlayerEconomy] {result.Message}");
                    break;
            }
        }

        public void InitializeNewPlayer()
        {
            if (config == null)
            {
                Debug.LogError("[PlayerEconomy] Config is not assigned! Please assign a PlayerEconomyConfig asset in the inspector.");
                return;
            }

            ApplySaveData(SaveData.CreateNew(config));
            SaveNow();
            Debug.Log($"[PlayerEconomy] New player initialized with {Coins} Coins and {Gems} Gems (from {config.name}).");
        }

        private void ApplySaveData(SaveData data)
        {
            Coins = data.economy.coins;
            Gems = data.economy.gems;
            Energy = data.economy.energy;
            MaxEnergy = config.MaxEnergy;
            ResortValue = data.economy.resortValue;
            Reputation = data.economy.reputation;

            OnCoinsChanged?.Invoke(Coins);
            OnGemsChanged?.Invoke(Gems);
            OnEnergyChanged?.Invoke(Energy);
            OnResortValueChanged?.Invoke(ResortValue);
            OnReputationChanged?.Invoke(Reputation);
        }

        private SaveData ToSaveData()
        {
            return new SaveData
            {
                configVersion = config.ConfigVersion,
                economy = new EconomySaveData
                {
                    coins = Coins,
                    gems = Gems,
                    energy = Energy,
                    resortValue = ResortValue,
                    reputation = Reputation
                }
            };
        }

        private void MarkDirty()
        {
            if (isDirty) return;
            isDirty = true;
            dirtySince = Time.unscaledTime;
        }

        private void Update()
        {
            if (isDirty && Time.unscaledTime - dirtySince >= autosaveDelay)
                SaveNow();
        }

        // Mobile OSes can kill a paused app without calling OnApplicationQuit.
        private void OnApplicationPause(bool paused)
        {
            if (paused && isDirty)
                SaveNow();
        }

        private void OnApplicationQuit()
        {
            if (isDirty)
                SaveNow();
        }

        private void SaveNow()
        {
            if (!canSave || saveSystem == null) return;

            try
            {
                saveSystem.Save(ToSaveData());
                isDirty = false;
            }
            catch (Exception e)
            {
                // Keep the dirty flag and retry after the autosave delay.
                dirtySince = Time.unscaledTime;
                Debug.LogError($"[PlayerEconomy] Saving failed: {e.Message}");
            }
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            Coins += amount;
            OnCoinsChanged?.Invoke(Coins);
            MarkDirty();
        }

        public bool TrySpendCoins(int amount)
        {
            if (amount <= 0 || Coins < amount) return false;
            Coins -= amount;
            OnCoinsChanged?.Invoke(Coins);
            MarkDirty();
            return true;
        }

        public void AddGems(int amount)
        {
            if (amount <= 0) return;
            Gems += amount;
            OnGemsChanged?.Invoke(Gems);
            MarkDirty();
        }

        public bool TrySpendGems(int amount)
        {
            if (amount <= 0 || Gems < amount) return false;
            Gems -= amount;
            OnGemsChanged?.Invoke(Gems);
            MarkDirty();
            return true;
        }

        public bool TrySpendEnergy(int amount)
        {
            if (amount <= 0 || Energy < amount) return false;
            Energy -= amount;
            OnEnergyChanged?.Invoke(Energy);
            MarkDirty();
            return true;
        }

        public void RestoreEnergy(int amount)
        {
            if (amount <= 0) return;
            Energy = Mathf.Min(Energy + amount, MaxEnergy);
            OnEnergyChanged?.Invoke(Energy);
            MarkDirty();
        }

        public void AddResortValue(int amount)
        {
            if (amount <= 0) return;
            ResortValue += amount;
            OnResortValueChanged?.Invoke(ResortValue);
            MarkDirty();
        }

        public void SetReputation(int value)
        {
            int nextValue = Mathf.Clamp(value, 0, 5);
            if (nextValue == Reputation) return;
            Reputation = nextValue;
            OnReputationChanged?.Invoke(Reputation);
            MarkDirty();
        }
    }
}
