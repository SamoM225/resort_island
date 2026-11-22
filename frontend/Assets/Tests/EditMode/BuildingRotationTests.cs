using NUnit.Framework;
using UnityEngine;
using ResortIsland.Resort;

namespace ResortIsland.Tests
{
    public class BuildingRotationTests
    {
        private static readonly Vector2Int Square = new Vector2Int(3, 3);
        private static readonly Vector2Int FrontLeftDoor = new Vector2Int(-1, 1);

        [Test]
        public void Entrance_GoesRoundTheBuilding_FrontLeft_BackLeft_BackRight_FrontRight()
        {
            // R0 faces -x, R1 +y, R2 +x, R3 -y (the ResortEmpire sheet columns)
            Assert.AreEqual(new Vector2Int(-1, 1), BuildingRotation.RotateCell(FrontLeftDoor, Square, 0));
            Assert.AreEqual(new Vector2Int(1, 3), BuildingRotation.RotateCell(FrontLeftDoor, Square, 1));
            Assert.AreEqual(new Vector2Int(3, 1), BuildingRotation.RotateCell(FrontLeftDoor, Square, 2));
            Assert.AreEqual(new Vector2Int(1, -1), BuildingRotation.RotateCell(FrontLeftDoor, Square, 3));
        }

        [Test]
        public void FourTurns_AreTheStartAgain()
        {
            var size = new Vector2Int(2, 4);
            var cell = new Vector2Int(-1, 3);

            Assert.AreEqual(cell, BuildingRotation.RotateCell(cell, size, 4));
            Assert.AreEqual(BuildingRotation.RotateCell(cell, size, 1), BuildingRotation.RotateCell(cell, size, -3));
        }

        [Test]
        public void OddRotations_SwapTheFootprint()
        {
            var size = new Vector2Int(2, 4);

            Assert.AreEqual(size, BuildingRotation.Size(size, 0));
            Assert.AreEqual(new Vector2Int(4, 2), BuildingRotation.Size(size, 1));
            Assert.AreEqual(size, BuildingRotation.Size(size, 2));
            Assert.AreEqual(new Vector2Int(4, 2), BuildingRotation.Size(size, 3));
        }

        [Test]
        public void FootprintCells_StayInsideTheRotatedFootprint()
        {
            var size = new Vector2Int(2, 4);
            for (int r = 0; r < 4; r++)
            {
                Vector2Int rotated = BuildingRotation.Size(size, r);
                for (int x = 0; x < size.x; x++)
                for (int y = 0; y < size.y; y++)
                {
                    Vector2Int c = BuildingRotation.RotateCell(new Vector2Int(x, y), size, r);
                    Assert.IsTrue(c.x >= 0 && c.x < rotated.x && c.y >= 0 && c.y < rotated.y, $"R{r}: {c} is outside {rotated}");
                }
            }
        }

        [Test]
        public void EntranceCell_IsOffsetFromTheOrigin()
        {
            var origin = new Vector3Int(5, -2, 0);

            Assert.AreEqual(new Vector3Int(4, -1, 0), BuildingRotation.EntranceCell(origin, Square, FrontLeftDoor, 0));
            Assert.AreEqual(new Vector3Int(6, 1, 0), BuildingRotation.EntranceCell(origin, Square, FrontLeftDoor, 1));
        }
    }
}
