using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using ResortIsland.Resort;

namespace ResortIsland.Tests
{
    public class PathNetworkTests
    {
        private static readonly Vector3Int Landing = new Vector3Int(10, 0, 0);

        private HashSet<Vector3Int> land;
        private List<PathFootprint> buildings;
        private PathNetwork network;

        [SetUp]
        public void SetUp()
        {
            land = new HashSet<Vector3Int>();
            for (int x = 0; x < 20; x++)
                for (int y = 0; y < 20; y++)
                    land.Add(new Vector3Int(x, y, 0));
            buildings = new List<PathFootprint>();
            network = new PathNetwork(IsWalkable);
        }

        private bool IsWalkable(Vector3Int cell) => land.Contains(cell) && !buildings.Exists(b => b.Contains(cell));

        private void WithMainPath() => network.SetMain(Landing, PathFinder.DirectionBit(Vector3Int.down));

        private void Place(string owner, int x, int y)
        {
            var before = network.ConnectedOwners(buildings);
            buildings.Add(new PathFootprint(owner, new Vector3Int(x, y, 0), new Vector2Int(3, 3)));
            network.Reconnect(buildings, before, owner);
        }

        private void Move(string owner, int x, int y)
        {
            var before = network.ConnectedOwners(buildings);
            int i = buildings.FindIndex(b => Equals(b.Owner, owner));
            buildings[i] = new PathFootprint(owner, new Vector3Int(x, y, 0), buildings[i].Size);
            network.Reconnect(buildings, before, owner);
        }

        private List<Vector3Int> OwnedBy(string owner)
            => network.Cells.Where(p => p.Value.Kind == PathKind.Auto && Equals(p.Value.Owner, owner)).Select(p => p.Key).ToList();

        private bool IsConnected(string owner) => network.ConnectedOwners(buildings).Contains(owner);

        private HashSet<Vector3Int> ReachableFromLanding()
        {
            var reached = new HashSet<Vector3Int> { Landing };
            var queue = new Queue<Vector3Int>(reached);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var step in PathFinder.Directions)
                    if (network.Contains(cell + step) && reached.Add(cell + step)) queue.Enqueue(cell + step);
            }
            return reached;
        }

        [Test]
        public void NewBuilding_GetsAStraightPathToTheMainPath()
        {
            WithMainPath();

            Place("A", 9, 6);

            Assert.IsTrue(IsConnected("A"));
            CollectionAssert.AreEquivalent(
                new[] { new Vector3Int(10, 1, 0), new Vector3Int(10, 2, 0), new Vector3Int(10, 3, 0), new Vector3Int(10, 4, 0), new Vector3Int(10, 5, 0) },
                OwnedBy("A"));
            // last cell leads into the building (+y), the landing leads onto the pier (-y)
            int straightAlongY = PathFinder.DirectionBit(Vector3Int.up) | PathFinder.DirectionBit(Vector3Int.down);
            Assert.AreEqual(straightAlongY, network.MaskAt(new Vector3Int(10, 5, 0)));
            Assert.AreEqual(straightAlongY, network.MaskAt(Landing));
        }

        [Test]
        public void SecondBuilding_JoinsTheNearestPath()
        {
            WithMainPath();
            Place("A", 9, 6);

            Place("B", 14, 6);

            Assert.IsTrue(IsConnected("B"));
            CollectionAssert.AreEquivalent(
                new[] { new Vector3Int(11, 5, 0), new Vector3Int(12, 5, 0), new Vector3Int(13, 5, 0), new Vector3Int(14, 5, 0) },
                OwnedBy("B"));
            Assert.AreEqual(5, OwnedBy("A").Count);
        }

        [Test]
        public void InitialGeneration_ConnectsNearerBuildingsFirst()
        {
            WithMainPath();
            buildings.Add(new PathFootprint("B", new Vector3Int(14, 6, 0), new Vector2Int(3, 3)));
            buildings.Add(new PathFootprint("A", new Vector3Int(9, 6, 0), new Vector2Int(3, 3)));

            network.Reconnect(buildings, new List<object> { "A", "B" });

            Assert.IsTrue(IsConnected("A"));
            Assert.IsTrue(IsConnected("B"));
            Assert.AreEqual(5, OwnedBy("A").Count);
            Assert.AreEqual(4, OwnedBy("B").Count);
        }

        [Test]
        public void MovedBuilding_RebuildsItsPath_AndCutOffBuildingsReconnect()
        {
            WithMainPath();
            Place("A", 9, 6);
            Place("B", 14, 6);

            Move("A", 3, 6);

            Assert.IsTrue(IsConnected("A"));
            Assert.IsTrue(IsConnected("B"));
            var reachable = ReachableFromLanding();
            foreach (var cell in OwnedBy("A").Concat(OwnedBy("B")))
                Assert.IsTrue(reachable.Contains(cell), "Generated path cell " + cell + " leads nowhere.");
        }

        [Test]
        public void PathDeletedByThePlayer_IsNotGeneratedAgainByOtherMoves()
        {
            WithMainPath();
            Place("A", 9, 6);
            Place("B", 14, 6);

            Assert.IsTrue(network.Remove(new Vector3Int(12, 5, 0)));
            Move("A", 2, 6);

            Assert.IsTrue(IsConnected("A"));
            Assert.IsFalse(IsConnected("B"));
        }

        [Test]
        public void MovingTheBuildingItself_RegeneratesItsPath()
        {
            WithMainPath();
            Place("A", 9, 6);
            network.Remove(new Vector3Int(10, 3, 0));
            Assert.IsFalse(IsConnected("A"));

            Move("A", 9, 7);

            Assert.IsTrue(IsConnected("A"));
            CollectionAssert.AreEquivalent(
                Enumerable.Range(1, 6).Select(y => new Vector3Int(10, y, 0)),
                OwnedBy("A"));
        }

        [Test]
        public void WithoutMainPath_BuildingsConnectToTheFirstBuilding()
        {
            Place("A", 2, 2);
            Place("B", 8, 2);

            Assert.IsTrue(IsConnected("A"));
            Assert.IsTrue(IsConnected("B"));
            Assert.IsEmpty(OwnedBy("A"));
            Assert.AreEqual(3, OwnedBy("B").Count);
        }

        [Test]
        public void PathUnderANewBuilding_IsRemoved()
        {
            WithMainPath();
            Assert.IsTrue(network.AddManual(new Vector3Int(5, 5, 0)));

            Place("C", 4, 4);

            Assert.IsFalse(network.Contains(new Vector3Int(5, 5, 0)));
        }

        [Test]
        public void ManualEditing_RespectsTheMainPathAndFreeLand()
        {
            WithMainPath();
            Place("A", 9, 6);

            Assert.IsFalse(network.Remove(Landing), "The main path cannot be removed.");
            Assert.IsFalse(network.AddManual(new Vector3Int(10, 7, 0)), "No path inside a building.");
            Assert.IsFalse(network.AddManual(new Vector3Int(10, 3, 0)), "Cell already has a path.");
            Assert.IsFalse(network.AddManual(new Vector3Int(-1, 0, 0)), "No path on water.");
            Assert.IsTrue(network.AddManual(new Vector3Int(0, 0, 0)));
            Assert.AreEqual(PathKind.Manual, network.Cells[new Vector3Int(0, 0, 0)].Kind);
            Assert.IsTrue(network.Remove(new Vector3Int(10, 3, 0)));
        }

        [Test]
        public void BuildingWithAnEntrance_GetsItsPathExactlyToTheDoor()
        {
            WithMainPath();
            // door on the far (+y) side, away from the landing
            var entrance = new Vector3Int(10, 9, 0);
            var before = network.ConnectedOwners(buildings);
            buildings.Add(new PathFootprint("A", new Vector3Int(9, 6, 0), new Vector2Int(3, 3), entrance));

            network.Reconnect(buildings, before, "A");

            Assert.IsTrue(IsConnected("A"));
            Assert.IsTrue(network.Contains(entrance), "The path must end in front of the door.");
            Assert.IsFalse(network.Contains(new Vector3Int(10, 5, 0)), "No path to the other sides.");
            Assert.AreEqual(PathFinder.DirectionBit(Vector3Int.down), network.Cells[entrance].EntryMask, "The last tile leads into the door.");
        }

        [Test]
        public void PathAlongTheWall_DoesNotConnectABuildingWithAnEntrance()
        {
            WithMainPath();
            for (int y = 1; y <= 5; y++) network.AddManual(new Vector3Int(10, y, 0));
            buildings.Add(new PathFootprint("A", new Vector3Int(9, 6, 0), new Vector2Int(3, 3), new Vector3Int(8, 7, 0)));

            Assert.IsFalse(IsConnected("A"), "(10,5) touches the building, but not its door.");
        }

        [Test]
        public void GeneratedAndMainPaths_UseTheDefaultStyle()
        {
            network.DefaultStyle = "sand";
            WithMainPath();

            Place("A", 9, 6);

            Assert.IsTrue(network.Cells.Values.All(c => c.Style == "sand" && c.Level == 1));
        }

        [Test]
        public void SetLook_ChangesStyleAndLevel_KeepsOwnerAndEntry()
        {
            WithMainPath();
            Place("A", 9, 6);
            var cell = new Vector3Int(10, 5, 0);
            var before = network.Cells[cell];

            Assert.IsTrue(network.SetLook(cell, "wood", 2));
            Assert.IsFalse(network.SetLook(cell, "wood", 2), "Same look again is not a change.");
            Assert.IsFalse(network.SetLook(new Vector3Int(0, 0, 0), "wood", 2), "No path there.");

            var after = network.Cells[cell];
            Assert.AreEqual("wood", after.Style);
            Assert.AreEqual(2, after.Level);
            Assert.AreEqual(before.Owner, after.Owner);
            Assert.AreEqual(before.EntryMask, after.EntryMask);
        }

        [Test]
        public void RestoreSnapshot_UndoesAnEdit()
        {
            WithMainPath();
            Place("A", 9, 6);
            var snapshot = network.Snapshot();

            network.AddManual(new Vector3Int(0, 0, 0), "wood");
            network.Remove(new Vector3Int(10, 3, 0));
            network.SetLook(new Vector3Int(10, 4, 0), "stone", 3);
            network.RestoreSnapshot(snapshot);

            CollectionAssert.AreEquivalent(snapshot, network.Cells);
            Assert.IsTrue(IsConnected("A"));
        }

        [Test]
        public void ChangeCost_PaysNewAndRestyledTiles_RemovingIsFree()
        {
            WithMainPath();
            network.AddManual(new Vector3Int(10, 1, 0), "sand");
            network.AddManual(new Vector3Int(10, 2, 0), "sand");
            network.AddManual(new Vector3Int(10, 3, 0), "sand");
            var before = network.Snapshot();

            network.AddManual(new Vector3Int(10, 4, 0), "wood");      // new wood tile: 10
            network.SetLook(new Vector3Int(10, 1, 0), "wood", 2);     // upgraded to wood Lv2: 15
            network.Remove(new Vector3Int(10, 3, 0));                 // free
            network.SetLook(Landing, "wood", 1);                      // the main path is never charged

            int Price(string style, int level) => style == "wood" ? (level == 2 ? 15 : 10) : 0;
            Assert.AreEqual(25, PathNetwork.ChangeCost(before, network.Cells, Price));
            Assert.AreEqual(0, PathNetwork.ChangeCost(network.Cells, network.Cells, Price));
        }

        [Test]
        public void ManualPath_CountsAsConnection()
        {
            WithMainPath();
            for (int y = 1; y <= 5; y++)
                network.AddManual(new Vector3Int(10, y, 0));

            Place("A", 9, 6);

            Assert.IsTrue(IsConnected("A"));
            Assert.IsEmpty(OwnedBy("A"));
        }
    }
}
