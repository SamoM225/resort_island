using System.Collections.Generic;
using UnityEngine;
using ResortIsland.Resort;

namespace ResortIsland.Characters
{
    /// Spawns test guests (and optionally VIPs) on the island so the character art can be checked.
    public class CharacterDebug : MonoBehaviour
    {
        [SerializeField] private ResortManager resort;
        [SerializeField] private IslandGenerator island;
        [SerializeField] private GameObject[] guestPrefabs;
        [SerializeField] private GameObject[] vipPrefabs;
        [SerializeField, Min(0)] private int guestCount = 6;
        [SerializeField, Min(0)] private int vipCount = 1;
        [SerializeField] private bool spawnOnStart = true;

        private readonly List<GameObject> spawned = new List<GameObject>();
        private bool pending;

        private void Start() => pending = spawnOnStart;

        private void Update()
        {
            if (pending && resort != null && resort.IsReady)
            {
                pending = false;
                SpawnTestCharacters();
            }
        }

        [ContextMenu("Spawn test characters")]
        private void SpawnTestCharacters()
        {
            if (resort == null || !resort.IsReady || island == null)
            {
                Debug.LogWarning("Najprv spusti Play a vyplň polia Resort a Island.");
                return;
            }

            foreach (var go in spawned)
                if (go != null) Destroy(go);
            spawned.Clear();

            // the cells nearest to the island centre are taken by the starting workers, so skip those
            var cells = new List<Vector3Int>();
            island.TryFindFreeCells(guestCount + vipCount + 4, cells);
            int next = 4;

            for (int i = 0; i < guestCount; i++) Spawn(guestPrefabs, i, cells, ref next);
            for (int i = 0; i < vipCount; i++) Spawn(vipPrefabs, i, cells, ref next);
        }

        private void Spawn(GameObject[] prefabs, int index, List<Vector3Int> cells, ref int next)
        {
            if (prefabs == null || prefabs.Length == 0 || next >= cells.Count) return;

            GameObject prefab = prefabs[index % prefabs.Length];
            if (prefab == null) return;

            GameObject go = Instantiate(prefab, resort.CellToWorld(cells[next++]), Quaternion.identity);
            ResortSorting.Apply(go);
            spawned.Add(go);
        }
    }
}
