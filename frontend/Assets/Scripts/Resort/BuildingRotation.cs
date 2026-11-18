using UnityEngine;

namespace ResortIsland.Resort
{
    /// Rotation of a building footprint in 90° steps, matching the ResortEmpire sheet columns R0…R3.
    /// One step turns the side a building faces from -x to +y to +x to -y (on screen: front-left,
    /// back-left, back-right, front-right).
    public static class BuildingRotation
    {
        public const int Count = 4;

        public static int Normalize(int rotation) => ((rotation % Count) + Count) % Count;

        /// Footprint size after rotating (width and height swap on R1 / R3).
        public static Vector2Int Size(Vector2Int size, int rotation)
            => Normalize(rotation) % 2 == 0 ? size : new Vector2Int(size.y, size.x);

        /// A cell given relative to the R0 footprint origin (it may lie outside the footprint, like an
        /// entrance) moved to where it is in the rotated footprint.
        public static Vector2Int RotateCell(Vector2Int local, Vector2Int size, int rotation)
        {
            Vector2Int cell = local;
            Vector2Int current = size;
            for (int i = 0; i < Normalize(rotation); i++)
            {
                cell = new Vector2Int(cell.y, current.x - 1 - cell.x);
                current = new Vector2Int(current.y, current.x);
            }
            return cell;
        }

        /// Grid cell in front of the door.
        public static Vector3Int EntranceCell(Vector3Int origin, Vector2Int size, Vector2Int entrance, int rotation)
        {
            Vector2Int local = RotateCell(entrance, size, rotation);
            return origin + new Vector3Int(local.x, local.y, 0);
        }
    }
}
