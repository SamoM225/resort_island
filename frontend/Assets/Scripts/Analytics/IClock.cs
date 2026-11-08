using System;

namespace ResortIsland.Analytics
{
    public interface IClock
    {
        DateTime UtcNow { get; }
    }
}
