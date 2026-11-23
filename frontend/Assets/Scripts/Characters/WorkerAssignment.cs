using System;

namespace ResortIsland.Characters
{
    public static class WorkerAssignment
    {
        public static int RequiredWorkerCount(int buildingCount)
        {
            if (buildingCount < 0)
                throw new ArgumentOutOfRangeException(nameof(buildingCount), "Building count cannot be negative.");
            return Math.Max(0, buildingCount - 1);
        }
    }
}
