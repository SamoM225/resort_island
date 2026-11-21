using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ResortIsland.Resort;

namespace ResortIsland.Tests
{
    public class PathStaggerTests
    {
        private static List<Vector3Int> Line(int length)
        {
            var cells = new List<Vector3Int>();
            for (int x = 0; x < length; x++) cells.Add(new Vector3Int(x, 0, 0));
            return cells;
        }

        [Test]
        public void Path_GrowsAwayFromTheSeed()
        {
            var steps = PathStagger.Steps(Line(5), cell => cell.x == 0);

            for (int x = 0; x < 5; x++)
                Assert.AreEqual(x, steps[new Vector3Int(x, 0, 0)]);
        }

        [Test]
        public void TwoSeeds_GrowTowardsEachOther()
        {
            var steps = PathStagger.Steps(Line(5), cell => cell.x == 0 || cell.x == 4);

            Assert.AreEqual(2, steps[new Vector3Int(2, 0, 0)]);
            Assert.AreEqual(1, steps[new Vector3Int(3, 0, 0)]);
        }

        [Test]
        public void PartWithoutSeed_StartsFromItsFirstCell()
        {
            var cells = Line(3);
            cells.Add(new Vector3Int(10, 0, 0));
            cells.Add(new Vector3Int(11, 0, 0));

            var steps = PathStagger.Steps(cells, cell => cell.x == 0);

            Assert.AreEqual(5, steps.Count);
            Assert.AreEqual(0, steps[new Vector3Int(10, 0, 0)]);
            Assert.AreEqual(1, steps[new Vector3Int(11, 0, 0)]);
        }
    }
}
