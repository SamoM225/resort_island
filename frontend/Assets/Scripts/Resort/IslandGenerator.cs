using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResortIsland.Resort
{
    public enum TerrainType
    {
        Water,
        Sand,
        Grass,
        Hills,
        Mountains,
        Rocks
    }

    [Serializable]
    public sealed class TileData
    {
        public int x;
        public int y;
        public TerrainType terrainType;
        public bool isLand;
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

    /// Generates the logical island first, then derives an isometric colored mesh from it.
    /// TileData is the authoritative source used by gameplay systems.
    /// Tiles are drawn exactly on the cells of the scene Grid (isometric layout), so buildings
    /// placed through ResortManager line up with the generated terrain. The mesh is generated
    /// in edit mode too (ExecuteAlways), so the island is visible in the Scene view.
    [ExecuteAlways]
    public sealed class IslandGenerator : MonoBehaviour
    {
        private const string RenderObjectName = "GeneratedIsland";
        private const string SortingLayerName = "Ground";

        [Header("Grid")]
        [Tooltip("Isometric Grid the island is drawn on. Found automatically when empty.")]
        [SerializeField] private Grid grid;
        [SerializeField, Min(1)] private int mapWidth = 64;
        [SerializeField, Min(1)] private int mapHeight = 64;
        [SerializeField] private int seed = 1337;

        [Header("Island shape")]
        [SerializeField, Range(0.1f, 0.49f)] private float islandRadius = 0.40f;
        [SerializeField, Range(0f, 0.2f)] private float coastlineIrregularity = 0.07f;
        [SerializeField, Min(0.01f)] private float noiseScale = 1.8f;
        [SerializeField, Min(0f)] private float beachWidth = 0.055f;

        [Header("Terrain thresholds")]
        [SerializeField, Range(0f, 1f)] private float hillThreshold = 0.54f;
        [SerializeField, Range(0f, 1f)] private float mountainThreshold = 0.70f;
        [SerializeField, Range(0f, 1f)] private float rockThreshold = 0.84f;
        [SerializeField, Range(0f, 1f)] private float mountainDensity = 0.85f;
        [SerializeField] private bool sandBuildable = true;
        [SerializeField] private bool grassBuildable = true;
        [SerializeField] private bool hillsBuildable = true;

        [Header("Isometric rendering")]
        [Tooltip("World height of one elevation level (mountains = 2 levels, rocks = 3).")]
        [SerializeField, Min(0f)] private float heightStep = 0.2f;
        [Tooltip("Also draw water tiles (useful in the Scene view). Colour matches the camera background.")]
        [SerializeField] private bool drawWater = true;

        [Header("Debug")]
        [Tooltip("Generate when the component is enabled (also in edit mode).")]
        [SerializeField] private bool generateOnStart = true;
        [SerializeField] private bool showGrid;
        [SerializeField] private bool highlightBuildable;

        private readonly List<TileData> tiles = new List<TileData>();
        private readonly List<TileData> waterLayer = new List<TileData>();
        private readonly List<TileData> sandLayer = new List<TileData>();
        private readonly List<TileData> grassLayer = new List<TileData>();
        private readonly List<TileData> hillsLayer = new List<TileData>();
        private readonly List<TileData> mountainsLayer = new List<TileData>();
        private readonly List<TileData> rocksLayer = new List<TileData>();
        private TileData[,] tileLookup;
        private GameObject renderObject;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh generatedMesh;
        private Material generatedMaterial;
        private bool regenerateQueued;

        private static readonly Color32 WaterColor = new Color32(30, 159, 196, 255);
        private static readonly Color32 SandColor = new Color32(224, 193, 111, 255);
        private static readonly Color32 GrassColor = new Color32(91, 166, 78, 255);
        private static readonly Color32 HillsColor = new Color32(66, 125, 61, 255);
        private static readonly Color32 MountainsColor = new Color32(112, 105, 94, 255);
        private static readonly Color32 RocksColor = new Color32(65, 65, 65, 255);
        private static readonly Color32 White = new Color32(255, 255, 255, 255);
        private static readonly Color32 Black = new Color32(0, 0, 0, 255);

        public int MapWidth => mapWidth;
        public int MapHeight => mapHeight;
        public int Seed => seed;
        public IReadOnlyList<TileData> Tiles => tiles;
        public IReadOnlyList<TileData> WaterLayer => waterLayer;
        public IReadOnlyList<TileData> SandLayer => sandLayer;
        public IReadOnlyList<TileData> GrassLayer => grassLayer;
        public IReadOnlyList<TileData> HillsLayer => hillsLayer;
        public IReadOnlyList<TileData> MountainsLayer => mountainsLayer;
        public IReadOnlyList<TileData> RocksLayer => rocksLayer;

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
                    tile.terrainType = ResolveTerrain(edge, tile.elevation, centerBias);
                    tile.isLand = tile.terrainType != TerrainType.Water;
                    tile.isBuildable = IsTerrainBuildable(tile.terrainType);
                    tileLookup[x, y] = tile;
                    tiles.Add(tile);
                    LayerFor(tile.terrainType).Add(tile);
                }
            }

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

        private TerrainType ResolveTerrain(float edge, float elevation, float centerBias)
        {
            if (edge <= 0f) return TerrainType.Water;
            if (edge < beachWidth) return TerrainType.Sand;
            if (elevation >= rockThreshold && centerBias > 0.35f) return TerrainType.Rocks;
            float mountainCutoff = mountainThreshold + (1f - mountainDensity) * 0.2f;
            if (elevation >= mountainCutoff && centerBias > 0.30f && mountainDensity > 0f)
                return TerrainType.Mountains;
            if (elevation >= hillThreshold && centerBias > 0.12f) return TerrainType.Hills;
            return TerrainType.Grass;
        }

        private List<TileData> LayerFor(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.Sand: return sandLayer;
                case TerrainType.Grass: return grassLayer;
                case TerrainType.Hills: return hillsLayer;
                case TerrainType.Mountains: return mountainsLayer;
                case TerrainType.Rocks: return rocksLayer;
                default: return waterLayer;
            }
        }

        private bool IsTerrainBuildable(TerrainType terrain)
        {
            if (terrain == TerrainType.Sand) return sandBuildable;
            if (terrain == TerrainType.Grass) return grassBuildable;
            return terrain == TerrainType.Hills && hillsBuildable;
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

        private void ClearLayers()
        {
            tiles.Clear();
            waterLayer.Clear();
            sandLayer.Clear();
            grassLayer.Clear();
            hillsLayer.Clear();
            mountainsLayer.Clear();
            rocksLayer.Clear();
        }

        // ---- isometric rendering -------------------------------------------------------------

        private Vector3Int CellOf(TileData tile) => new Vector3Int(tile.x - mapWidth / 2, tile.y - mapHeight / 2, 0);

        // Cell centre in this object's local space (same position ResortManager places buildings at).
        private Vector3 CellCenterLocal(TileData tile)
            => transform.InverseTransformPoint(grid.GetCellCenterWorld(CellOf(tile)));

        private float LiftOf(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.Mountains: return heightStep * 2f;
                case TerrainType.Rocks: return heightStep * 3f;
                default: return 0f;
            }
        }

        private float LiftAt(int x, int y)
            => TryGetTile(x, y, out var tile) ? LiftOf(tile.terrainType) : 0f;

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

            // Back-to-front: on an isometric grid tiles with a higher x+y are further from the camera.
            var order = new List<TileData>(tiles.Count);
            foreach (TileData tile in tiles)
                if (drawWater || tile.isLand) order.Add(tile);
            order.Sort((a, b) =>
            {
                int c = (b.x + b.y).CompareTo(a.x + a.y);
                return c != 0 ? c : a.x.CompareTo(b.x);
            });

            var vertices = new List<Vector3>(order.Count * 8);
            var colors = new List<Color32>(order.Count * 8);
            var indices = new List<int>(order.Count * 12);

            Vector3 cellSize = grid.cellSize;
            float hx = cellSize.x * 0.5f;
            float hy = cellSize.y * 0.5f;

            foreach (TileData tile in order)
            {
                Vector3 c = CellCenterLocal(tile);
                float lift = LiftOf(tile.terrainType);
                Color32 top = ColorFor(tile);

                Vector3 left = new Vector3(c.x - hx, c.y, c.z);
                Vector3 bottom = new Vector3(c.x, c.y - hy, c.z);
                Vector3 right = new Vector3(c.x + hx, c.y, c.z);
                Vector3 back = new Vector3(c.x, c.y + hy, c.z);
                Vector3 up = new Vector3(0f, lift, 0f);

                // Side walls toward the viewer, only where this tile stands above its neighbour.
                float liftLeft = LiftAt(tile.x - 1, tile.y);   // neighbour across the bottom-left edge
                float liftRight = LiftAt(tile.x, tile.y - 1);  // neighbour across the bottom-right edge
                if (lift > liftLeft)
                {
                    var low = new Vector3(0f, liftLeft, 0f);
                    AddQuad(vertices, colors, indices, left + up, bottom + up, bottom + low, left + low,
                        Shade(top, 0.78f));
                }
                if (lift > liftRight)
                {
                    var low = new Vector3(0f, liftRight, 0f);
                    AddQuad(vertices, colors, indices, bottom + up, right + up, right + low, bottom + low,
                        Shade(top, 0.58f));
                }

                AddQuad(vertices, colors, indices, left + up, bottom + up, right + up, back + up, top);
            }

            if (generatedMesh == null)
                generatedMesh = new Mesh { name = "Generated Island Mesh", hideFlags = HideFlags.HideAndDontSave };
            generatedMesh.Clear();
            generatedMesh.indexFormat = vertices.Count > 65535
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            generatedMesh.SetVertices(vertices);
            generatedMesh.SetColors(colors);
            generatedMesh.SetIndices(indices, MeshTopology.Triangles, 0);
            generatedMesh.RecalculateBounds();
            meshFilter.sharedMesh = generatedMesh;
        }

        private static void AddQuad(List<Vector3> vertices, List<Color32> colors, List<int> indices,
            Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color32 color)
        {
            int start = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            for (int i = 0; i < 4; i++) colors.Add(color);
            indices.Add(start); indices.Add(start + 1); indices.Add(start + 2);
            indices.Add(start); indices.Add(start + 2); indices.Add(start + 3);
        }

        private static Color32 Shade(Color32 color, float factor)
            => new Color32((byte)(color.r * factor), (byte)(color.g * factor), (byte)(color.b * factor), color.a);

        private Color32 ColorFor(TileData tile)
        {
            Color32 color = BaseColorFor(tile.terrainType);
            if (tile.isLand)
                color = Color32.Lerp(color, tile.elevation > 0.5f ? White : Black,
                    Mathf.Abs(tile.elevation - 0.5f) * 0.18f);
            if (highlightBuildable && tile.isBuildable) color = Color32.Lerp(color, White, 0.25f);
            return color;
        }

        private static Color32 BaseColorFor(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.Sand: return SandColor;
                case TerrainType.Grass: return GrassColor;
                case TerrainType.Hills: return HillsColor;
                case TerrainType.Mountains: return MountainsColor;
                case TerrainType.Rocks: return RocksColor;
                default: return WaterColor;
            }
        }

        private void DestroyRendering()
        {
            DestroyObject(renderObject);
            DestroyObject(generatedMesh);
            DestroyObject(generatedMaterial);
            renderObject = null;
            meshFilter = null;
            meshRenderer = null;
            generatedMesh = null;
            generatedMaterial = null;
        }

        private static void DestroyObject(UnityEngine.Object obj)
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
                if ((tile.terrainType == TerrainType.Mountains || tile.terrainType == TerrainType.Rocks) &&
                    tile.isBuildable) Debug.LogError("IslandGenerator: elevated tile marked buildable.");
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
