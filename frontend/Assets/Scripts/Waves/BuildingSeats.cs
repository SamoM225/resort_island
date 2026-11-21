using System;
using System.Linq;
using ResortIsland.Config;

namespace ResortIsland.Waves
{
    public static class BuildingSeats
    {
        public static int For(BuildingsConfig buildings, int level)
        {
            if (buildings == null)
                throw new ArgumentNullException(nameof(buildings));

            BuildingLevelConfig config = buildings.Levels.FirstOrDefault(entry => entry.Level == level);
            if (config == null)
                throw new ArgumentOutOfRangeException(nameof(level), $"Level {level} is not in the buildings config.");

            return config.Capacity;
        }
    }
}
