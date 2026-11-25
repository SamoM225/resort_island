using System;
using System.Collections.Generic;

namespace ResortIsland.Config
{
    /// <summary>
    /// Tunable numbers of the game (prices, limits, workers, resort value) looked up in the game configs.
    /// Nothing here has a value of its own: something the configs do not list costs nothing, gives nothing
    /// and cannot be built, so a missing entry is visible in the game instead of silently using a default.
    /// </summary>
    public class GameTuning
    {
        private readonly GameConfigs configs;

        public GameTuning(GameConfigs configs)
        {
            this.configs = configs;
        }

        public GameConfigs Configs => configs;

        // ---- buildings ----

        public BuildingDefinition Building(string id)
        {
            List<BuildingDefinition> buildings = configs?.Buildings?.Buildings;
            if (buildings == null || id == null) return null;
            foreach (BuildingDefinition building in buildings)
                if (building != null && building.Id == id) return building;
            return null;
        }

        public int BuildingCost(string id) => Math.Max(0, Building(id)?.Cost ?? 0);

        /// <summary>How many of this building the player may have; 0 when the configs do not know it.</summary>
        public int MaxCount(string id) => Math.Max(0, Building(id)?.MaxCount ?? 0);

        public int BuildingValue(string id) => Math.Max(0, Building(id)?.ResortValue ?? 0);

        /// <summary>Guests served at once by a building of this level (1 when the level is missing).</summary>
        public int Capacity(int level) => Math.Max(1, Level(level)?.Capacity ?? 1);

        public BuildingLevelConfig Level(int level)
        {
            List<BuildingLevelConfig> levels = configs?.Buildings?.Levels;
            if (levels == null) return null;
            foreach (BuildingLevelConfig entry in levels)
                if (entry != null && entry.Level == level) return entry;
            return null;
        }

        // ---- workers ----

        public int StartingWorkers => Math.Max(0, configs?.Economy?.StartingWorkers ?? 0);

        /// <summary>starting_workers plus the workers_added of every unlock the player has.</summary>
        public int Workers(Func<string, bool> hasUnlock)
        {
            int workers = StartingWorkers;
            List<UnlockConfig> unlocks = configs?.Unlocks?.Unlocks;
            if (unlocks == null || hasUnlock == null) return workers;
            foreach (UnlockConfig unlock in unlocks)
                if (unlock != null && hasUnlock(unlock.Id)) workers += Math.Max(0, unlock.WorkersAdded);
            return workers;
        }

        // ---- resort value ----

        /// <summary>Resort value for a wave rated 1-5 stars or 6 = Perfect.</summary>
        public int RatingValue(int rating)
        {
            int[] values = configs?.Economy?.ResortValueByRating;
            if (values == null || values.Length == 0 || rating < 1) return 0;
            return Math.Max(0, values[Math.Min(rating, values.Length) - 1]);
        }

        // ---- paths ----

        public PathStyleConfig PathStyle(string id)
        {
            List<PathStyleConfig> styles = configs?.Paths?.Styles;
            if (styles == null || id == null) return null;
            foreach (PathStyleConfig style in styles)
                if (style != null && style.Id == id) return style;
            return null;
        }

        /// <summary>A style the configs do not list is never unlocked.</summary>
        public bool IsPathUnlocked(string id, int reachedWave)
        {
            PathStyleConfig style = PathStyle(id);
            return style != null && reachedWave >= style.UnlockWave;
        }

        public int PathUnlockWave(string id) => PathStyle(id)?.UnlockWave ?? int.MaxValue;

        /// <summary>level is 1-based; null when the style or the level is not in the configs.</summary>
        public PathLevelConfig PathLevel(string id, int level)
        {
            List<PathLevelConfig> levels = PathStyle(id)?.Levels;
            return levels != null && level >= 1 && level <= levels.Count ? levels[level - 1] : null;
        }

        public int PathCost(string id, int level) => Math.Max(0, PathLevel(id, level)?.CostPerTile ?? 0);

        public int PathValue(string id, int level) => Math.Max(0, PathLevel(id, level)?.ValuePerTile ?? 0);
    }
}
