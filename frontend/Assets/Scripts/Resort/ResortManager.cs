using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;
using ResortIsland.Config;

namespace ResortIsland.Resort
{
    public class ResortManager : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private Grid grid;
        [SerializeField] private Tilemap groundTilemap;   

        [Header("Config")]
        [SerializeField] private ResortStartConfig startConfig;
        [SerializeField] private BuildingConfig[] catalog;   

        private readonly Dictionary<Vector3Int, PlacedBuilding> occupied = new Dictionary<Vector3Int, PlacedBuilding>();
        private readonly List<GameObject> workers = new List<GameObject>();
        private readonly List<NeedConfig> activeNeeds = new List<NeedConfig>();

        public bool IsReady { get; private set; }
        public BuildingConfig[] Catalog => catalog;
        public IReadOnlyList<NeedConfig> ActiveNeeds => activeNeeds;

        private string SavePath => Path.Combine(Application.persistentDataPath, "resort_save.json");

        private void Start()
        {
            if (grid == null || startConfig == null)
            {
                Debug.LogError("ResortManager: v Inspectore treba vyplniť Grid a Start Config.");
                return;
            }

            Debug.Log("Súbor uloženia: " + SavePath);
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

        public Vector3 CellToWorld(Vector3Int cell) => grid.GetCellCenterWorld(cell);

        public Vector3Int WorldToCell(Vector3 world) => grid.WorldToCell(world);

        public bool IsFree(Vector3Int cell)
        {
            if (occupied.ContainsKey(cell)) return false;
            if (groundTilemap != null && !groundTilemap.HasTile(cell)) return false;
            return true;
        }

        public bool TryGetBuilding(Vector3Int cell, out PlacedBuilding building)
            => occupied.TryGetValue(cell, out building);

        public int CountOf(BuildingConfig config)
        {
            int count = 0;
            foreach (var b in occupied.Values)
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
            if (!IsFree(cell))
            {
                Debug.Log("Budovu nemožno položiť: bunka " + cell + " je obsadená.");
                return false;
            }

            PlaceBuilding(config, cell);
            return true;
        }

        public PlacedBuilding PlaceBuilding(BuildingConfig config, Vector3Int cell, bool save = true)
        {
            if (config == null || config.Prefab == null)
            {
                Debug.LogError("PlaceBuilding: budova nemá priradený prefab.");
                return null;
            }
            if (occupied.ContainsKey(cell))
                Debug.LogWarning("PlaceBuilding: bunka " + cell + " je už obsadená.");

            var go = Instantiate(config.Prefab);
            var placed = go.GetComponent<PlacedBuilding>();
            if (placed == null) placed = go.AddComponent<PlacedBuilding>();

            placed.Init(config);
            placed.SetCell(cell, CellToWorld(cell));
            occupied[cell] = placed;

            if (save) SaveGame();
            return placed;
        }

        public void MoveBuilding(PlacedBuilding building, Vector3Int newCell)
        {
            occupied.Remove(building.Cell);
            building.SetCell(newCell, CellToWorld(newCell));
            occupied[newCell] = building;
            SaveGame();
        }

        public void StartNewGame()
        {
            ClearWorld();
            Unlocks.Reset();

            foreach (var sb in startConfig.StartingBuildings)
                if (sb.Building != null) PlaceBuilding(sb.Building, sb.Cell, false);

            SetupNeedsAndWorkers();
            SaveGame();

            Debug.Log($"Nová hra: {occupied.Count} budovy, {activeNeeds.Count} potreby, {workers.Count} pracovníci");
        }

        public void ResetGame()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            StartNewGame();
        }

        public void SaveGame()
        {
            var save = new ResortSave();
            foreach (var b in occupied.Values)
            {
                save.buildings.Add(new BuildingSave
                {
                    buildingId = b.Config.Id,
                    cellX = b.Cell.x,
                    cellY = b.Cell.y
                });
            }
            save.unlocks.AddRange(Unlocks.All);

            File.WriteAllText(SavePath, JsonUtility.ToJson(save, true));
        }

        private bool LoadGame()
        {
            if (!File.Exists(SavePath)) return false;

            ResortSave save;
            try { save = JsonUtility.FromJson<ResortSave>(File.ReadAllText(SavePath)); }
            catch { return false; }

            if (save == null || save.buildings.Count == 0) return false;

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
                PlaceBuilding(config, new Vector3Int(b.cellX, b.cellY, 0), false);
            }

            SetupNeedsAndWorkers();
            Debug.Log($"Načítaná hra: {occupied.Count} budov");
            return true;
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

            for (int i = 0; i < startConfig.StartingWorkers; i++)
            {
                if (startConfig.WorkerPrefab == null) break;

                var cell = startConfig.WorkerStartCell + new Vector3Int(i, -i, 0);
                var worker = Instantiate(startConfig.WorkerPrefab, CellToWorld(cell), Quaternion.identity);
                ResortSorting.Apply(worker);
                workers.Add(worker);
            }
        }

        private void ClearWorld()
        {
            foreach (var b in occupied.Values)
                if (b != null) Destroy(b.gameObject);
            occupied.Clear();

            foreach (var w in workers)
                if (w != null) Destroy(w);
            workers.Clear();
        }
    }
}