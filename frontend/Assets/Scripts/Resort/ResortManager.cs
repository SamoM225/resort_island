using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;
using ResortIsland.Config;
using ResortIsland.Save;

namespace ResortIsland.Resort
{
    public class ResortManager : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private Grid grid;
        [SerializeField] private Tilemap groundTilemap;
        [SerializeField] private IslandGenerator islandGenerator;

        [Header("Config")]
        [SerializeField] private ResortStartConfig startConfig;
        [SerializeField] private BuildingConfig[] catalog;

        // every cell of a footprint points to its building
        private readonly Dictionary<Vector3Int, PlacedBuilding> occupied = new Dictionary<Vector3Int, PlacedBuilding>();
        private readonly List<PlacedBuilding> buildings = new List<PlacedBuilding>();
        private readonly List<GameObject> workers = new List<GameObject>();
        private readonly List<NeedConfig> activeNeeds = new List<NeedConfig>();

        public bool IsReady { get; private set; }
        public BuildingConfig[] Catalog => catalog;
        public IReadOnlyList<NeedConfig> ActiveNeeds => activeNeeds;

        // The map used to have its own file; it now lives in the shared save (SaveStore). A legacy file is imported once.
        private string LegacySavePath => Path.Combine(Application.persistentDataPath, "resort_save.json");

        private SaveStore store;

        private void Start()
        {
            if (grid == null || startConfig == null)
            {
                Debug.LogError("ResortManager: v Inspectore treba vyplniť Grid a Start Config.");
                return;
            }

            store = SaveStore.Shared;
            if (!store.IsLoaded)
                Debug.LogError("ResortManager: uloženie nie je načítané (načíta ho PlayerEconomy), mapa sa nebude ukladať.");

            if (!LoadGame()) StartNewGame();

            Unlocks.OnUnlocked += OnUnlocked;
            IsReady = true;
        }

        private void OnDestroy() => Unlocks.OnUnlocked -= OnUnlocked;

        private void OnApplicationQuit()
        {
            if (IsReady) SaveGame();
        }

        private void OnUnlocked(string id) => SaveGame();

        private void Update()
        {
            if (IsReady && Input.GetKeyDown(KeyCode.F9)) ResetGame();
        }

        // Sand and grass slabs stand above the water level, so objects on them are lifted by that height.
        public Vector3 CellToWorld(Vector3Int cell)
        {
            Vector3 world = grid.GetCellCenterWorld(cell);
            if (islandGenerator != null) world.y += islandGenerator.GroundHeightAt(cell);
            return world;
        }

        /// World position of the footprint centre; the ResortEmpire sprites have their pivot there.
        public Vector3 FootprintToWorld(Vector3Int origin, Vector2Int size)
        {
            Vector3 local = grid.CellToLocalInterpolated(
                new Vector3(origin.x + size.x * 0.5f, origin.y + size.y * 0.5f, 0f));
            Vector3 world = grid.LocalToWorld(local);
            if (islandGenerator != null)
                world.y += islandGenerator.GroundHeightAt(origin + new Vector3Int(size.x / 2, size.y / 2, 0));
            return world;
        }

        /// Footprint origin that puts the given cell in the middle of the footprint.
        public Vector3Int OriginFor(Vector3Int cell, Vector2Int size)
            => cell - new Vector3Int(size.x / 2, size.y / 2, 0);

        public Vector3Int WorldToCell(Vector3 world) => grid.WorldToCell(world);

        public bool IsFree(Vector3Int cell) => IsAreaFree(cell, Vector2Int.one);

        /// Every cell of the footprint is on buildable land and not used by another building.
        public bool IsAreaFree(Vector3Int origin, Vector2Int size, PlacedBuilding ignore = null)
        {
            for (int y = 0; y < size.y; y++)
            for (int x = 0; x < size.x; x++)
            {
                var cell = new Vector3Int(origin.x + x, origin.y + y, 0);
                if (occupied.TryGetValue(cell, out var other) && other != ignore) return false;
                if (groundTilemap != null && !groundTilemap.HasTile(cell)) return false;
                if (islandGenerator != null &&
                    (!islandGenerator.TryGetTile(cell, out var tile) || !tile.isBuildable)) return false;
            }
            return true;
        }

        public bool TryGetBuilding(Vector3Int cell, out PlacedBuilding building)
            => occupied.TryGetValue(cell, out building);

        public int CountOf(BuildingConfig config)
        {
            int count = 0;
            foreach (var b in buildings)
                if (b.Config == config) count++;
            return count;
        }

        public bool CanBuild(BuildingConfig config)
            => config != null && Unlocks.Has(config.UnlockId) && CountOf(config) < config.MaxCount;

        public bool TryPlaceBuilding(BuildingConfig config, Vector3Int cell)
        {
            if (!CanBuild(config))
            {
                Debug.Log("Budovu nemožno položiť: nie je odomknutá alebo už dosiahla limit.");
                return false;
            }
            if (!IsAreaFree(cell, config.Footprint))
            {
                Debug.Log("Budovu nemožno položiť: bunka " + cell + " je obsadená.");
                return false;
            }

            PlaceBuilding(config, cell);
            return true;
        }

        /// origin = lowest x/y cell of the building footprint.
        public PlacedBuilding PlaceBuilding(BuildingConfig config, Vector3Int origin, bool save = true)
        {
            if (config == null || config.Prefab == null)
            {
                Debug.LogError("PlaceBuilding: budova nemá priradený prefab.");
                return null;
            }
            if (!IsAreaFree(origin, config.Footprint))
                Debug.LogWarning("PlaceBuilding: oblasť od bunky " + origin + " je obsadená.");

            var go = Instantiate(config.Prefab);
            var placed = go.GetComponent<PlacedBuilding>();
            if (placed == null) placed = go.AddComponent<PlacedBuilding>();

            placed.Init(config);
            Register(placed, origin);
            buildings.Add(placed);

            if (save) SaveGame();
            return placed;
        }

        public void MoveBuilding(PlacedBuilding building, Vector3Int newOrigin)
        {
            Unregister(building);
            Register(building, newOrigin);
            SaveGame();
        }

        /// Snaps a building back to its stored position (e.g. after a cancelled drag).
        public void RefreshPosition(PlacedBuilding building)
            => building.SetCell(building.Cell, FootprintToWorld(building.Cell, building.Footprint));

        private void Register(PlacedBuilding building, Vector3Int origin)
        {
            Vector2Int size = building.Footprint;
            building.SetCell(origin, FootprintToWorld(origin, size));
            for (int y = 0; y < size.y; y++)
            for (int x = 0; x < size.x; x++)
                occupied[new Vector3Int(origin.x + x, origin.y + y, 0)] = building;
            if (islandGenerator != null) islandGenerator.TrySetOccupied(origin, size, true);
        }

        private void Unregister(PlacedBuilding building)
        {
            Vector2Int size = building.Footprint;
            for (int y = 0; y < size.y; y++)
            for (int x = 0; x < size.x; x++)
            {
                var cell = new Vector3Int(building.Cell.x + x, building.Cell.y + y, 0);
                if (occupied.TryGetValue(cell, out var other) && other == building) occupied.Remove(cell);
            }
            if (islandGenerator != null) islandGenerator.TrySetOccupied(building.Cell, size, false);
        }

        public void StartNewGame()
        {
            ClearWorld();
            Unlocks.Reset();

            foreach (var sb in startConfig.StartingBuildings)
            {
                if (sb.Building == null) continue;

                // The island generator decides where the building goes; the configured cell is the fallback.
                Vector3Int cell = sb.Cell;
                if (sb.AutoPlace && islandGenerator != null)
                {
                    if (islandGenerator.TryFindSite(sb.Building.Footprint, sb.Building.PreferShore, out var site))
                        cell = site;
                    else
                        Debug.LogWarning("Na ostrove sa nenašlo miesto pre " + sb.Building.Id + ", použije sa bunka " + cell);
                }
                PlaceBuilding(sb.Building, cell, false);
            }

            SetupNeedsAndWorkers();
            SaveGame();

            Debug.Log($"Nová hra: {buildings.Count} budovy, {activeNeeds.Count} potreby, {workers.Count} pracovníci");
        }

        public void ResetGame()
        {
            if (File.Exists(LegacySavePath)) File.Delete(LegacySavePath);
            if (store != null && store.IsLoaded) store.Data.resort = new ResortSave();
            StartNewGame();
        }

        public void SaveGame()
        {
            if (store == null || !store.IsLoaded) return;

            var save = new ResortSave();
            foreach (var b in buildings)
            {
                save.buildings.Add(new BuildingSave
                {
                    buildingId = b.Config.Id,
                    cellX = b.Cell.x,
                    cellY = b.Cell.y
                });
            }
            save.unlocks.AddRange(Unlocks.All);

            store.Data.resort = save;
            try { store.Save(); }
            catch (System.Exception e) { Debug.LogError("Uloženie mapy zlyhalo: " + e.Message); }
        }

        private bool LoadGame()
        {
            ResortSave save = store != null && store.IsLoaded ? store.Data.resort : null;
            bool fromLegacyFile = false;
            if ((save == null || save.buildings.Count == 0) && TryReadLegacySave(out var legacy))
            {
                save = legacy;
                fromLegacyFile = true;
            }

            if (save == null || save.buildings == null || save.buildings.Count == 0) return false;

            ClearWorld();
            Unlocks.Load(save.unlocks);

            foreach (var b in save.buildings)
            {
                var config = FindConfig(b.buildingId);
                if (config == null)
                {
                    Debug.LogWarning("Neznáma budova v uložení: " + b.buildingId);
                    continue;
                }

                var origin = new Vector3Int(b.cellX, b.cellY, 0);
                if (!IsAreaFree(origin, config.Footprint))
                {
                    // e.g. a save from before buildings had footprints or from a different island layout
                    Debug.LogWarning("Uložená budova " + b.buildingId + " nepasuje na ostrov, začína sa nová hra.");
                    ClearWorld();
                    return false;
                }
                PlaceBuilding(config, origin, false);
            }

            SetupNeedsAndWorkers();
            Debug.Log($"Načítaná hra: {buildings.Count} budov");

            if (fromLegacyFile && store != null && store.IsLoaded)
            {
                SaveGame();
                RetireLegacySave();
                Debug.Log("Mapa prenesená zo starého resort_save.json do spoločného uloženia.");
            }
            return true;
        }

        private bool TryReadLegacySave(out ResortSave legacy)
        {
            legacy = null;
            if (!File.Exists(LegacySavePath)) return false;

            try { legacy = JsonUtility.FromJson<ResortSave>(File.ReadAllText(LegacySavePath)); }
            catch { return false; }

            return legacy != null;
        }

        // Kept as .imported instead of deleted, in case the import has to be checked.
        private void RetireLegacySave()
        {
            string imported = LegacySavePath + ".imported";
            try
            {
                if (File.Exists(imported)) File.Delete(imported);
                File.Move(LegacySavePath, imported);
            }
            catch (IOException e) { Debug.LogWarning("Starý resort_save.json sa nepodarilo premenovať: " + e.Message); }
        }

        private BuildingConfig FindConfig(string id)
        {
            foreach (var c in catalog)
                if (c != null && c.Id == id) return c;
            return null;
        }

        private void SetupNeedsAndWorkers()
        {
            activeNeeds.Clear();
            activeNeeds.AddRange(startConfig.ActiveNeeds);

            // workers start on free land close to the centre of the generated island
            var spawnCells = new List<Vector3Int>();
            if (islandGenerator != null)
                islandGenerator.TryFindFreeCells(startConfig.StartingWorkers, spawnCells);

            for (int i = 0; i < startConfig.StartingWorkers; i++)
            {
                if (startConfig.WorkerPrefab == null) break;

                var cell = i < spawnCells.Count
                    ? spawnCells[i]
                    : startConfig.WorkerStartCell + new Vector3Int(i, -i, 0);
                var worker = Instantiate(startConfig.WorkerPrefab, CellToWorld(cell), Quaternion.identity);
                ResortSorting.Apply(worker);
                workers.Add(worker);
            }
        }

        private void ClearWorld()
        {
            foreach (var b in buildings)
            {
                if (b == null) continue;
                Unregister(b);
                Destroy(b.gameObject);
            }
            buildings.Clear();
            occupied.Clear();

            foreach (var w in workers)
                if (w != null) Destroy(w);
            workers.Clear();
        }
    }
}
