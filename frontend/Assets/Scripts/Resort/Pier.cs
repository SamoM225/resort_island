using System.Collections.Generic;
using UnityEngine;

namespace ResortIsland.Resort
{
    public class Pier : MonoBehaviour
    {
        [SerializeField] private ResortManager resort;
        [SerializeField] private IslandGenerator island;
        [SerializeField] private Grid grid;
        [Tooltip("Pier_R0 (lies from top right to bottom left on screen).")]
        [SerializeField] private Sprite risingSprite;
        [Tooltip("Pier_R1 (lies from top left to bottom right on screen).")]
        [SerializeField] private Sprite fallingSprite;

        public bool HasDock { get; private set; }
        public Dock Dock { get; private set; }

        private void Update()
        {
            if (resort == null || !resort.IsReady)
                return;

            enabled = false;
            Build();
        }

        private void Build()
        {
            if (island == null || grid == null)
            {
                Debug.LogError("[Pier] Island or Grid is not assigned! Please assign them in the inspector.");
                return;
            }

            if (!DockLocator.TryFind(BeachCells(), IsWater, grid.GetCellCenterWorld, out Dock dock))
            {
                Debug.LogWarning("[Pier] No free beach with open water found, guests will arrive without the ferry.");
                return;
            }

            Dock = dock;
            HasDock = true;

            GameObject pier = new GameObject("Pier", typeof(SpriteRenderer));
            pier.transform.SetParent(transform, false);
            pier.transform.position = resort.FootprintToWorld(dock.PierOrigin, dock.PierSize);
            Vector3 seaward = grid.GetCellCenterWorld(dock.LandingCell + dock.Seaward) - grid.GetCellCenterWorld(dock.LandingCell);
            pier.GetComponent<SpriteRenderer>().sprite = seaward.x * seaward.y > 0f ? risingSprite : fallingSprite;
            ResortSorting.Apply(pier);
        }

        private IEnumerable<Vector3Int> BeachCells()
        {
            foreach (TileData tile in island.Tiles)
            {
                Vector3Int cell = CellOf(tile);
                if (tile.isLand && tile.shoreDistance == 1 && resort.IsFree(cell))
                    yield return cell;
            }
        }

        private bool IsWater(Vector3Int cell) => !island.TryGetTile(cell, out TileData tile) || !tile.isLand;

        private Vector3Int CellOf(TileData tile) => new Vector3Int(tile.x - island.MapWidth / 2, tile.y - island.MapHeight / 2, 0);
    }
}
