using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using ResortIsland.Resort;

namespace ResortIsland.Tests
{
    public class DockLocatorTests
    {
        private HashSet<Vector3Int> land;

        [SetUp]
        public void SetUp()
        {
            land = new HashSet<Vector3Int>();
            for (int x = 0; x < 5; x++)
                for (int y = 0; y < 5; y++)
                    land.Add(new Vector3Int(x, y, 0));
        }

        private static Vector3 Isometric(Vector3Int cell) => new Vector3((cell.x - cell.y) * 0.5f, (cell.x + cell.y) * 0.25f, 0f);

        private bool IsWater(Vector3Int cell) => !land.Contains(cell);

        private List<Vector3Int> Beach() => land.Where(cell => new[] { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down }.Any(step => IsWater(cell + step))).ToList();

        private static IEnumerable<Vector3Int> Cells(Vector3Int origin, Vector2Int size)
        {
            for (int x = 0; x < size.x; x++)
                for (int y = 0; y < size.y; y++)
                    yield return origin + new Vector3Int(x, y, 0);
        }

        [Test]
        public void Pier_IsBuiltOnTheFrontmostBeachTowardsTheViewer()
        {
            Assert.IsTrue(DockLocator.TryFind(Beach(), IsWater, Isometric, out Dock dock));

            Assert.AreEqual(Vector3Int.zero, dock.LandingCell);
            Assert.Less(Isometric(dock.LandingCell + dock.Seaward).y, Isometric(dock.LandingCell).y);
        }

        [Test]
        public void PierAndFerry_StandOnOpenWaterNextToTheLanding()
        {
            DockLocator.TryFind(Beach(), IsWater, Isometric, out Dock dock);

            List<Vector3Int> pier = Cells(dock.PierOrigin, dock.PierSize).ToList();
            List<Vector3Int> ferry = Cells(dock.FerryOrigin, dock.FerrySize).ToList();
            Assert.AreEqual(Dock.PierLength, pier.Count);
            Assert.AreEqual(Dock.PierLength * Dock.FerryWidth, ferry.Count);
            Assert.IsTrue(pier.Concat(ferry).All(IsWater));
            Assert.IsEmpty(pier.Intersect(ferry));
            Assert.Contains(dock.LandingCell + dock.Seaward, pier);
        }

        [Test]
        public void BlockedBeach_IsSkipped()
        {
            List<Vector3Int> beach = Beach();
            land.Add(new Vector3Int(-2, 0, 0));
            land.Add(new Vector3Int(0, -2, 0));

            Assert.IsTrue(DockLocator.TryFind(beach, IsWater, Isometric, out Dock dock));

            Assert.AreNotEqual(Vector3Int.zero, dock.LandingCell);
            Assert.IsTrue(Cells(dock.PierOrigin, dock.PierSize).Concat(Cells(dock.FerryOrigin, dock.FerrySize)).All(IsWater));
        }

        [Test]
        public void Pier_NeverPointsAwayFromTheViewer()
        {
            Assert.IsFalse(DockLocator.TryFind(new[] { new Vector3Int(4, 4, 0) }, IsWater, Isometric, out _));
        }

        [Test]
        public void WithoutOpenWater_NoDockIsFound()
        {
            Assert.IsFalse(DockLocator.TryFind(Beach(), _ => false, Isometric, out _));
        }
    }
}
