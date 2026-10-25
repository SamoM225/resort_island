using NUnit.Framework;
using ResortIsland.UI;
using ResortIsland.Waves;

namespace ResortIsland.Tests
{
    public class WaveForecastFormatterTests
    {
        [Test]
        public void Format_ListsEveryNeedWithItsUnits()
        {
            WavePlan plan = new WavePlan(1, new[] { new NeedDemand("rooms", 8), new NeedDemand("drinks", 8), new NeedDemand("food", 6) });

            Assert.AreEqual("Next wave: Rooms 8 · Drinks 8 · Food 6", WaveForecastFormatter.Format(plan));
        }

        [Test]
        public void DisplayName_TurnsNeedIdIntoTitle()
        {
            Assert.AreEqual("Ocean View", WaveForecastFormatter.DisplayName("ocean_view"));
        }
    }
}
