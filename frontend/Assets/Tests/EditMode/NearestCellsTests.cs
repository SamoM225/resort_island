using NUnit.Framework;
using UnityEngine;
using ResortIsland.Resort;

namespace ResortIsland.Tests
{
    public class NearestCellsTests
    {
        [Test]
        public void Take_ReturnsClosestCellsFirst()
        {
            Vector3Int[] cells = { new Vector3Int(5, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(0, 3, 0), new Vector3Int(2, 2, 0) };

            CollectionAssert.AreEqual(
                new[] { new Vector3Int(1, 0, 0), new Vector3Int(2, 2, 0) },
                NearestCells.Take(cells, Vector3Int.zero, 2));
        }

        [Test]
        public void MoreGuestsThanCells_SharesCells()
        {
            Vector3Int[] cells = { new Vector3Int(1, 0, 0), new Vector3Int(0, 2, 0) };

            CollectionAssert.AreEqual(
                new[] { cells[0], cells[1], cells[0], cells[1], cells[0] },
                NearestCells.Take(cells, Vector3Int.zero, 5));
        }

        [Test]
        public void NoCells_GivesNothing()
        {
            CollectionAssert.IsEmpty(NearestCells.Take(new Vector3Int[0], Vector3Int.zero, 3));
        }
    }
}
