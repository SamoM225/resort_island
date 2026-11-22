using System.Collections.Generic;

namespace ResortIsland.Waves
{
    public interface IGuestSpawner
    {
        void Spawn(int count);
    }

    public interface INeedAwareGuestSpawner
    {
        void Spawn(int count, IReadOnlyList<NeedDemand> needs);
    }
}
