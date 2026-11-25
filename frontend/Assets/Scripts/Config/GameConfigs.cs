using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace ResortIsland.Config
{
    public class GameConfigs
    {
        private static readonly JsonSerializer Serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
        });

        [JsonProperty(Required = Required.Always)] public EconomyConfig Economy { get; set; }
        [JsonProperty(Required = Required.Always)] public WaveSystemConfig WaveSystem { get; set; }
        [JsonProperty(Required = Required.Always)] public EnergyConfig Energy { get; set; }
        [JsonProperty(Required = Required.Always)] public NeedsConfig Needs { get; set; }
        [JsonProperty(Required = Required.Always)] public BuildingsConfig Buildings { get; set; }
        [JsonProperty(Required = Required.Always)] public UnlocksConfig Unlocks { get; set; }
        [JsonProperty(Required = Required.Always)] public PathsConfig Paths { get; set; }
        [JsonProperty(Required = Required.Always)] public WavesConfig Waves { get; set; }

        public static GameConfigs Parse(string response)
        {
            JArray items = JObject.Parse(response)["data"] as JArray;
            if (items == null)
                throw new JsonException("Response has no \"data\" array.");

            JObject byKey = new JObject();
            foreach (JToken item in items)
                byKey[item.Value<string>("key")] = item["data"];

            return byKey.ToObject<GameConfigs>(Serializer);
        }
    }

    public class EconomyConfig
    {
        public int StartingCoins { get; set; }
        public int StartingGems { get; set; }
        public int StartingEnergy { get; set; }
        public int MaxEnergy { get; set; }
        public int StartingResortValue { get; set; }
        public int StartingReputation { get; set; }
        [JsonProperty(Required = Required.Always)] public int StartingWorkers { get; set; }
        public int CoinsPerUnit { get; set; }
        public float[] CoinsRatingMult { get; set; }
        /// <summary>Resort value gained for a wave rated 1-5 stars and Perfect (index = rating - 1).</summary>
        [JsonProperty(Required = Required.Always)] public int[] ResortValueByRating { get; set; }
    }

    public class WaveSystemConfig
    {
        public float WaveTimerS { get; set; }
        public float ServiceTimeS { get; set; }
        public float UtilizationStaffed { get; set; }
        public float UtilizationEmpty { get; set; }
        public int WorkerSlots { get; set; }
        public int BottleneckThreshold { get; set; }
        public float BottleneckHysteresisS { get; set; }
        public float PatienceGraceS { get; set; }
        public float PatienceFloorAfterS { get; set; }
        public float UnitFloor { get; set; }
        public float WaveFloor { get; set; }
        public float[] RatingThresholds { get; set; }
        public float NewNeedDebutRatio { get; set; }
        public float DemandGrowthPerWave { get; set; }
        public RushConfig Rush { get; set; }
    }

    public class RushConfig
    {
        public int Wave { get; set; }
        public float TimeS { get; set; }
        public int Units { get; set; }
        public float DurationS { get; set; }
    }

    public class EnergyConfig
    {
        public int BoostCost { get; set; }
        public float SpeedReductionPct { get; set; }
        public float SpeedDurationS { get; set; }
        public int MaxBoostsPerBuildingPerWave { get; set; }
        public int[] DropByRating { get; set; }
    }

    public class NeedsConfig
    {
        public List<NeedDefinition> Needs { get; set; }
    }

    public class NeedDefinition
    {
        public string Id { get; set; }
        public float SatisfactionWeight { get; set; }
    }

    public class BuildingsConfig
    {
        public List<BuildingLevelConfig> Levels { get; set; }
        public List<BuildingDefinition> Buildings { get; set; }
    }

    public class BuildingLevelConfig
    {
        public int Level { get; set; }
        public int Capacity { get; set; }
        public int UpgradeCost { get; set; }
        /// <summary>Resort value gained when a building is upgraded to this level.</summary>
        [JsonProperty(Required = Required.Always)] public int ResortValue { get; set; }
    }

    public class BuildingDefinition
    {
        public string Id { get; set; }
        public string Need { get; set; }
        public string Unlock { get; set; }
        /// <summary>Coins paid for building one from the build menu (0 = free).</summary>
        [JsonProperty(Required = Required.Always)] public int Cost { get; set; }
        [JsonProperty(Required = Required.Always)] public int MaxCount { get; set; }
        /// <summary>Resort value gained for building one.</summary>
        [JsonProperty(Required = Required.Always)] public int ResortValue { get; set; }
    }

    public class UnlocksConfig
    {
        public List<UnlockConfig> Unlocks { get; set; }
    }

    public class UnlockConfig
    {
        public string Id { get; set; }
        public int AfterWave { get; set; }
        public int Cost { get; set; }
        public string Building { get; set; }
        public int WorkersAdded { get; set; }
    }

    public class PathsConfig
    {
        public List<PathStyleConfig> Styles { get; set; }
    }

    /// <summary>The numbers of a path style; its look (sheets, names) is in Resources/PathStyles.asset.</summary>
    public class PathStyleConfig
    {
        public string Id { get; set; }
        /// <summary>Can be built from this wave on (1 = from the start).</summary>
        [JsonProperty(Required = Required.Always)] public int UnlockWave { get; set; }
        /// <summary>Level 1, 2, ... in the order of the levels in PathStyles.asset.</summary>
        [JsonProperty(Required = Required.Always)] public List<PathLevelConfig> Levels { get; set; }
    }

    public class PathLevelConfig
    {
        [JsonProperty(Required = Required.Always)] public int CostPerTile { get; set; }
        /// <summary>Resort value gained for each tile laid or raised to this level.</summary>
        [JsonProperty(Required = Required.Always)] public int ValuePerTile { get; set; }
        /// <summary>Shown in the build menu; guests do not walk faster yet.</summary>
        [JsonProperty(Required = Required.Always)] public int GuestSpeedPct { get; set; }
    }

    public class WavesConfig
    {
        public List<WaveConfig> Waves { get; set; }
    }

    public class WaveConfig
    {
        public int Wave { get; set; }
        public int Guests { get; set; }
        public Dictionary<string, int> Demand { get; set; }
    }
}
