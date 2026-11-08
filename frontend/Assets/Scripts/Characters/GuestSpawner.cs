using System.Collections.Generic;
using UnityEngine;
using ResortIsland.Resort;
using ResortIsland.Waves;

namespace ResortIsland.Characters
{
    public class GuestSpawner : MonoBehaviour, IGuestSpawner
    {
        [SerializeField] private ResortManager resort;
        [SerializeField] private IslandGenerator island;
        [SerializeField] private Pier pier;
        [SerializeField] private GameObject[] guestPrefabs;

        public void Spawn(int count)
        {
            if (count <= 0)
                return;

            if (resort == null || island == null || guestPrefabs == null || guestPrefabs.Length == 0)
            {
                Debug.LogError("[GuestSpawner] Resort, Island or guest prefabs are not assigned! Please assign them in the inspector.");
                return;
            }

            Vector3Int landing = pier != null && pier.HasDock ? pier.Dock.LandingCell : Vector3Int.zero;
            List<Vector3Int> cells = NearestCells.Take(FreeCells(), landing, count);
            for (int i = 0; i < cells.Count; i++)
            {
                GameObject guest = Instantiate(guestPrefabs[i % guestPrefabs.Length], resort.CellToWorld(cells[i]), Quaternion.identity, transform);
                ResortSorting.Apply(guest);
            }
        }

        private IEnumerable<Vector3Int> FreeCells()
        {
            foreach (TileData tile in island.Tiles)
            {
                Vector3Int cell = new Vector3Int(tile.x - island.MapWidth / 2, tile.y - island.MapHeight / 2, 0);
                if (tile.isLand && resort.IsFree(cell))
                    yield return cell;
            }
        }
    }
}
