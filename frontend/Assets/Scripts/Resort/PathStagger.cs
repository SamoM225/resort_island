using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResortIsland.Resort
{
    /// Order in which path cells are animated: steps (4-neighbourhood, inside the given set) from the
    /// seed cells. A part of the set that touches no seed starts from its first cell.
    public static class PathStagger
    {
        public static Dictionary<Vector3Int, int> Steps(IReadOnlyList<Vector3Int> cells, Func<Vector3Int, bool> isSeed)
        {
            var set = new HashSet<Vector3Int>(cells);
            var steps = new Dictionary<Vector3Int, int>(cells.Count);
            var queue = new Queue<Vector3Int>();

            foreach (Vector3Int cell in cells)
            {
                if (!isSeed(cell)) continue;
                steps[cell] = 0;
                queue.Enqueue(cell);
            }
            Spread(set, steps, queue);

            foreach (Vector3Int cell in cells)
            {
                if (steps.ContainsKey(cell)) continue;
                steps[cell] = 0;
                queue.Enqueue(cell);
                Spread(set, steps, queue);
            }
            return steps;
        }

        private static void Spread(HashSet<Vector3Int> set, Dictionary<Vector3Int, int> steps, Queue<Vector3Int> queue)
        {
            while (queue.Count > 0)
            {
                Vector3Int cell = queue.Dequeue();
                foreach (Vector3Int step in PathFinder.Directions)
                {
                    Vector3Int next = cell + step;
                    if (!set.Contains(next) || steps.ContainsKey(next)) continue;
                    steps[next] = steps[cell] + 1;
                    queue.Enqueue(next);
                }
            }
        }
    }
}
