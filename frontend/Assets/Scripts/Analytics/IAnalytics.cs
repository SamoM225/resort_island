using System.Collections.Generic;

namespace ResortIsland.Analytics
{
    public interface IAnalytics
    {
        void Track(string name, Dictionary<string, object> payload = null);
    }
}
