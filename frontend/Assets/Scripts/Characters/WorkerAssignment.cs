using System;

namespace ResortIsland.Characters
{
    public static class WorkerAssignment
    {
        /// <summary>
        /// The workers the player has (starting_workers + workers of unlocks, see GameTuning.Workers),
        /// but never more than there are buildings to work in.
        /// </summary>
        public static int RequiredWorkerCount(int buildingCount, int workers)
        {
            if (buildingCount < 0)
                throw new ArgumentOutOfRangeException(nameof(buildingCount), "Building count cannot be negative.");
            return Math.Min(buildingCount, Math.Max(0, workers));
        }
    }
}
