using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResortIsland.Resort
{
    public enum PathKind
    {
        /// Start of the main path (the pier landing). Permanent, buildings cannot stand on it.
        Main = 0,
        /// Generated for a building; removed and rebuilt when that building moves.
        Auto = 1,
        /// Laid by the player; never generated or removed automatically (except under a building).
        Manual = 2
    }

    public readonly struct PathCell
    {
        public readonly PathKind Kind;
        /// Building the generated path belongs to; null for main and manual paths.
        public readonly object Owner;
        /// Extra sprite-mask bits pointing at the building or pier the path leads into.
        public readonly int EntryMask;
        /// PathStyle id (null = the network's default style).
        public readonly string Style;
        /// 1-based level of the style.
        public readonly int Level;

        public PathCell(PathKind kind, object owner, int entryMask, string style = null, int level = 1)
        {
            Kind = kind;
            Owner = owner;
            EntryMask = entryMask;
            Style = style;
            Level = Mathf.Max(1, level);
        }

        public PathCell WithEntry(int entryMask) => new PathCell(Kind, Owner, entryMask, Style, Level);
        public PathCell WithLook(string style, int level) => new PathCell(Kind, Owner, EntryMask, style, level);
        public bool SameLook(PathCell other) => Style == other.Style && Level == other.Level;
    }

    /// A building as the path network sees it.
    public readonly struct PathFootprint
    {
        public readonly object Owner;
        /// Lowest x/y cell of the footprint.
        public readonly Vector3Int Origin;
        public readonly Vector2Int Size;
        /// Cell in front of the door; when set, paths lead only there.
        public readonly bool HasEntrance;
        public readonly Vector3Int Entrance;

        public PathFootprint(object owner, Vector3Int origin, Vector2Int size)
        {
            Owner = owner;
            Origin = origin;
            Size = size;
            HasEntrance = false;
            Entrance = default;
        }

        public PathFootprint(object owner, Vector3Int origin, Vector2Int size, Vector3Int entrance)
        {
            Owner = owner;
            Origin = origin;
            Size = size;
            HasEntrance = true;
            Entrance = entrance;
        }

        /// Where a path has to touch the building: its entrance, or any side without one.
        public IEnumerable<Vector3Int> AccessCells()
        {
            if (HasEntrance)
            {
                yield return Entrance;
                yield break;
            }
            foreach (Vector3Int cell in SideCells()) yield return cell;
        }

        public Vector3 Center => Origin + new Vector3((Size.x - 1) * 0.5f, (Size.y - 1) * 0.5f, 0f);

        public bool Contains(Vector3Int cell)
            => cell.x >= Origin.x && cell.x < Origin.x + Size.x && cell.y >= Origin.y && cell.y < Origin.y + Size.y;

        /// Cells touching a side of the footprint (corners excluded) - where a path can lead in.
        public IEnumerable<Vector3Int> SideCells()
        {
            for (int x = 0; x < Size.x; x++)
            {
                yield return Origin + new Vector3Int(x, -1, 0);
                yield return Origin + new Vector3Int(x, Size.y, 0);
            }
            for (int y = 0; y < Size.y; y++)
            {
                yield return Origin + new Vector3Int(-1, y, 0);
                yield return Origin + new Vector3Int(Size.x, y, 0);
            }
        }
    }

    /// Path cells of the resort and the rules that keep buildings connected to the main path.
    ///
    /// A building is connected when a path cell next to one of its sides is linked (through path
    /// cells) to the main path. Without a main path (no pier) the first building is the hub and the
    /// others connect to it instead.
    public sealed class PathNetwork
    {
        private readonly Dictionary<Vector3Int, PathCell> cells = new Dictionary<Vector3Int, PathCell>();
        private readonly List<Vector3Int> mainCells = new List<Vector3Int>();
        private readonly Func<Vector3Int, bool> isWalkable;
        private readonly List<Vector3Int> route = new List<Vector3Int>();
        private readonly List<Vector3Int> scratch = new List<Vector3Int>();

        public event Action Changed;

        /// isWalkable: land a path may lie on (buildable and without a building).
        public PathNetwork(Func<Vector3Int, bool> isWalkable)
        {
            this.isWalkable = isWalkable ?? throw new ArgumentNullException(nameof(isWalkable));
        }

        public IReadOnlyDictionary<Vector3Int, PathCell> Cells => cells;
        public bool HasMain => mainCells.Count > 0;
        /// Style of the main path and of generated paths.
        public string DefaultStyle { get; set; }

        public bool Contains(Vector3Int cell) => cells.ContainsKey(cell);
        public bool TryGet(Vector3Int cell, out PathCell pathCell) => cells.TryGetValue(cell, out pathCell);
        public bool IsMain(Vector3Int cell) => cells.TryGetValue(cell, out var c) && c.Kind == PathKind.Main;

        /// Sprite index: path neighbours plus the entry towards a building or the pier.
        public int MaskAt(Vector3Int cell)
        {
            if (!cells.TryGetValue(cell, out var pathCell)) return 0;
            int mask = pathCell.EntryMask;
            for (int d = 0; d < PathFinder.Directions.Length; d++)
                if (cells.ContainsKey(cell + PathFinder.Directions[d])) mask |= 1 << d;
            return mask;
        }

        // ---- editing --------------------------------------------------------------------------

        public void SetMain(Vector3Int cell, int entryMask)
        {
            cells[cell] = new PathCell(PathKind.Main, null, entryMask, DefaultStyle);
            if (!mainCells.Contains(cell)) mainCells.Add(cell);
            Changed?.Invoke();
        }

        /// Removes generated and manual paths; the main path stays unless keepMain is false.
        public void Clear(bool keepMain = true)
        {
            scratch.Clear();
            foreach (var pair in cells)
                if (!keepMain || pair.Value.Kind != PathKind.Main) scratch.Add(pair.Key);
            foreach (Vector3Int cell in scratch) cells.Remove(cell);
            if (!keepMain) mainCells.Clear();
            Changed?.Invoke();
        }

        public bool CanAdd(Vector3Int cell) => !cells.ContainsKey(cell) && isWalkable(cell);

        public bool AddManual(Vector3Int cell, string style = null, int level = 1)
        {
            if (!CanAdd(cell)) return false;
            cells[cell] = new PathCell(PathKind.Manual, null, 0, style ?? DefaultStyle, level);
            Changed?.Invoke();
            return true;
        }

        /// Changes the style / level of an existing cell (the main path included). False when there is
        /// no path or it already looks like that.
        public bool SetLook(Vector3Int cell, string style, int level)
        {
            if (!cells.TryGetValue(cell, out var pathCell)) return false;
            var changed = pathCell.WithLook(style, level);
            if (changed.SameLook(pathCell)) return false;
            cells[cell] = changed;
            Changed?.Invoke();
            return true;
        }

        /// Copy of the current cells, for undoing an edit (RestoreSnapshot).
        public Dictionary<Vector3Int, PathCell> Snapshot() => new Dictionary<Vector3Int, PathCell>(cells);

        public void RestoreSnapshot(IReadOnlyDictionary<Vector3Int, PathCell> snapshot)
        {
            cells.Clear();
            mainCells.Clear();
            foreach (var pair in snapshot)
            {
                cells[pair.Key] = pair.Value;
                if (pair.Value.Kind == PathKind.Main) mainCells.Add(pair.Key);
            }
            Changed?.Invoke();
        }

        /// Price of an edit: every cell that is new or got a different style / level is paid in full
        /// (priceOf(style, level) per tile). Removing paths is free.
        public static int ChangeCost(IReadOnlyDictionary<Vector3Int, PathCell> before,
            IReadOnlyDictionary<Vector3Int, PathCell> after, Func<string, int, int> priceOf)
        {
            int cost = 0;
            foreach (var pair in after)
            {
                if (pair.Value.Kind == PathKind.Main) continue;
                if (before.TryGetValue(pair.Key, out var old) && old.SameLook(pair.Value)) continue;
                cost += priceOf(pair.Value.Style, pair.Value.Level);
            }
            return cost;
        }

        public bool CanRemove(Vector3Int cell) => cells.TryGetValue(cell, out var c) && c.Kind != PathKind.Main;

        /// Removes a generated or manual path cell. Nothing is regenerated, so the player can
        /// delete a generated path and lay their own.
        public bool Remove(Vector3Int cell)
        {
            if (!CanRemove(cell)) return false;
            cells.Remove(cell);
            Changed?.Invoke();
            return true;
        }

        /// Puts back a saved cell (no regeneration). Cells that no longer lie on free land are skipped.
        public bool Restore(Vector3Int cell, PathKind kind, object owner, int entryMask, string style = null, int level = 1)
        {
            if (kind == PathKind.Main || !CanAdd(cell)) return false;
            cells[cell] = new PathCell(kind, kind == PathKind.Auto ? owner : null, entryMask,
                string.IsNullOrEmpty(style) ? DefaultStyle : style, level);
            Changed?.Invoke();
            return true;
        }

        // ---- connectivity ---------------------------------------------------------------------

        /// Owners of the buildings that are connected right now.
        public HashSet<object> ConnectedOwners(IReadOnlyList<PathFootprint> buildings)
        {
            var reached = ReachableCells(buildings);
            var result = new HashSet<object>();
            for (int i = 0; i < buildings.Count; i++)
                if (IsConnected(i, buildings, reached)) result.Add(buildings[i].Owner);
            return result;
        }

        /// Brings the network up to date after buildings were placed or moved:
        /// - paths now covered by a building disappear,
        /// - the generated path of <paramref name="rebuild"/> is deleted and generated again,
        /// - every building in <paramref name="keepConnected"/> that lost its connection gets a new path.
        /// Buildings outside keepConnected are left alone, so a path the player deleted on purpose
        /// does not come back by itself.
        public void Reconnect(IReadOnlyList<PathFootprint> buildings, ICollection<object> keepConnected, object rebuild = null)
        {
            scratch.Clear();
            foreach (var pair in cells)
                if (pair.Value.Kind != PathKind.Main && !isWalkable(pair.Key)) scratch.Add(pair.Key);
            foreach (Vector3Int cell in scratch) cells.Remove(cell);

            if (rebuild != null) RemoveOwned(rebuild, null);

            var reached = ReachableCells(buildings);
            foreach (int i in ConnectOrder(buildings, rebuild))
            {
                object owner = buildings[i].Owner;
                bool wanted = (rebuild != null && Equals(owner, rebuild)) ||
                              (keepConnected != null && keepConnected.Contains(owner));
                if (!wanted || IsConnected(i, buildings, reached)) continue;

                // pieces of its old path that lead nowhere any more
                RemoveOwned(owner, reached);
                if (Connect(i, buildings, reached))
                    reached = ReachableCells(buildings);
                else
                    Debug.LogWarning("Chodník: budova sa nedá napojiť na hlavný chodník (žiadna voľná cesta).");
            }

            Changed?.Invoke();
        }

        private bool Connect(int index, IReadOnlyList<PathFootprint> buildings, HashSet<Vector3Int> reached)
        {
            PathFootprint building = buildings[index];
            HashSet<Vector3Int> hubSides = null;
            if (!HasMain && buildings.Count > 0)
                hubSides = new HashSet<Vector3Int>(buildings[0].AccessCells());

            bool IsTarget(Vector3Int cell)
                => reached.Contains(cell) || (hubSides != null && hubSides.Contains(cell));

            if (!PathFinder.TryFind(building.AccessCells(), IsPassable, IsTarget, route, cells.ContainsKey))
                return false;

            for (int i = 0; i < route.Count; i++)
            {
                Vector3Int cell = route[i];
                int entry = i == 0 ? EntryTowards(building, cell) : 0;
                if (cells.TryGetValue(cell, out var existing))
                {
                    if (entry != 0) cells[cell] = existing.WithEntry(existing.EntryMask | entry);
                }
                else
                {
                    cells[cell] = new PathCell(PathKind.Auto, building.Owner, entry, DefaultStyle);
                }
            }
            return true;
        }

        private bool IsPassable(Vector3Int cell) => isWalkable(cell) || IsMain(cell);

        private static int EntryTowards(PathFootprint building, Vector3Int cell)
        {
            foreach (Vector3Int step in PathFinder.Directions)
                if (building.Contains(cell + step)) return PathFinder.DirectionBit(step);
            return 0;
        }

        private void RemoveOwned(object owner, HashSet<Vector3Int> keep)
        {
            scratch.Clear();
            foreach (var pair in cells)
            {
                if (pair.Value.Kind != PathKind.Auto || !Equals(pair.Value.Owner, owner)) continue;
                if (keep == null || !keep.Contains(pair.Key)) scratch.Add(pair.Key);
            }
            foreach (Vector3Int cell in scratch) cells.Remove(cell);
        }

        private bool IsConnected(int index, IReadOnlyList<PathFootprint> buildings, HashSet<Vector3Int> reached)
        {
            if (!HasMain && index == 0) return true; // the hub
            foreach (Vector3Int cell in buildings[index].AccessCells())
                if (reached.Contains(cell)) return true;
            return false;
        }

        /// Path cells linked to the main path (or, without one, to the hub building).
        private HashSet<Vector3Int> ReachableCells(IReadOnlyList<PathFootprint> buildings)
        {
            var reached = new HashSet<Vector3Int>();
            var queue = new Queue<Vector3Int>();

            void Seed(Vector3Int cell)
            {
                if (cells.ContainsKey(cell) && reached.Add(cell)) queue.Enqueue(cell);
            }

            foreach (Vector3Int cell in mainCells) Seed(cell);
            if (!HasMain && buildings.Count > 0)
                foreach (Vector3Int cell in buildings[0].AccessCells()) Seed(cell);

            while (queue.Count > 0)
            {
                Vector3Int cell = queue.Dequeue();
                foreach (Vector3Int step in PathFinder.Directions) Seed(cell + step);
            }
            return reached;
        }

        /// The building being rebuilt first, then the others from the nearest to the main path,
        /// so far buildings join the paths of nearer ones.
        private List<int> ConnectOrder(IReadOnlyList<PathFootprint> buildings, object rebuild)
        {
            Vector3 root = HasMain ? (Vector3)mainCells[0] : buildings.Count > 0 ? buildings[0].Center : Vector3.zero;
            var order = new List<int>(buildings.Count);
            for (int i = 0; i < buildings.Count; i++) order.Add(i);

            order.Sort((a, b) =>
            {
                bool ra = rebuild != null && Equals(buildings[a].Owner, rebuild);
                bool rb = rebuild != null && Equals(buildings[b].Owner, rebuild);
                if (ra != rb) return ra ? -1 : 1;
                int c = (buildings[a].Center - root).sqrMagnitude.CompareTo((buildings[b].Center - root).sqrMagnitude);
                return c != 0 ? c : a.CompareTo(b);
            });
            return order;
        }
    }
}
