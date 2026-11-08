using System.Globalization;
using System.Linq;
using ResortIsland.Waves;

namespace ResortIsland.UI
{
    public static class WaveForecastFormatter
    {
        public static string Format(WavePlan plan)
        {
            return "Next wave: " + string.Join(" · ", plan.Needs.Select(need => $"{DisplayName(need.NeedId)} {need.Units}"));
        }

        public static string DisplayName(string needId)
        {
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(needId.Replace('_', ' '));
        }
    }
}
