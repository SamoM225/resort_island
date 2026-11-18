using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResortIsland.Resort
{
    /// Shortest route over grid cells (4-neighbourhood). Every turn costs extra, so routes come out
    /// straight or L-shaped instead of staircases; walking over an existing path is cheaper than
    /// laying a new one, so new routes reuse what is already there.
    public static class PathFinder
    {
        public const int StepCost = 10;
        public const int ExistingPathStepCost = 4;
        public const int TurnCost = 6;
        private const int MaxExpanded = 50000;

        /// Index i is bit (1 << i) of the Path_*_00…15 sprite mask. The sheet's own x/y axes do not match
        /// the Unity isometric Grid (+x = up-right, +y = up-left on screen); the bits point on screen to
        /// 1 = up-left (+y), 2 = down-right (-y), 4 = up-right (+x), 8 = down-left (-x).
        public static readonly Vector3Int[] Directions = { Vector3Int.up, Vector3Int.down, Vector3Int.right, Vector3Int.left };

        public static int DirectionBit(Vector3Int step)
        {
            for (int i = 0; i < Directions.Length; i++)
                if (Directions[i] == step) return 1 << i;
            return 0;
        }

        /// Finds the cheapest route from one of the start cells to the first target cell.
        /// route[0] is the start cell, the last item is the target cell.
        public static bool TryFind(IEnumerable<Vector3Int> starts, Func<Vector3Int, bool> isWalkable,
            Func<Vector3Int, bool> isTarget, List<Vector3Int> route, Func<Vector3Int, bool> isExistingPath = null,
            int stepCost = StepCost, int pathStepCost = ExistingPathStepCost, int turnCost = TurnCost)
        {
            route.Clear();
            var best = new Dictionary<Node, int>();
            var parent = new Dictionary<Node, Node>();
            var open = new MinHeap();

            foreach (Vector3Int start in starts)
            {
                var node = new Node(start, -1);
                if (best.ContainsKey(node) || !isWalkable(start)) continue;
                best[node] = 0;
                open.Push(0, node);
            }

            int expanded = 0;
            while (open.TryPop(out int cost, out Node node))
            {
                if (cost > best[node]) continue;
                if (isTarget(node.Cell))
                {
                    BuildRoute(node, parent, route);
                    return true;
                }
                if (++expanded > MaxExpanded) break;

                for (int d = 0; d < Directions.Length; d++)
                {
                    Vector3Int next = node.Cell + Directions[d];
                    if (!isWalkable(next)) continue;

                    int step = isExistingPath != null && isExistingPath(next) ? pathStepCost : stepCost;
                    if (node.Direction >= 0 && node.Direction != d) step += turnCost;

                    var nextNode = new Node(next, d);
                    int nextCost = cost + step;
                    if (best.TryGetValue(nextNode, out int known) && known <= nextCost) continue;

                    best[nextNode] = nextCost;
                    parent[nextNode] = node;
                    open.Push(nextCost, nextNode);
                }
            }
            return false;
        }

        private static void BuildRoute(Node end, Dictionary<Node, Node> parent, List<Vector3Int> route)
        {
            Node node = end;
            route.Add(node.Cell);
            while (parent.TryGetValue(node, out Node previous))
            {
                node = previous;
                route.Add(node.Cell);
            }
            route.Reverse();
        }

        /// A cell together with the direction it was entered from (-1 = start), so turns can be priced.
        private readonly struct Node : IEquatable<Node>
        {
            public readonly Vector3Int Cell;
            public readonly int Direction;

            public Node(Vector3Int cell, int direction)
            {
                Cell = cell;
                Direction = direction;
            }

            public bool Equals(Node other) => Cell == other.Cell && Direction == other.Direction;
            public override bool Equals(object obj) => obj is Node other && Equals(other);
            public override int GetHashCode() => Cell.GetHashCode() * 5 + Direction + 1;
        }

        /// Binary heap ordered by cost, then by insertion order (keeps results deterministic).
        private sealed class MinHeap
        {
            private readonly List<(int cost, int order, Node node)> items = new List<(int, int, Node)>();
            private int counter;

            public void Push(int cost, Node node)
            {
                items.Add((cost, counter++, node));
                int i = items.Count - 1;
                while (i > 0)
                {
                    int up = (i - 1) / 2;
                    if (!Less(items[i], items[up])) break;
                    (items[i], items[up]) = (items[up], items[i]);
                    i = up;
                }
            }

            public bool TryPop(out int cost, out Node node)
            {
                if (items.Count == 0)
                {
                    cost = 0;
                    node = default;
                    return false;
                }

                cost = items[0].cost;
                node = items[0].node;
                int last = items.Count - 1;
                items[0] = items[last];
                items.RemoveAt(last);

                int i = 0;
                while (true)
                {
                    int left = i * 2 + 1, right = left + 1, smallest = i;
                    if (left < items.Count && Less(items[left], items[smallest])) smallest = left;
                    if (right < items.Count && Less(items[right], items[smallest])) smallest = right;
                    if (smallest == i) break;
                    (items[i], items[smallest]) = (items[smallest], items[i]);
                    i = smallest;
                }
                return true;
            }

            private static bool Less((int cost, int order, Node node) a, (int cost, int order, Node node) b)
                => a.cost != b.cost ? a.cost < b.cost : a.order < b.order;
        }
    }
}
