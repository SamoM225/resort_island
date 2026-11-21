using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ResortIsland.Resort;

namespace ResortIsland.Tests
{
    public class PathFinderTests
    {
        private HashSet<Vector3Int> land;
        private List<Vector3Int> route;

        [SetUp]
        public void SetUp()
        {
            land = new HashSet<Vector3Int>();
            for (int x = 0; x < 10; x++)
                for (int y = 0; y < 10; y++)
                    land.Add(new Vector3Int(x, y, 0));
            route = new List<Vector3Int>();
        }

        private bool IsWalkable(Vector3Int cell) => land.Contains(cell);

        private static void AssertContinuous(List<Vector3Int> cells)
        {
            for (int i = 1; i < cells.Count; i++)
                Assert.AreEqual(1, Mathf.Abs(cells[i].x - cells[i - 1].x) + Mathf.Abs(cells[i].y - cells[i - 1].y),
                    "Route jumps between " + cells[i - 1] + " and " + cells[i]);
        }

        private static int Turns(List<Vector3Int> cells)
        {
            int turns = 0;
            for (int i = 2; i < cells.Count; i++)
                if (cells[i] - cells[i - 1] != cells[i - 1] - cells[i - 2]) turns++;
            return turns;
        }

        [Test]
        public void StraightLine_IsTheShortestRoute()
        {
            var target = new Vector3Int(6, 2, 0);

            Assert.IsTrue(PathFinder.TryFind(new[] { new Vector3Int(1, 2, 0) }, IsWalkable, c => c == target, route));

            Assert.AreEqual(6, route.Count);
            Assert.AreEqual(new Vector3Int(1, 2, 0), route[0]);
            Assert.AreEqual(target, route[route.Count - 1]);
            Assert.AreEqual(0, Turns(route));
        }

        [Test]
        public void DiagonalTarget_GetsAnLShapeInsteadOfStairs()
        {
            var target = new Vector3Int(5, 5, 0);

            Assert.IsTrue(PathFinder.TryFind(new[] { Vector3Int.zero }, IsWalkable, c => c == target, route));

            Assert.AreEqual(11, route.Count);
            Assert.AreEqual(1, Turns(route));
            AssertContinuous(route);
        }

        [Test]
        public void Route_GoesAroundObstacles()
        {
            // wall at x = 4 with a single gap at y = 8
            for (int y = 0; y < 10; y++)
                if (y != 8) land.Remove(new Vector3Int(4, y, 0));
            var target = new Vector3Int(8, 1, 0);

            Assert.IsTrue(PathFinder.TryFind(new[] { new Vector3Int(1, 1, 0) }, IsWalkable, c => c == target, route));

            Assert.Contains(new Vector3Int(4, 8, 0), route);
            Assert.IsTrue(route.TrueForAll(IsWalkable));
            AssertContinuous(route);
        }

        [Test]
        public void StartOnTarget_ReturnsJustThatCell()
        {
            var start = new Vector3Int(3, 3, 0);

            Assert.IsTrue(PathFinder.TryFind(new[] { start }, IsWalkable, c => c == start, route));

            CollectionAssert.AreEqual(new[] { start }, route);
        }

        [Test]
        public void NearestOfSeveralStarts_IsUsed()
        {
            var target = new Vector3Int(9, 0, 0);
            var starts = new[] { new Vector3Int(0, 0, 0), new Vector3Int(7, 0, 0) };

            Assert.IsTrue(PathFinder.TryFind(starts, IsWalkable, c => c == target, route));

            Assert.AreEqual(new Vector3Int(7, 0, 0), route[0]);
            Assert.AreEqual(3, route.Count);
        }

        [Test]
        public void UnreachableTarget_ReturnsFalse()
        {
            for (int y = 0; y < 10; y++) land.Remove(new Vector3Int(4, y, 0));

            Assert.IsFalse(PathFinder.TryFind(new[] { Vector3Int.zero }, IsWalkable, c => c == new Vector3Int(8, 0, 0), route));
            Assert.IsEmpty(route);
        }

        // Unity isometric Grid with cell size (1, 0.5)
        private static Vector2 Screen(Vector3Int cell) => new Vector2((cell.x - cell.y) * 0.5f, (cell.x + cell.y) * 0.25f);

        [Test]
        public void DirectionBits_PointWhereThePathSpritesDrawTheirArms()
        {
            // Path_*_01 has its arm up-left, _02 down-right, _04 up-right, _08 down-left
            Vector2 upLeft = Screen(Directions(1)), downRight = Screen(Directions(2));
            Vector2 upRight = Screen(Directions(4)), downLeft = Screen(Directions(8));

            Assert.IsTrue(upLeft.x < 0f && upLeft.y > 0f);
            Assert.IsTrue(downRight.x > 0f && downRight.y < 0f);
            Assert.IsTrue(upRight.x > 0f && upRight.y > 0f);
            Assert.IsTrue(downLeft.x < 0f && downLeft.y < 0f);
            Assert.AreEqual(0, PathFinder.DirectionBit(new Vector3Int(1, 1, 0)));
        }

        private static Vector3Int Directions(int bit)
        {
            foreach (var step in PathFinder.Directions)
                if (PathFinder.DirectionBit(step) == bit) return step;
            Assert.Fail("No direction for bit " + bit);
            return default;
        }
    }
}
