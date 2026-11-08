using UnityEngine;

namespace ResortIsland.Resort
{
    public readonly struct Dock
    {
        public const int PierLength = 4;
        public const int FerryWidth = 2;

        public Vector3Int LandingCell { get; }
        public Vector3Int Seaward { get; }
        public Vector3Int Side { get; }

        public Dock(Vector3Int landingCell, Vector3Int seaward, Vector3Int side)
        {
            LandingCell = landingCell;
            Seaward = seaward;
            Side = side;
        }

        public Vector3Int PierOrigin => Vector3Int.Min(LandingCell + Seaward, LandingCell + Seaward * PierLength);
        public Vector2Int PierSize => Seaward.x != 0 ? new Vector2Int(PierLength, 1) : new Vector2Int(1, PierLength);

        public Vector3Int FerryOrigin => Vector3Int.Min(LandingCell + Seaward + Side, LandingCell + Seaward * PierLength + Side * FerryWidth);
        public Vector2Int FerrySize => Seaward.x != 0 ? new Vector2Int(PierLength, FerryWidth) : new Vector2Int(FerryWidth, PierLength);
    }
}
