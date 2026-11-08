using System.Collections.Generic;
using System.Linq;
using ResortIsland.Config;

namespace ResortIsland.Waves
{
    public static class StartingNeeds
    {
        public static IEnumerable<string> From(BuildingsConfig buildings)
        {
            return buildings.Buildings.Where(building => building.Unlock == null).Select(building => building.Need);
        }
    }
}
