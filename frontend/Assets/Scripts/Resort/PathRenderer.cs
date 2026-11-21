using System.Collections.Generic;
using UnityEngine;

namespace ResortIsland.Resort
{
    /// Draws the cells of ResortManager.Paths with the ResortEmpire path sprites.
    /// New paths grow tile by tile from the existing network (each tile drops in with a bounce and a
    /// small dust flash), removed paths shrink back. Runs on unscaled time, so it also plays while the
    /// game is paused.
    public sealed class PathRenderer : MonoBehaviour
    {
        // above the island mesh (-10), below the placement preview
        private const string SortingLayerName = "Ground";
        private const int BaseSortingOrder = 100;
        private const int EffectSortingOrder = 400;

        // sprites come from ResortManager.PathStyles (style + level of each cell)
        [SerializeField] private ResortManager resort;

        [Header("Animation")]
        [SerializeField] private bool animate = true;
        [Tooltip("Delay between neighbouring tiles, seconds.")]
        [SerializeField, Min(0f)] private float stepDelay = 0.045f;
        [SerializeField, Min(0.01f)] private float appearDuration = 0.4f;
        [SerializeField, Min(0.01f)] private float disappearDuration = 0.22f;
        [Tooltip("How high above the ground a new tile starts falling from (world units).")]
        [SerializeField, Min(0f)] private float dropHeight = 0.35f;
        [SerializeField] private bool dustEffect = true;

        private enum TileState
        {
            Appearing,
            Idle,
            Disappearing
        }

        private sealed class Tile
        {
            public Vector3Int Cell;
            public SpriteRenderer Renderer;
            public TileState State;
            /// Seconds before the animation starts.
            public float Delay;
            /// Seconds since the animation started.
            public float Time;
            public int Entry;
            public string Style;
            public int Level;
            public int ShownMask = -1;
            /// Seconds left of the bounce of an idle tile, its length and strength.
            public float Pulse;
            public float PulseLength = PulseDuration;
            public float PulseStrength = 0.08f;
        }

        private struct Particle
        {
            public SpriteRenderer Renderer;
            public Vector3 Position;
            public Vector3 Velocity;
            public float Time;
            public float Life;
            public float ScaleFrom;
            public float ScaleTo;
            public float AlphaFrom;
            public Color Color;
        }

        private static readonly Color FlashColor = new Color(1f, 0.96f, 0.82f);
        private static readonly Color DustColor = new Color(0.86f, 0.8f, 0.68f);
        private const float PulseDuration = 0.16f;
        private const float RestyleDuration = 0.3f;

        private readonly Dictionary<Vector3Int, Tile> tiles = new Dictionary<Vector3Int, Tile>();
        private readonly List<Tile> tileList = new List<Tile>();
        private readonly Stack<SpriteRenderer> freeRenderers = new Stack<SpriteRenderer>();
        private readonly List<Particle> particles = new List<Particle>();
        private readonly Stack<SpriteRenderer> freeParticles = new Stack<SpriteRenderer>();
        private readonly List<Vector3Int> added = new List<Vector3Int>();
        private readonly List<Vector3Int> removed = new List<Vector3Int>();
        private readonly HashSet<Vector3Int> addedSet = new HashSet<Vector3Int>();

        private PathNetwork network;
        private Transform root;
        private Sprite effectSprite;
        private Texture2D effectTexture;
        private bool dirty;
        private bool animating;

        private void Awake()
        {
            if (resort == null) resort = GetComponent<ResortManager>();
        }

        private void OnDestroy()
        {
            if (network != null) network.Changed -= MarkDirty;
            if (effectSprite != null) Destroy(effectSprite);
            if (effectTexture != null) Destroy(effectTexture);
        }

        private void LateUpdate()
        {
            // wait for the main path, so the first paths grow out from the pier
            if (resort == null || resort.Paths == null || !resort.PathsReady) return;

            if (network != resort.Paths)
            {
                if (network != null) network.Changed -= MarkDirty;
                network = resort.Paths;
                network.Changed += MarkDirty;
                dirty = true;
            }

            if (dirty)
            {
                dirty = false;
                Sync();
            }

            if (animating) Animate(Mathf.Min(Time.unscaledDeltaTime, 0.1f));
        }

        private void MarkDirty() => dirty = true;

        // ---- network -> tiles ---------------------------------------------------------------

        private void Sync()
        {
            EnsureRoot();
            added.Clear();
            removed.Clear();

            foreach (var pair in network.Cells)
            {
                if (!tiles.TryGetValue(pair.Key, out var tile) || tile.State == TileState.Disappearing)
                {
                    added.Add(pair.Key);
                    continue;
                }

                tile.Entry = pair.Value.EntryMask;
                if (tile.Style == pair.Value.Style && tile.Level == pair.Value.Level) continue;

                // restyled / upgraded: new look with a bigger bounce and a puff of dust
                tile.Style = pair.Value.Style;
                tile.Level = pair.Value.Level;
                tile.ShownMask = -1;
                if (!animate || IsHidden(tile)) continue;
                tile.Pulse = tile.PulseLength = RestyleDuration;
                tile.PulseStrength = 0.18f;
                if (dustEffect) SpawnDust(resort.CellToWorld(pair.Key));
            }
            for (int i = tileList.Count - 1; i >= 0; i--)
            {
                Tile tile = tileList[i];
                if (tile.State == TileState.Disappearing || network.Contains(tile.Cell)) continue;
                if (IsHidden(tile)) Release(i); // never landed, nothing to animate
                else removed.Add(tile.Cell);
            }
            addedSet.Clear();
            addedSet.UnionWith(added);

            if (added.Count == 0 && removed.Count == 0)
            {
                animating = true; // entry bits may have changed
                return;
            }

            // removed paths retract from their far end towards what stays
            float removeTime = 0f;
            if (removed.Count > 0)
            {
                var steps = PathStagger.Steps(removed, TouchesRemainingPath);
                int max = 0;
                foreach (int s in steps.Values) max = Mathf.Max(max, s);
                foreach (Vector3Int cell in removed)
                {
                    Tile tile = tiles[cell];
                    tile.State = TileState.Disappearing;
                    tile.Delay = animate ? (max - steps[cell]) * stepDelay : 0f;
                    tile.Time = animate ? 0f : disappearDuration;
                }
                if (animate) removeTime = max * stepDelay + disappearDuration * 0.6f;
            }

            // new paths grow out of the existing network (or the main path)
            if (added.Count > 0)
            {
                var steps = PathStagger.Steps(added, GrowsFromHere);
                foreach (Vector3Int cell in added)
                {
                    network.TryGet(cell, out var pathCell);
                    if (!tiles.TryGetValue(cell, out var tile))
                    {
                        tile = new Tile { Cell = cell, Renderer = TakeRenderer() };
                        tiles[cell] = tile;
                        tileList.Add(tile);
                    }
                    tile.Entry = pathCell.EntryMask;
                    tile.Style = pathCell.Style;
                    tile.Level = pathCell.Level;
                    tile.Pulse = 0f;
                    tile.State = TileState.Appearing;
                    tile.Delay = animate ? removeTime + steps[cell] * stepDelay : 0f;
                    tile.Time = animate ? 0f : appearDuration;
                    tile.ShownMask = -1;
                    tile.Renderer.enabled = false;
                    tile.Renderer.sortingOrder = BaseSortingOrder - (cell.x + cell.y);
                }
            }

            animating = true;
        }

        private bool TouchesRemainingPath(Vector3Int cell)
        {
            foreach (Vector3Int step in PathFinder.Directions)
                if (network.Contains(cell + step)) return true;
            return false;
        }

        private bool GrowsFromHere(Vector3Int cell)
        {
            if (network.IsMain(cell)) return true;
            foreach (Vector3Int step in PathFinder.Directions)
                if (tiles.TryGetValue(cell + step, out var tile) && tile.State != TileState.Disappearing &&
                    !addedSet.Contains(cell + step)) return true;
            return false;
        }

        // ---- animation ----------------------------------------------------------------------

        private void Animate(float dt)
        {
            bool busy = false;

            // 1. timers and state changes
            for (int i = tileList.Count - 1; i >= 0; i--)
            {
                Tile tile = tileList[i];
                if (tile.Delay > 0f)
                {
                    tile.Delay -= dt;
                    busy = true;
                    continue;
                }

                switch (tile.State)
                {
                    case TileState.Appearing:
                        if (tile.Time == 0f && dustEffect) SpawnDust(resort.CellToWorld(tile.Cell));
                        tile.Time += dt;
                        if (tile.Time >= appearDuration) tile.State = TileState.Idle;
                        else busy = true;
                        break;
                    case TileState.Disappearing:
                        tile.Time += dt;
                        if (tile.Time >= disappearDuration) Release(i);
                        else busy = true;
                        break;
                    default:
                        if (tile.Pulse > 0f)
                        {
                            tile.Pulse = Mathf.Max(0f, tile.Pulse - dt);
                            busy = true;
                        }
                        break;
                }
            }

            // 2. shapes, 3. poses
            UpdateSprites();
            foreach (Tile tile in tileList)
            {
                if (IsHidden(tile)) continue;
                if (tile.State == TileState.Disappearing)
                {
                    float t = tile.Delay > 0f ? 0f : Mathf.Clamp01(tile.Time / disappearDuration);
                    Pose(tile, Mathf.Max(0f, 1f - EaseInBack(t)), -0.06f * t, 1f - t);
                }
                else if (tile.State == TileState.Appearing)
                {
                    float t = Mathf.Clamp01(tile.Time / appearDuration);
                    Pose(tile, EaseOutBack(t), dropHeight * (1f - EaseOutBounce(t)), Mathf.Clamp01(t * 4f));
                }
                else
                {
                    float pulse = tile.PulseStrength * Mathf.Sin(tile.Pulse / tile.PulseLength * Mathf.PI);
                    Pose(tile, 1f + pulse, 0f, 1f);
                }
            }

            busy |= AnimateParticles(dt);
            animating = busy;
        }

        // the shape of a tile only connects to neighbours that are already on the ground
        private void UpdateSprites()
        {
            foreach (Tile tile in tileList)
            {
                if (tile.State == TileState.Disappearing || IsHidden(tile)) continue;

                int mask = tile.Entry;
                for (int d = 0; d < PathFinder.Directions.Length; d++)
                    if (tiles.TryGetValue(tile.Cell + PathFinder.Directions[d], out var other) && IsPresent(other))
                        mask |= 1 << d;

                if (mask == tile.ShownMask) continue;
                if (tile.ShownMask >= 0 && tile.State == TileState.Idle && tile.Pulse <= 0f)
                {
                    tile.Pulse = tile.PulseLength = PulseDuration;
                    tile.PulseStrength = 0.08f;
                }
                tile.ShownMask = mask;
                tile.Renderer.sprite = resort.PathStyles != null ? resort.PathStyles.GetSprite(tile.Style, tile.Level, mask) : null;
            }
        }

        private static bool IsHidden(Tile tile) => tile.State == TileState.Appearing && tile.Delay > 0f;

        // on the ground: landed or landing, or removed but still waiting for its turn to shrink
        private static bool IsPresent(Tile tile)
        {
            switch (tile.State)
            {
                case TileState.Appearing: return tile.Delay <= 0f;
                case TileState.Disappearing: return tile.Delay > 0f;
                default: return true;
            }
        }

        private void Pose(Tile tile, float scale, float lift, float alpha)
        {
            Transform t = tile.Renderer.transform;
            t.position = resort.CellToWorld(tile.Cell) + Vector3.up * lift;
            t.localScale = new Vector3(scale, scale, 1f);
            Color c = tile.Renderer.color;
            c.a = alpha;
            tile.Renderer.color = c;
            tile.Renderer.enabled = tile.Renderer.sprite != null && scale > 0f;
        }

        private void Release(int index)
        {
            Tile tile = tileList[index];
            tileList.RemoveAt(index);
            tiles.Remove(tile.Cell);
            tile.Renderer.enabled = false;
            freeRenderers.Push(tile.Renderer);
        }

        // ---- dust flash ---------------------------------------------------------------------

        private void SpawnDust(Vector3 position)
        {
            EnsureEffectSprite();

            // soft glow on the ground under the tile
            AddParticle(position, Vector3.zero, 0.35f, 0.7f, 1.5f, 0.55f, FlashColor);

            // four puffs flying out to the corners of the cell
            Vector3[] corners = { new Vector3(-0.5f, 0f), new Vector3(0.5f, 0f), new Vector3(0f, 0.25f), new Vector3(0f, -0.25f) };
            foreach (Vector3 dir in corners)
            {
                Vector3 velocity = dir * Random.Range(0.9f, 1.4f) + Vector3.up * Random.Range(0.15f, 0.35f);
                AddParticle(position, velocity, Random.Range(0.3f, 0.45f), 0.22f, 0.08f, 0.8f, DustColor);
            }
        }

        private void AddParticle(Vector3 position, Vector3 velocity, float life, float scaleFrom, float scaleTo, float alpha, Color color)
        {
            SpriteRenderer renderer = freeParticles.Count > 0 ? freeParticles.Pop() : CreateParticleRenderer();
            renderer.enabled = true;
            particles.Add(new Particle
            {
                Renderer = renderer,
                Position = position,
                Velocity = velocity,
                Life = life,
                ScaleFrom = scaleFrom,
                ScaleTo = scaleTo,
                AlphaFrom = alpha,
                Color = color
            });
        }

        private bool AnimateParticles(float dt)
        {
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                Particle p = particles[i];
                p.Time += dt;
                float t = p.Time / p.Life;
                if (t >= 1f)
                {
                    p.Renderer.enabled = false;
                    freeParticles.Push(p.Renderer);
                    particles.RemoveAt(i);
                    continue;
                }

                p.Velocity += Vector3.down * 1.2f * dt; // a little gravity on the puffs
                p.Velocity *= 1f - 3f * dt;
                p.Position += p.Velocity * dt;

                float scale = Mathf.Lerp(p.ScaleFrom, p.ScaleTo, EaseOutCubic(t));
                p.Renderer.transform.position = p.Position;
                p.Renderer.transform.localScale = new Vector3(scale, scale, 1f);
                Color c = p.Color;
                c.a = p.AlphaFrom * (1f - t) * (1f - t);
                p.Renderer.color = c;
                particles[i] = p;
            }
            return particles.Count > 0;
        }

        // ---- objects ------------------------------------------------------------------------

        private void EnsureRoot()
        {
            if (root != null) return;
            root = new GameObject("Paths").transform;
            root.SetParent(transform, false);
        }

        private SpriteRenderer TakeRenderer()
        {
            if (freeRenderers.Count > 0) return freeRenderers.Pop();

            var go = new GameObject("Path");
            go.transform.SetParent(root, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = SortingLayerName;
            return renderer;
        }

        private SpriteRenderer CreateParticleRenderer()
        {
            var go = new GameObject("Path Dust");
            go.transform.SetParent(root, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = effectSprite;
            renderer.sortingLayerName = SortingLayerName;
            renderer.sortingOrder = EffectSortingOrder;
            return renderer;
        }

        // soft isometric diamond, 1 cell wide at scale 1
        private void EnsureEffectSprite()
        {
            if (effectSprite != null) return;

            const int width = 64;
            const int height = 32;
            effectTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "Path Dust Texture"
            };

            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float nx = Mathf.Abs((x + 0.5f - width * 0.5f) / (width * 0.5f));
                float ny = Mathf.Abs((y + 0.5f - height * 0.5f) / (height * 0.5f));
                float a = Mathf.Clamp01(1f - (nx + ny));
                pixels[y * width + x] = new Color(1f, 1f, 1f, a * a);
            }

            effectTexture.SetPixels(pixels);
            effectTexture.Apply();
            effectSprite = Sprite.Create(effectTexture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), width);
            effectSprite.name = "Path Dust";
        }

        // ---- easing -------------------------------------------------------------------------

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        private static float EaseInBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return c3 * t * t * t - c1 * t * t;
        }

        private static float EaseOutCubic(float t)
        {
            float u = 1f - t;
            return 1f - u * u * u;
        }

        private static float EaseOutBounce(float t)
        {
            const float n1 = 7.5625f, d1 = 2.75f;
            if (t < 1f / d1) return n1 * t * t;
            if (t < 2f / d1) { t -= 1.5f / d1; return n1 * t * t + 0.75f; }
            if (t < 2.5f / d1) { t -= 2.25f / d1; return n1 * t * t + 0.9375f; }
            t -= 2.625f / d1;
            return n1 * t * t + 0.984375f;
        }
    }
}
