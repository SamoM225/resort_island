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

    /// Generates the logical island first, then derives a lightweight colored mesh from it.
    /// TileData is the authoritative source used by gameplay systems.
    public sealed class IslandGenerator : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField, Min(1)] private int mapWidth = 64;
        [SerializeField, Min(1)] private int mapHeight = 64;
        [SerializeField, Min(0.01f)] private float tileSize = 0.25f;
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
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;

        private static readonly Color32 WaterColor = new Color32(42, 137, 190, 255);
        private static readonly Color32 SandColor = new Color32(224, 193, 111, 255);
        private static readonly Color32 GrassColor = new Color32(91, 166, 78, 255);
        private static readonly Color32 HillsColor = new Color32(66, 125, 61, 255);
        private static readonly Color32 MountainsColor = new Color32(112, 105, 94, 255);
        private static readonly Color32 RocksColor = new Color32(65, 65, 65, 255);

        public int MapWidth => mapWidth;
        public int MapHeight => mapHeight;
        public float TileSize => tileSize;
        public int Seed => seed;
        public IReadOnlyList<TileData> Tiles => tiles;
        public IReadOnlyList<TileData> WaterLayer => waterLayer;
        public IReadOnlyList<TileData> SandLayer => sandLayer;
        public IReadOnlyList<TileData> GrassLayer => grassLayer;
        public IReadOnlyList<TileData> HillsLayer => hillsLayer;
        public IReadOnlyList<TileData> MountainsLayer => mountainsLayer;
        public IReadOnlyList<TileData> RocksLayer => rocksLayer;

        private void Awake()
        {
            if (generateOnStart) Generate();
        }

        [ContextMenu("Generate island")]
        public void Generate()
        {
            mapWidth = Mathf.Max(1, mapWidth);
            mapHeight = Mathf.Max(1, mapHeight);
            tileLookup = new TileData[mapWidth, mapHeight];
            ClearLayers();

            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    var tile = new TileData(x, y);
                    float nx = (x + 0.5f) / mapWidth - 0.5f;
                    float ny = (y + 0.5f) / mapHeight - 0.5f;
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
            // ResortManager uses the existing centered Grid coordinates, while the
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

        private void BuildMesh()
        {
            if (meshFilter == null || meshRenderer == null)
            {
                Transform renderTransform = transform.Find("GeneratedIsland");
                if (renderTransform == null)
                {
                    var renderObject = new GameObject("GeneratedIsland");
                    renderTransform = renderObject.transform;
                    renderTransform.SetParent(transform, false);
                }

                meshFilter = renderTransform.GetComponent<MeshFilter>();
                if (meshFilter == null)
                    meshFilter = renderTransform.gameObject.AddComponent<MeshFilter>();

                meshRenderer = renderTransform.GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                    meshRenderer = renderTransform.gameObject.AddComponent<MeshRenderer>();
            }
            if (meshFilter == null || meshRenderer == null)
            {
                Debug.LogError("IslandGenerator: could not create the generated mesh components.");
                return;
            }
            if (meshRenderer.sharedMaterial == null)
                meshRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default")) { name = "Island Generated Material" };

            var vertices = new Vector3[tiles.Count * 4];
            var colors = new Color32[vertices.Length];
            var indices = new int[tiles.Count * 6];
            float originX = -mapWidth * tileSize * 0.5f;
            float originY = -mapHeight * tileSize * 0.5f;
            for (int i = 0; i < tiles.Count; i++)
            {
                TileData tile = tiles[i];
                int vertex = i * 4;
                float x = originX + tile.x * tileSize;
                float y = originY + tile.y * tileSize;
                vertices[vertex] = new Vector3(x, y);
                vertices[vertex + 1] = new Vector3(x + tileSize, y);
                vertices[vertex + 2] = new Vector3(x + tileSize, y + tileSize);
                vertices[vertex + 3] = new Vector3(x, y + tileSize);
                Color32 color = ColorFor(tile.terrainType);
                if (highlightBuildable && tile.isBuildable) color = Color32.Lerp(color, Color.white, 0.25f);
                colors[vertex] = colors[vertex + 1] = colors[vertex + 2] = colors[vertex + 3] = color;
                int index = i * 6;
                indices[index] = vertex;
                indices[index + 1] = vertex + 2;
                indices[index + 2] = vertex + 1;
                indices[index + 3] = vertex;
                indices[index + 4] = vertex + 3;
                indices[index + 5] = vertex + 2;
            }
            var mesh = new Mesh { name = "Generated Island Mesh" };
            mesh.indexFormat = vertices.Length > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 :
                UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.vertices = vertices;
            mesh.colors32 = colors;
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            mesh.RecalculateBounds();
            meshFilter.sharedMesh = mesh;
            meshRenderer.sortingOrder = -10;
        }

        private static Color32 ColorFor(TerrainType terrain)
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
            if (!showGrid) return;
            Gizmos.color = new Color(1f, 1f, 1f, 0.12f);
            float originX = -mapWidth * tileSize * 0.5f;
            float originY = -mapHeight * tileSize * 0.5f;
            for (int x = 0; x <= mapWidth; x++)
                Gizmos.DrawLine(transform.TransformPoint(new Vector3(originX + x * tileSize, originY)),
                    transform.TransformPoint(new Vector3(originX + x * tileSize, originY + mapHeight * tileSize)));
            for (int y = 0; y <= mapHeight; y++)
                Gizmos.DrawLine(transform.TransformPoint(new Vector3(originX, originY + y * tileSize)),
                    transform.TransformPoint(new Vector3(originX + mapWidth * tileSize, originY + y * tileSize)));
        }
    }
}
