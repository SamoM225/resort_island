using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResortIsland.Resort
{
    public static class DockLocator
    {
        private static readonly Vector3Int[] Directions = { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down };

        public static bool TryFind(IEnumerable<Vector3Int> beachCells, Func<Vector3Int, bool> isWater, Func<Vector3Int, Vector3> cellToWorld, out Dock dock)
        {
            dock = default;
            bool found = false;
            float frontmost = float.MaxValue;

            foreach (Vector3Int cell in beachCells)
            {
                float height = cellToWorld(cell).y;
                if (height >= frontmost || !TryDockAt(cell, isWater, cellToWorld, out Dock candidate))
                    continue;

                dock = candidate;
                frontmost = height;
                found = true;
            }
            return found;
        }

        private static bool TryDockAt(Vector3Int cell, Func<Vector3Int, bool> isWater, Func<Vector3Int, Vector3> cellToWorld, out Dock dock)
        {
            foreach (Vector3Int seaward in Directions)
            {
                if (cellToWorld(cell + seaward).y >= cellToWorld(cell).y)
                    continue;

                foreach (Vector3Int side in SidesOf(seaward))
                {
                    if (!IsOpenWater(cell, seaward, side, isWater))
                        continue;

                    dock = new Dock(cell, seaward, side);
                    return true;
                }
            }

            dock = default;
            return false;
        }

        private static IEnumerable<Vector3Int> SidesOf(Vector3Int seaward)
        {
            Vector3Int side = new Vector3Int(-seaward.y, seaward.x, 0);
            yield return side;
            yield return -side;
        }

        private static bool IsOpenWater(Vector3Int cell, Vector3Int seaward, Vector3Int side, Func<Vector3Int, bool> isWater)
        {
            for (int along = 1; along <= Dock.PierLength; along++)
                for (int across = 0; across <= Dock.FerryWidth; across++)
                    if (!isWater(cell + seaward * along + side * across))
                        return false;
            return true;
        }
    }
}
