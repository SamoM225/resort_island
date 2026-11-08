using System.Collections.Generic;

namespace ResortIsland.Analytics
{
    public interface IWaveMetric
    {
        void AddTo(Dictionary<string, object> payload);
    }
}
