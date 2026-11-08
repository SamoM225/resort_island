using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResortIsland.Resort
{
    public enum TerrainType
    {
        Water,
        Sand,
        Grass
    }

    [Serializable]
    public sealed class TileData
    {
        public int x;
        public int y;
        public TerrainType terrainType;
        public bool isLand;
        public bool isShallow;
        /// Steps (4-neighbourhood) to the nearest water tile; 0 for water, 1 for the beach edge.
        public int shoreDistance;
        public bool isBuildable;
        public bool isOccupied;
        public float elevation;

        public TileData(int x, int y)
        {
            this.x = x;
            this.y = y;
            terrainType = TerrainType.Water;
        }
    }

    [Serializable]
    public struct TerrainProperties
    {
        public TerrainType terrainType;
        public bool isBuildable;
        public int elevationLevel;

        public TerrainProperties(TerrainType terrainType, bool isBuildable, int elevationLevel)
        {
            this.terrainType = terrainType;
            this.isBuildable = isBuildable;
            this.elevationLevel = elevationLevel;
        }
    }

    /// Generates the logical island first, then draws it with the ResortEmpire terrain sprites.
    /// TileData is the authoritative source used by gameplay systems.
    /// Tiles are drawn exactly on the cells of the scene Grid (isometric layout), so buildings
    /// placed through ResortManager line up with the generated terrain. The mesh is generated
    /// in edit mode too (ExecuteAlways), so the island is visible in the Scene view.
    [ExecuteAlways]
    public sealed class IslandGenerator : MonoBehaviour
    {
        /// Height of the sand / grass surface above the water level (see ResortEmpire/Data/SpriteCatalog.json).
        public const float SandHeight = 0.0735f;
        public const float GrassHeight = 0.147f;

        private const string RenderObjectName = "GeneratedIsland";
        private const string SortingLayerName = "Ground";

        [Header("Grid")]
        [Tooltip("Isometric Grid the island is drawn on. Found automatically when empty.")]
        [SerializeField] private Grid grid;
        [SerializeField, Min(1)] private int mapWidth = 32;
        [SerializeField, Min(1)] private int mapHeight = 32;
        [SerializeField] private int seed = 1337;

        [Header("Island shape")]
        [SerializeField, Range(0.1f, 0.49f)] private float islandRadius = 0.40f;
        [SerializeField, Range(0f, 0.2f)] private float coastlineIrregularity = 0.07f;
        [SerializeField, Min(0.01f)] private float noiseScale = 1.8f;
        [SerializeField, Min(0f)] private float beachWidth = 0.055f;
        [SerializeField] private bool sandBuildable = true;
        [SerializeField] private bool grassBuildable = true;
        [Tooltip("Water this many cells from the shore (or closer) is drawn as shallow water.")]
        [SerializeField, Min(0)] private int shallowRange = 2;

        [Header("Terrain sprites (ResortEmpire/Sprites/Terrain/Terrain.png)")]
        [Tooltip("Variants R0-R3 of each surface; a variant is picked per cell from the seed.")]
        [SerializeField] private Sprite[] deepWaterSprites;
        [SerializeField] private Sprite[] shallowWaterSprites;
        [SerializeField] private Sprite[] sandSprites;
        [SerializeField] private Sprite[] grassSprites;
        [Tooltip("Also draw water tiles.")]
        [SerializeField] private bool drawWater = true;
        [Tooltip("Deep water drawn this many cells beyond the map edge, so the sea reaches past the screen. Visual only.")]
        [SerializeField, Min(0)] private int waterMargin = 24;

        [Header("Debug")]
        [Tooltip("Generate when the component is enabled (also in edit mode).")]
        [SerializeField] private bool generateOnStart = true;
        [SerializeField] private bool showGrid;
        [Tooltip("Tint buildable cells green.")]
        [SerializeField] private bool highlightBuildable;

        private readonly List<TileData> tiles = new List<TileData>();
        private readonly List<TileData> waterLayer = new List<TileData>();
        private readonly List<TileData> sandLayer = new List<TileData>();
        private readonly List<TileData> grassLayer = new List<TileData>();
        private TileData[,] tileLookup;
        private GameObject renderObject;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh generatedMesh;
        private Material generatedMaterial;
        private bool regenerateQueued;

        private static readonly Color32 White = new Color32(255, 255, 255, 255);
        private static readonly Color32 BuildableTint = new Color32(190, 255, 190, 255);

        public int MapWidth => mapWidth;
        public int MapHeight => mapHeight;
        public int Seed => seed;
        public IReadOnlyList<TileData> Tiles => tiles;
        public IReadOnlyList<TileData> WaterLayer => waterLayer;
        public IReadOnlyList<TileData> SandLayer => sandLayer;
        public IReadOnlyList<TileData> GrassLayer => grassLayer;

        private void OnEnable()
        {
            if (generateOnStart) Generate();
        }

        private void OnDisable() => DestroyRendering();

#if UNITY_EDITOR
        // Regenerate when an Inspector value changes. Creating objects inside OnValidate is not
        // allowed, so the work is deferred to the editor's next update.
        private void OnValidate()
        {
            if (regenerateQueued) return;
            regenerateQueued = true;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                regenerateQueued = false;
                if (this != null && isActiveAndEnabled && generateOnStart) Generate();
            };
        }
#endif

        [ContextMenu("Generate island")]
        public void Generate()
        {
            if (!ResolveGrid()) return;

            mapWidth = Mathf.Max(1, mapWidth);
            mapHeight = Mathf.Max(1, mapHeight);
            tileLookup = new TileData[mapWidth, mapHeight];
            ClearLayers();

            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    var tile = new TileData(x, y);
                    // Offsets are measured from the centre cell (0,0) of the Grid.
                    float nx = (x - mapWidth / 2) / (float)mapWidth;
                    float ny = (y - mapHeight / 2) / (float)mapHeight;
                    float distance = Mathf.Sqrt(nx * nx + ny * ny);
                    float shapeNoise = (Mathf.PerlinNoise((x + seed * 0.37f) / mapWidth * noiseScale,
                        (y - seed * 0.19f) / mapHeight * noiseScale) - 0.5f) * coastlineIrregularity;
                    float edge = islandRadius + shapeNoise - distance;
                    float elevationNoise = Mathf.PerlinNoise((x + seed * 0.11f) / mapWidth * noiseScale * 1.35f,
                        (y + seed * 0.23f) / mapHeight * noiseScale * 1.35f);
                    float centerBias = Mathf.Clamp01(1f - distance / Mathf.Max(islandRadius, 0.001f));

                    tile.elevation = Mathf.Clamp01(elevationNoise * 0.75f + centerBias * 0.25f);
                    tile.terrainType = ResolveTerrain(edge);
                    tile.isLand = tile.terrainType != TerrainType.Water;
                    tile.isBuildable = IsTerrainBuildable(tile.terrainType);
                    tileLookup[x, y] = tile;
                    tiles.Add(tile);
                }
            }

            MarkShallowWater();
            ComputeShoreDistance();
            foreach (TileData tile in tiles) LayerFor(tile.terrainType).Add(tile);

            BuildMesh();
            Validate();
        }

        private bool ResolveGrid()
        {
            if (grid == null) grid = FindFirstObjectByType<Grid>();
            if (grid != null) return true;
            Debug.LogError("IslandGenerator: no Grid found, assign an isometric Grid in the Inspector.");
            return false;
        }

        private TerrainType ResolveTerrain(float edge)
        {
            if (edge <= 0f) return TerrainType.Water;
            if (edge < beachWidth) return TerrainType.Sand;
            return TerrainType.Grass;
        }

        // Water within shallowRange cells of any land is shallow, the rest is deep.
        private void MarkShallowWater()
        {
            foreach (TileData tile in tiles)
            {
                if (tile.isLand) continue;
                for (int dy = -shallowRange; dy <= shallowRange && !tile.isShallow; dy++)
                for (int dx = -shallowRange; dx <= shallowRange; dx++)
                {
                    if (TryGetTile(tile.x + dx, tile.y + dy, out var other) && other.isLand)
                    {
                        tile.isShallow = true;
                        break;
                    }
                }
            }
        }

        // Multi-source BFS from all water tiles; the outside of the map counts as water.
        private void ComputeShoreDistance()
        {
            var queue = new Queue<TileData>();
            foreach (TileData tile in tiles)
            {
                tile.shoreDistance = tile.isLand ? int.MaxValue : 0;
                if (!tile.isLand) queue.Enqueue(tile);
            }

            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };
            while (queue.Count > 0)
            {
                TileData current = queue.Dequeue();
                for (int i = 0; i < 4; i++)
                {
                    if (TryGetTile(current.x + dx[i], current.y + dy[i], out var next) &&
                        next.shoreDistance == int.MaxValue)
                    {
                        next.shoreDistance = current.shoreDistance + 1;
                        queue.Enqueue(next);
                    }
                }
            }

            foreach (TileData tile in tiles)
            {
                bool onMapEdge = tile.x == 0 || tile.y == 0 || tile.x == mapWidth - 1 || tile.y == mapHeight - 1;
                if (tile.isLand && onMapEdge) tile.shoreDistance = Mathf.Min(tile.shoreDistance, 1);
            }
        }

        private List<TileData> LayerFor(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.Sand: return sandLayer;
                case TerrainType.Grass: return grassLayer;
                default: return waterLayer;
            }
        }

        private bool IsTerrainBuildable(TerrainType terrain)
        {
            if (terrain == TerrainType.Sand) return sandBuildable;
            return terrain == TerrainType.Grass && grassBuildable;
        }

        public bool TryGetTile(int x, int y, out TileData tile)
        {
            if (tileLookup != null && x >= 0 && x < mapWidth && y >= 0 && y < mapHeight)
            {
                tile = tileLookup[x, y];
                return true;
            }
            tile = null;
            return false;
        }

        public bool TryGetTile(Vector3Int coordinate, out TileData tile)
        {
            // ResortManager uses the centered Grid coordinates, while the
            // public integer API remains a conventional 0..width/height coordinate.
            return TryGetTile(coordinate.x + mapWidth / 2, coordinate.y + mapHeight / 2, out tile);
        }

        /// Height of the surface above the water level; objects standing on the cell are shifted up by this.
        public static float GroundHeightOf(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.Sand: return SandHeight;
                case TerrainType.Grass: return GrassHeight;
                default: return 0f;
            }
        }

        public float GroundHeightAt(Vector3Int cell)
            => TryGetTile(cell, out var tile) ? GroundHeightOf(tile.terrainType) : 0f;

        public bool IsBuildableArea(Vector2Int origin, Vector2Int size, bool requireUnoccupied = true)
        {
            if (size.x < 1 || size.y < 1) return false;
            for (int y = origin.y; y < origin.y + size.y; y++)
            for (int x = origin.x; x < origin.x + size.x; x++)
            {
                if (!TryGetTile(x, y, out var tile) || !tile.isBuildable ||
                    (requireUnoccupied && tile.isOccupied)) return false;
            }
            return true;
        }

        public bool TrySetOccupiedArea(Vector2Int origin, Vector2Int size, bool occupied = true)
        {
            if (!IsBuildableArea(origin, size, occupied)) return false;
            for (int y = origin.y; y < origin.y + size.y; y++)
            for (int x = origin.x; x < origin.x + size.x; x++)
                tileLookup[x, y].isOccupied = occupied;
            return true;
        }

        // ---- building sites (cell coordinates = centered Grid coordinates) --------------------

        private Vector2Int CellToTile(Vector3Int cell) => new Vector2Int(cell.x + mapWidth / 2, cell.y + mapHeight / 2);

        /// Footprint starting at originCell is entirely buildable (and unoccupied when requested).
        public bool IsAreaBuildable(Vector3Int originCell, Vector2Int size, bool requireUnoccupied = true)
            => IsBuildableArea(CellToTile(originCell), size, requireUnoccupied);

        public bool TrySetOccupied(Vector3Int originCell, Vector2Int size, bool occupied)
            => TrySetOccupiedArea(CellToTile(originCell), size, occupied);

        /// Finds a free site for a building: inland (nearest to the island centre, never on the
        /// waterline) or, with preferShore, on the beach. Keeps a one-cell gap to other buildings.
        public bool TryFindSite(Vector2Int size, bool preferShore, out Vector3Int originCell)
        {
            originCell = default;
            if (tileLookup == null || size.x < 1 || size.y < 1) return false;

            float centerX = mapWidth / 2;
            float centerY = mapHeight / 2;
            float bestScore = float.MaxValue;
            bool found = false;

            for (int y = 0; y < mapHeight; y++)
            for (int x = 0; x < mapWidth; x++)
            {
                var origin = new Vector2Int(x, y);
                if (!IsBuildableArea(origin, size) || !IsRingUnoccupied(origin, size)) continue;

                int minShore = int.MaxValue;
                for (int ty = y; ty < y + size.y; ty++)
                for (int tx = x; tx < x + size.x; tx++)
                    minShore = Mathf.Min(minShore, tileLookup[tx, ty].shoreDistance);
                if (!preferShore && minShore < 2) continue;

                float dx = x + (size.x - 1) * 0.5f - centerX;
                float dy = y + (size.y - 1) * 0.5f - centerY;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float score = preferShore ? minShore * 1000f + distance : distance;
                if (score >= bestScore) continue;

                bestScore = score;
                found = true;
                originCell = new Vector3Int(x - mapWidth / 2, y - mapHeight / 2, 0);
            }
            return found;
        }

        private bool IsRingUnoccupied(Vector2Int origin, Vector2Int size)
        {
            for (int y = origin.y - 1; y <= origin.y + size.y; y++)
            for (int x = origin.x - 1; x <= origin.x + size.x; x++)
            {
                bool inside = x >= origin.x && x < origin.x + size.x && y >= origin.y && y < origin.y + size.y;
                if (!inside && TryGetTile(x, y, out var tile) && tile.isOccupied) return false;
            }
            return true;
        }

        /// The free buildable cells closest to the island centre (used to spawn workers).
        public void TryFindFreeCells(int count, List<Vector3Int> result)
        {
            result.Clear();
            if (tileLookup == null || count <= 0) return;

            var candidates = new List<TileData>();
            foreach (TileData tile in tiles)
                if (tile.isBuildable && !tile.isOccupied) candidates.Add(tile);

            float centerX = mapWidth / 2;
            float centerY = mapHeight / 2;
            candidates.Sort((a, b) =>
            {
                float da = (a.x - centerX) * (a.x - centerX) + (a.y - centerY) * (a.y - centerY);
                float db = (b.x - centerX) * (b.x - centerX) + (b.y - centerY) * (b.y - centerY);
                return da.CompareTo(db);
            });

            for (int i = 0; i < candidates.Count && result.Count < count; i++)
                result.Add(new Vector3Int(candidates[i].x - mapWidth / 2, candidates[i].y - mapHeight / 2, 0));
        }

        private void ClearLayers()
        {
            tiles.Clear();
            waterLayer.Clear();
            sandLayer.Clear();
            grassLayer.Clear();
        }

        // ---- isometric sprite rendering ------------------------------------------------------

        private Vector3Int CellOf(TileData tile) => new Vector3Int(tile.x - mapWidth / 2, tile.y - mapHeight / 2, 0);

        // Cell centre in this object's local space (same position ResortManager places buildings at).
        private Vector3 CellCenterLocal(TileData tile)
            => transform.InverseTransformPoint(grid.GetCellCenterWorld(CellOf(tile)));

        private Sprite SpriteFor(TileData tile)
        {
            Sprite[] variants;
            switch (tile.terrainType)
            {
                case TerrainType.Sand: variants = sandSprites; break;
                case TerrainType.Grass: variants = grassSprites; break;
                default: variants = tile.isShallow ? shallowWaterSprites : deepWaterSprites; break;
            }
            if (variants == null || variants.Length == 0) return null;

            unchecked
            {
                int hash = (tile.x * 73856093) ^ (tile.y * 19349663) ^ (seed * 83492791);
                return variants[(hash & 0x7fffffff) % variants.Length];
            }
        }

        private void EnsureRendering()
        {
            if (renderObject == null)
            {
                Transform existing = transform.Find(RenderObjectName);
                renderObject = existing != null ? existing.gameObject : new GameObject(RenderObjectName);
                renderObject.transform.SetParent(transform, false);
                renderObject.hideFlags = HideFlags.DontSave; // regenerated, never serialized in the scene
            }

            meshFilter = renderObject.GetComponent<MeshFilter>();
            if (meshFilter == null) meshFilter = renderObject.AddComponent<MeshFilter>();
            meshRenderer = renderObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null) meshRenderer = renderObject.AddComponent<MeshRenderer>();

            if (generatedMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                generatedMaterial = new Material(shader)
                {
                    name = "Island Generated Material",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }
            meshRenderer.sharedMaterial = generatedMaterial;
            meshRenderer.sortingLayerName = SortingLayerName;
            meshRenderer.sortingOrder = -10;
        }

        private void BuildMesh()
        {
            EnsureRendering();

            // Back-to-front: on an isometric grid tiles with a higher x+y are further from the camera,
            // so nearer slabs are drawn later and cover the sides of the ones behind them.
            var order = new List<TileData>(tiles.Count);
            foreach (TileData tile in tiles)
                if (drawWater || tile.isLand) order.Add(tile);

            // Endless-looking sea: deep water around the map. These cells exist only for rendering.
            if (drawWater && waterMargin > 0)
            {
                for (int y = -waterMargin; y < mapHeight + waterMargin; y++)
                for (int x = -waterMargin; x < mapWidth + waterMargin; x++)
                {
                    bool insideMap = x >= 0 && x < mapWidth && y >= 0 && y < mapHeight;
                    if (!insideMap) order.Add(new TileData(x, y));
                }
            }

            order.Sort((a, b) =>
            {
                int c = (b.x + b.y).CompareTo(a.x + a.y);
                return c != 0 ? c : a.x.CompareTo(b.x);
            });

            var vertices = new List<Vector3>(order.Count * 4);
            var uvs = new List<Vector2>(order.Count * 4);
            var colors = new List<Color32>(order.Count * 4);
            var indices = new List<int>(order.Count * 6);

            Texture2D texture = null;
            bool missingSprites = false;
            foreach (TileData tile in order)
            {
                Sprite sprite = SpriteFor(tile);
                if (sprite == null) { missingSprites = true; continue; }
                if (texture == null) texture = sprite.texture;
                else if (sprite.texture != texture)
                {
                    Debug.LogWarning("IslandGenerator: all terrain sprites must come from the same sheet (Terrain.png).");
                    continue;
                }

                // Sprite pivot sits on the cell centre; the sheet's pivots are authored for that.
                float ppu = sprite.pixelsPerUnit;
                Rect rect = sprite.textureRect;
                Vector3 c = CellCenterLocal(tile);
                float x0 = c.x - sprite.pivot.x / ppu;
                float y0 = c.y - sprite.pivot.y / ppu;
                float x1 = x0 + rect.width / ppu;
                float y1 = y0 + rect.height / ppu;

                float u0 = rect.xMin / texture.width, u1 = rect.xMax / texture.width;
                float v0 = rect.yMin / texture.height, v1 = rect.yMax / texture.height;

                int start = vertices.Count;
                vertices.Add(new Vector3(x0, y0, c.z)); uvs.Add(new Vector2(u0, v0));
                vertices.Add(new Vector3(x0, y1, c.z)); uvs.Add(new Vector2(u0, v1));
                vertices.Add(new Vector3(x1, y1, c.z)); uvs.Add(new Vector2(u1, v1));
                vertices.Add(new Vector3(x1, y0, c.z)); uvs.Add(new Vector2(u1, v0));
                Color32 color = highlightBuildable && tile.isBuildable ? BuildableTint : White;
                for (int i = 0; i < 4; i++) colors.Add(color);
                indices.Add(start); indices.Add(start + 1); indices.Add(start + 2);
                indices.Add(start); indices.Add(start + 2); indices.Add(start + 3);
            }

            if (missingSprites)
                Debug.LogError("IslandGenerator: terrain sprites are not assigned. Assign the Terrain_* sprites " +
                               "from ResortEmpire/Sprites/Terrain/Terrain.png in the Inspector.");
            if (texture != null) generatedMaterial.mainTexture = texture;

            if (generatedMesh == null)
                generatedMesh = new Mesh { name = "Generated Island Mesh", hideFlags = HideFlags.HideAndDontSave };
            generatedMesh.Clear();
            generatedMesh.indexFormat = vertices.Count > 65535
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            generatedMesh.SetVertices(vertices);
            generatedMesh.SetUVs(0, uvs);
            generatedMesh.SetColors(colors);
            generatedMesh.SetIndices(indices, MeshTopology.Triangles, 0);
            generatedMesh.RecalculateBounds();
            meshFilter.sharedMesh = generatedMesh;
        }

        private void DestroyRendering()
        {
            DestroyUnityObject(renderObject);
            DestroyUnityObject(generatedMesh);
            DestroyUnityObject(generatedMaterial);
            renderObject = null;
            meshFilter = null;
            meshRenderer = null;
            generatedMesh = null;
            generatedMaterial = null;
        }

        private static void DestroyUnityObject(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        private void Validate()
        {
            if (tiles.Count != mapWidth * mapHeight) Debug.LogError("IslandGenerator: tile count is invalid.");
            foreach (TileData tile in tiles)
            {
                if (tile.terrainType == TerrainType.Water && tile.isBuildable)
                    Debug.LogError("IslandGenerator: water tile marked buildable.");
            }
            for (int x = 0; x < mapWidth; x++)
            {
                if (tileLookup[x, 0].terrainType != TerrainType.Water ||
                    tileLookup[x, mapHeight - 1].terrainType != TerrainType.Water)
                    Debug.LogWarning("IslandGenerator: island touches a horizontal map edge.");
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGrid || grid == null) return;
            Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
            Vector3 cellSize = grid.cellSize;
            float hx = cellSize.x * 0.5f;
            float hy = cellSize.y * 0.5f;
            for (int y = 0; y < mapHeight; y++)
            for (int x = 0; x < mapWidth; x++)
            {
                Vector3 c = grid.GetCellCenterWorld(new Vector3Int(x - mapWidth / 2, y - mapHeight / 2, 0));
                Vector3 l = c + new Vector3(-hx, 0f), b = c + new Vector3(0f, -hy);
                Vector3 r = c + new Vector3(hx, 0f), t = c + new Vector3(0f, hy);
                Gizmos.DrawLine(l, b); Gizmos.DrawLine(b, r); Gizmos.DrawLine(r, t); Gizmos.DrawLine(t, l);
            }
        }
    }
}
