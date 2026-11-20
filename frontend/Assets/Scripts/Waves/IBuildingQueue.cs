using System;

namespace ResortIsland.Waves
{
    public interface IBuildingQueue
    {
        event Action<double> UnitSeated;
        event Action UnitServed;

        string NeedId { get; }
        int Seats { get; }
        int Seated { get; }
        int Waiting { get; }
        bool IsClear { get; }
    }
}
