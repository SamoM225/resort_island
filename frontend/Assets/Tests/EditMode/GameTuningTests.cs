using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using ResortIsland.Config;

namespace ResortIsland.Tests
{
    public class GameTuningTests
    {
        private static GameTuning Tuning() => new GameTuning(GameConfigs.Parse(
            File.ReadAllText(Path.Combine(Application.dataPath, "Tests/EditMode/Fixtures/configs_response.json"))));

        [Test]
        public void Buildings_ComeFromTheConfigs()
        {
            GameTuning tuning = Tuning();

            Assert.AreEqual(0, tuning.BuildingCost("reception"));
            Assert.AreEqual(3, tuning.MaxCount("reception"));
            Assert.AreEqual(2500, tuning.BuildingValue("bungalows"));
            Assert.AreEqual(3, tuning.Capacity(1));
            Assert.AreEqual(7, tuning.Capacity(3));
        }

        [Test]
        public void UnknownBuilding_CannotBeBuiltAndGivesNothing()
        {
            GameTuning tuning = Tuning();

            Assert.AreEqual(0, tuning.MaxCount("spa"));
            Assert.AreEqual(0, tuning.BuildingCost("spa"));
            Assert.AreEqual(0, tuning.BuildingValue("spa"));
        }

        [Test]
        public void Workers_AreStartingWorkersPlusWorkersOfUnlocks()
        {
            GameTuning tuning = Tuning();

            Assert.AreEqual(2, tuning.Workers(id => false));
            Assert.AreEqual(3, tuning.Workers(id => id == "pool"));
            Assert.AreEqual(4, tuning.Workers(id => true));
        }

        [Test]
        public void RatingValue_UsesTheRatingAsIndex_PerfectIsTheLast()
        {
            GameTuning tuning = Tuning();
            int[] values = tuning.Configs.Economy.ResortValueByRating;

            Assert.AreEqual(values[0], tuning.RatingValue(1));
            Assert.AreEqual(values[4], tuning.RatingValue(5));
            Assert.AreEqual(values[5], tuning.RatingValue(6));
            Assert.AreEqual(0, tuning.RatingValue(0));
        }

        [Test]
        public void Paths_PriceValueAndUnlockWavePerLevel()
        {
            GameTuning tuning = Tuning();

            Assert.AreEqual(0, tuning.PathCost("white_paving", 1));
            Assert.AreEqual(15, tuning.PathCost("boardwalk", 2));
            Assert.AreEqual(3, tuning.PathValue("boardwalk", 3));
            Assert.AreEqual(0, tuning.PathCost("boardwalk", 4));
            Assert.IsTrue(tuning.IsPathUnlocked("sand_trail", 1));
            Assert.IsFalse(tuning.IsPathUnlocked("cobblestone", 5));
            Assert.IsTrue(tuning.IsPathUnlocked("cobblestone", 6));
        }

        [Test]
        public void UnknownPathStyle_IsNeverUnlocked()
        {
            GameTuning tuning = Tuning();

            Assert.IsFalse(tuning.IsPathUnlocked("marble", 99));
            Assert.AreEqual(0, tuning.PathCost("marble", 1));
        }

        [Test]
        public void WithoutConfigs_NothingCanBeBuilt()
        {
            var tuning = new GameTuning(null);

            Assert.AreEqual(0, tuning.MaxCount("reception"));
            Assert.AreEqual(0, tuning.Workers(id => true));
            Assert.AreEqual(1, tuning.Capacity(1));
            Assert.IsFalse(tuning.IsPathUnlocked("white_paving", 1));
        }

        [Test]
        public void BundledPathStyles_HaveNumbersForEveryLevel()
        {
            var catalog = Resources.Load<ResortIsland.Resort.PathStyleCatalog>("PathStyles");
            GameTuning tuning = new GameTuning(GameConfigs.Parse(Resources.Load<TextAsset>(GameConfigProvider.BundledResourcePath).text));

            Assert.IsNotNull(catalog);
            foreach (var style in catalog.Styles)
            {
                PathStyleConfig numbers = tuning.PathStyle(style.Id);
                Assert.IsNotNull(numbers, style.Id + " is missing in paths.json.");
                Assert.AreEqual(style.LevelCount, numbers.Levels.Count, style.Id + ": paths.json has a different number of levels than PathStyles.asset.");
            }
            CollectionAssert.IsSubsetOf(tuning.Configs.Paths.Styles.Select(s => s.Id), catalog.Styles.Select(s => s.Id),
                "paths.json has a style PathStyles.asset does not draw.");
        }
    }
}
