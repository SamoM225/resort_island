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

        [Header("Paths")]
        [Tooltip("Its landing cell is the start of the main path. Found automatically when empty.")]
        [SerializeField] private Pier pier;
        [Tooltip("Path styles: look, price, unlock wave.")]
        [SerializeField] private PathStyleCatalog pathStyles;

        // every cell of a footprint points to its building
        private readonly Dictionary<Vector3Int, PlacedBuilding> occupied = new Dictionary<Vector3Int, PlacedBuilding>();
        private readonly List<PlacedBuilding> buildings = new List<PlacedBuilding>();
        private readonly List<GameObject> workers = new List<GameObject>();
        private readonly List<NeedConfig> activeNeeds = new List<NeedConfig>();
        private readonly List<PathFootprint> footprints = new List<PathFootprint>();

        private PathNetwork paths;
        // paths are generated once the pier (start of the main path) exists
        private bool pathsReady;
        private bool pathsGenerated;
        // cells before the running path edit; null when no edit is running
        private Dictionary<Vector3Int, PathCell> pathEditSnapshot;

        public bool IsReady { get; private set; }
        public BuildingConfig[] Catalog => catalog;
        public IReadOnlyList<NeedConfig> ActiveNeeds => activeNeeds;
        public PathNetwork Paths => paths;
        public bool PathsReady => pathsReady;
        public PathStyleCatalog PathStyles => pathStyles;

        // The map used to have its own file; it now lives in the shared save (SaveStore). A legacy file is imported once.
        private string LegacySavePath => Path.Combine(Application.persistentDataPath, "resort_save.json");

        private SaveStore store;

        private void Awake()
        {
            paths = new PathNetwork(IsPathGround);
            // works without the scene reference too (Assets/Resources/PathStyles.asset)
            if (pathStyles == null) pathStyles = Resources.Load<PathStyleCatalog>("PathStyles");
            if (pathStyles != null) paths.DefaultStyle = pathStyles.DefaultStyleId;
            else Debug.LogError("ResortManager: path styles not found (Resources/PathStyles), paths will be invisible.");
        }

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

            if (pier == null) pier = FindFirstObjectByType<Pier>();

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
            if (!IsReady) return;
            if (!pathsReady && (pier == null || pier.IsBuilt)) InitPaths();
            if (Input.GetKeyDown(KeyCode.F9)) ResetGame();
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

        /// Free area that covers neither the main path (the pier landing) nor another building's entrance.
        public bool CanPlaceBuilding(Vector3Int origin, Vector2Int size, PlacedBuilding ignore = null)
        {
            if (!IsAreaFree(origin, size, ignore)) return false;
            for (int y = 0; y < size.y; y++)
            for (int x = 0; x < size.x; x++)
            {
                var cell = new Vector3Int(origin.x + x, origin.y + y, 0);
                if (paths.IsMain(cell) || IsEntranceOfOther(cell, ignore)) return false;
            }
            return true;
        }

        /// The building fits in this rotation and the cell in front of its door is free land.
        public bool CanPlaceBuilding(BuildingConfig config, Vector3Int origin, int rotation, PlacedBuilding ignore = null)
        {
            if (config == null) return false;
            Vector2Int size = BuildingRotation.Size(config.Footprint, rotation);
            return CanPlaceBuilding(origin, size, ignore) &&
                   IsUsableEntrance(BuildingRotation.EntranceCell(origin, config.Footprint, config.Entrance, rotation), ignore);
        }

        /// A guest can stand there: free buildable land (a path may lie on it).
        public bool IsUsableEntrance(Vector3Int cell, PlacedBuilding ignore = null) => IsAreaFree(cell, Vector2Int.one, ignore);

        private bool IsEntranceOfOther(Vector3Int cell, PlacedBuilding ignore)
        {
            foreach (var b in buildings)
                if (b != ignore && b.EntranceCell == cell) return true;
            return false;
        }

        /// Rotation whose entrance is usable and closest to the target (e.g. the island centre); fallback if none fits.
        public int BestRotation(BuildingConfig config, Vector3Int origin, Vector3Int towards, PlacedBuilding ignore = null, int fallback = 0)
        {
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int r = 0; r < BuildingRotation.Count; r++)
            {
                if (!CanPlaceBuilding(config, origin, r, ignore)) continue;
                float distance = (BuildingRotation.EntranceCell(origin, config.Footprint, config.Entrance, r) - towards).sqrMagnitude;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = r;
            }
            return best < 0 ? fallback : best;
        }

        public IReadOnlyList<PlacedBuilding> Buildings => buildings;

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

        public bool TryPlaceBuilding(BuildingConfig config, Vector3Int cell, int rotation = 0)
        {
            if (!CanBuild(config))
            {
                Debug.Log("Budovu nemožno položiť: nie je odomknutá alebo už dosiahla limit.");
                return false;
            }
            if (!CanPlaceBuilding(config, cell, rotation))
            {
                Debug.Log("Budovu nemožno položiť: bunka " + cell + " je obsadená alebo vchod nevedie na voľnú zem.");
                return false;
            }

            var connected = ConnectedBuildings();
            var placed = PlaceBuilding(config, cell, rotation, false);
            if (placed == null) return false;

            UpdatePaths(connected, placed);
            SaveGame();
            return true;
        }

        /// origin = lowest x/y cell of the building footprint.
        public PlacedBuilding PlaceBuilding(BuildingConfig config, Vector3Int origin, int rotation = 0, bool save = true)
        {
            if (config == null || config.Prefab == null)
            {
                Debug.LogError("PlaceBuilding: budova nemá priradený prefab.");
                return null;
            }
            if (!IsAreaFree(origin, BuildingRotation.Size(config.Footprint, rotation)))
                Debug.LogWarning("PlaceBuilding: oblasť od bunky " + origin + " je obsadená.");

            var go = Instantiate(config.Prefab);
            var placed = go.GetComponent<PlacedBuilding>();
            if (placed == null) placed = go.AddComponent<PlacedBuilding>();

            placed.Init(config);
            placed.SetRotation(rotation);
            Register(placed, origin);
            buildings.Add(placed);

            if (save) SaveGame();
            return placed;
        }

        /// The building's generated path is deleted and generated again from the new position;
        /// buildings whose path the move cut off are reconnected.
        public void MoveBuilding(PlacedBuilding building, Vector3Int newOrigin) => MoveBuilding(building, newOrigin, building.Rotation);

        /// Moves and / or turns a building (the caller checked CanPlaceBuilding).
        public void MoveBuilding(PlacedBuilding building, Vector3Int newOrigin, int rotation)
        {
            var connected = ConnectedBuildings();
            Unregister(building);
            building.SetRotation(rotation);
            Register(building, newOrigin);
            UpdatePaths(connected, building);
            SaveGame();
        }

        /// Turns a building in place to the next rotation whose footprint fits. The entrance may end up
        /// blocked (check IsUsableEntrance): the player decides, paths simply cannot reach a blocked door.
        public bool TryRotateBuilding(PlacedBuilding building)
        {
            if (building == null) return false;
            for (int step = 1; step < BuildingRotation.Count; step++)
            {
                int rotation = BuildingRotation.Normalize(building.Rotation + step);
                Vector2Int size = BuildingRotation.Size(building.Config.Footprint, rotation);
                if (!CanPlaceBuilding(building.Cell, size, building)) continue;
                MoveBuilding(building, building.Cell, rotation);
                return true;
            }
            return false;
        }

        /// Rotation that turns the door towards the nearest path (towards the island centre when there
        /// are no paths yet), among the rotations that fit with a usable entrance.
        public int AutoRotation(BuildingConfig config, Vector3Int origin, PlacedBuilding ignore = null, int fallback = 0)
        {
            Vector2Int size = config.Footprint;
            Vector3 centre = origin + new Vector3(size.x * 0.5f, size.y * 0.5f, 0f);
            Vector3Int towards = Vector3Int.zero;
            float best = float.MaxValue;
            foreach (var pair in paths.Cells)
            {
                // the building's own generated path is about to move with it
                if (ignore != null && pair.Value.Kind == PathKind.Auto && Equals(pair.Value.Owner, ignore)) continue;
                float distance = (pair.Key - centre).sqrMagnitude;
                if (distance >= best) continue;
                best = distance;
                towards = pair.Key;
            }
            return BestRotation(config, origin, towards, ignore, fallback);
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

        // ---- paths ------------------------------------------------------------------------------

        public bool CanBuildPath(Vector3Int cell) => pathsReady && paths.CanAdd(cell);
        public bool CanRemovePath(Vector3Int cell) => pathsReady && paths.CanRemove(cell);

        /// Lays a manual path cell. The caller saves the game when the stroke ends.
        public bool TryBuildPath(Vector3Int cell, string style = null, int level = 1)
            => pathsReady && paths.AddManual(cell, style, ClampLevel(style, level));

        /// Removes a generated or manual path cell; it is not generated again on its own.
        public bool TryRemovePath(Vector3Int cell) => pathsReady && paths.Remove(cell);

        public bool CanRestylePath(Vector3Int cell, string style)
            => pathsReady && paths.TryGet(cell, out var c) && c.Style != style;

        /// Gives an existing path another style; its level is kept where the new style has it.
        public bool TryRestylePath(Vector3Int cell, string style)
            => CanRestylePath(cell, style) && paths.TryGet(cell, out var c) &&
               paths.SetLook(cell, style, ClampLevel(style, c.Level));

        public bool CanUpgradePath(Vector3Int cell, string style, int level)
            => pathsReady && paths.TryGet(cell, out var c) && c.Style == style && c.Level != ClampLevel(style, level);

        /// Sets the level of a path of the given style.
        public bool TryUpgradePath(Vector3Int cell, string style, int level)
            => CanUpgradePath(cell, style, level) && paths.SetLook(cell, style, ClampLevel(style, level));

        private int ClampLevel(string style, int level)
        {
            PathStyle found = pathStyles != null ? pathStyles.Find(style) : null;
            return found != null ? found.ClampLevel(level) : Mathf.Max(1, level);
        }

        // ---- path edit session (the path editor's X / Done) ---------------------------------

        public bool IsEditingPaths => pathEditSnapshot != null;

        /// Coins the changes made since BeginPathEdit cost.
        public int PendingPathCost => pathEditSnapshot == null || pathStyles == null ? 0
            : PathNetwork.ChangeCost(pathEditSnapshot, paths.Cells, pathStyles.CostPerTile);

        public void BeginPathEdit()
        {
            if (!IsEditingPaths) pathEditSnapshot = paths.Snapshot();
        }

        /// Keeps the changes (the caller has paid PendingPathCost).
        public void CommitPathEdit()
        {
            if (!IsEditingPaths) return;
            pathEditSnapshot = null;
            SaveGame();
        }

        /// Throws the changes away.
        public void CancelPathEdit()
        {
            if (!IsEditingPaths) return;
            var snapshot = pathEditSnapshot;
            pathEditSnapshot = null;
            paths.RestoreSnapshot(snapshot);
        }

        // free buildable land without a building
        private bool IsPathGround(Vector3Int cell) => IsAreaFree(cell, Vector2Int.one);

        private void InitPaths()
        {
            pathsReady = true;
            if (pier != null && pier.HasDock)
                paths.SetMain(pier.Dock.LandingCell, PathFinder.DirectionBit(pier.Dock.Seaward));

            if (!pathsGenerated)
            {
                GenerateAllPaths();
                SaveGame();
            }
        }

        private void GenerateAllPaths()
        {
            var all = new HashSet<object>();
            foreach (var b in buildings) all.Add(b);
            paths.Reconnect(Footprints(), all);
            pathsGenerated = true;
        }

        private HashSet<object> ConnectedBuildings()
            => pathsReady ? paths.ConnectedOwners(Footprints()) : null;

        private void UpdatePaths(HashSet<object> connectedBefore, PlacedBuilding rebuild)
        {
            if (pathsReady) paths.Reconnect(Footprints(), connectedBefore, rebuild);
        }

        private List<PathFootprint> Footprints()
        {
            footprints.Clear();
            foreach (var b in buildings)
                footprints.Add(new PathFootprint(b, b.Cell, b.Footprint, b.EntranceCell));
            return footprints;
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
                // door towards the middle of the island, never into the water
                PlaceBuilding(sb.Building, cell, BestRotation(sb.Building, cell, Vector3Int.zero), false);
            }

            SetupNeedsAndWorkers();
            if (pathsReady) GenerateAllPaths();
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
                    cellY = b.Cell.y,
                    rotation = b.Rotation
                });
            }
            save.buildingVersion = BuildingVersion;
            save.unlocks.AddRange(Unlocks.All);

            save.pathsGenerated = pathsGenerated;
            save.pathMaskVersion = PathMaskVersion;
            save.pathStyleVersion = PathStyleVersion;
            // an unfinished (unpaid) path edit is not saved
            foreach (var pair in pathEditSnapshot ?? paths.Cells)
            {
                if (pair.Value.Kind == PathKind.Main) continue;
                save.paths.Add(new PathSave
                {
                    cellX = pair.Key.x,
                    cellY = pair.Key.y,
                    kind = (int)pair.Value.Kind,
                    owner = pair.Value.Owner is PlacedBuilding owner ? buildings.IndexOf(owner) : -1,
                    entry = pair.Value.EntryMask,
                    style = pair.Value.Style,
                    level = pair.Value.Level
                });
            }

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

            // index = position in save.buildings (path owners refer to it), null = skipped
            var loaded = new List<PlacedBuilding>();
            foreach (var b in save.buildings)
            {
                var config = FindConfig(b.buildingId);
                if (config == null)
                {
                    Debug.LogWarning("Neznáma budova v uložení: " + b.buildingId);
                    loaded.Add(null);
                    continue;
                }

                var origin = new Vector3Int(b.cellX, b.cellY, 0);
                int rotation = BuildingRotation.Normalize(b.rotation);
                if (!IsAreaFree(origin, BuildingRotation.Size(config.Footprint, rotation)))
                {
                    // e.g. a save from before buildings had footprints or from a different island layout
                    Debug.LogWarning("Uložená budova " + b.buildingId + " nepasuje na ostrov, začína sa nová hra.");
                    ClearWorld();
                    return false;
                }
                // saves from before rotations: turn the door towards the island centre
                if (save.buildingVersion < BuildingVersion || !CanPlaceBuilding(config, origin, rotation))
                    rotation = BestRotation(config, origin, Vector3Int.zero, null, rotation);
                loaded.Add(PlaceBuilding(config, origin, rotation, false));
            }

            LoadPaths(save, loaded);
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
        
        private void LoadPaths(ResortSave save, List<PlacedBuilding> loaded)
        {
            pathsGenerated = save.pathsGenerated;
            if (save.paths == null) return;

            // generated paths of saves from before entrances led to any side: build them again to the doors
            bool regenerate = save.buildingVersion < BuildingVersion;
            if (regenerate) pathsGenerated = false;

            foreach (var p in save.paths)
            {
                var kind = (PathKind)p.kind;
                if (regenerate && kind == PathKind.Auto) continue;
                PlacedBuilding owner = p.owner >= 0 && p.owner < loaded.Count ? loaded[p.owner] : null;
                // a generated path whose building is gone stays as an ordinary path
                if (kind == PathKind.Auto && owner == null) kind = PathKind.Manual;
                int entry = save.pathMaskVersion >= PathMaskVersion ? p.entry : ConvertOldMask(p.entry);
                string style = kind == PathKind.Auto && save.pathStyleVersion < PathStyleVersion ? null : p.style;
                paths.Restore(new Vector3Int(p.cellX, p.cellY, 0), kind, owner, entry, style, p.level);
            }
        }

        private const int PathMaskVersion = 1;
        private const int PathStyleVersion = 1;
        // 1 = buildings have a rotation and an entrance
        private const int BuildingVersion = 1;

        // version 0 stored 1 = -x, 2 = +x, 4 = -y, 8 = +y
        private static int ConvertOldMask(int old)
        {
            int mask = 0;
            if ((old & 1) != 0) mask |= PathFinder.DirectionBit(Vector3Int.left);
            if ((old & 2) != 0) mask |= PathFinder.DirectionBit(Vector3Int.right);
            if ((old & 4) != 0) mask |= PathFinder.DirectionBit(Vector3Int.down);
            if ((old & 8) != 0) mask |= PathFinder.DirectionBit(Vector3Int.up);
            return mask;
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
            pathEditSnapshot = null;
            paths.Clear();
            pathsGenerated = false;

            foreach (var w in workers)
                if (w != null) Destroy(w);
            workers.Clear();
        }
    }
}
