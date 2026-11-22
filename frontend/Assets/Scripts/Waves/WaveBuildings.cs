using System;
using System.Collections.Generic;
using System.Linq;
using ResortIsland.Config;

namespace ResortIsland.Waves
{
    public class WaveBuildings
    {
        private const int StartLevel = 1;

        private readonly Dictionary<string, BuildingService> byNeed = new Dictionary<string, BuildingService>();

        public WaveBuildings(IEnumerable<BuildingService> buildings)
        {
            if (buildings == null)
                throw new ArgumentNullException(nameof(buildings));

            foreach (BuildingService building in buildings)
            {
                if (byNeed.ContainsKey(building.NeedId))
                    throw new ArgumentException($"Need \"{building.NeedId}\" has more than one building.", nameof(buildings));

                byNeed.Add(building.NeedId, building);
                building.UnitSeated += wait => UnitSeated?.Invoke(building.NeedId, wait);
            }
        }

        public event Action<string, double> UnitSeated;

        public IEnumerable<IBuildingQueue> Queues => byNeed.Values;

        public void Arrive(string needId)
        {
            if (needId == null || !byNeed.TryGetValue(needId, out BuildingService building))
                throw new ArgumentException($"No building serves need \"{needId}\".", nameof(needId));

            building.Arrive();
        }

        public void Tick(double seconds)
        {
            foreach (BuildingService building in byNeed.Values)
                building.Tick(seconds);
        }

        public static WaveBuildings From(GameConfigs configs)
        {
            if (configs == null)
                throw new ArgumentNullException(nameof(configs));

            int seats = BuildingSeats.For(configs.Buildings, StartLevel);
            ServiceRules rules = ServiceRules.From(configs.WaveSystem);
            return new WaveBuildings(configs.Buildings.Buildings.Select(building => new BuildingService(building.Need, seats, rules)));
        }
    }
}
