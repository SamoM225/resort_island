using System.Collections.Generic;

namespace ResortIsland.Analytics
{
    public interface IAnalyticsStore
    {
        List<AnalyticsEvent> Load();
        void Save(IReadOnlyList<AnalyticsEvent> events);
    }
}
