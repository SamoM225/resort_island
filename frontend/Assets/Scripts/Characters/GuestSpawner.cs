using System.Collections.Generic;
using UnityEngine;
using ResortIsland.Resort;
using ResortIsland.Waves;

namespace ResortIsland.Characters
{
    public class GuestSpawner : MonoBehaviour, IGuestSpawner, INeedAwareGuestSpawner
    {
        [SerializeField] private ResortManager resort;
        [SerializeField] private IslandGenerator island;
        [SerializeField] private Pier pier;
        [SerializeField] private GameObject[] guestPrefabs;

        public void Spawn(int count)
        {
            Spawn(count, null);
        }

        public void Spawn(int count, IReadOnlyList<NeedDemand> needs)
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
            List<string> demand = ExpandNeeds(needs);
            for (int i = 0; i < cells.Count; i++)
            {
                GameObject guest = Instantiate(guestPrefabs[i % guestPrefabs.Length], resort.CellToWorld(cells[i]), Quaternion.identity, transform);
                ResortSorting.Apply(guest);
                CharacterWander wander = guest.GetComponent<CharacterWander>();
                if (wander != null) wander.enabled = false;
                GuestAI ai = guest.GetComponent<GuestAI>();
                if (ai == null) ai = guest.AddComponent<GuestAI>();
                ai.Initialize(resort, pier, i < demand.Count ? demand[i] : null);
            }
        }

        private List<string> ExpandNeeds(IReadOnlyList<NeedDemand> needs)
        {
            var result = new List<string>();
            if (needs == null) return result;
            foreach (NeedDemand need in needs)
                if (need != null)
                    for (int i = 0; i < Mathf.Max(0, need.Units); i++)
                        result.Add(need.NeedId);
            return result;
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
