using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ResortIsland.Resort
{
    public static class NearestCells
    {
        public static List<Vector3Int> Take(IEnumerable<Vector3Int> cells, Vector3Int from, int count)
        {
            List<Vector3Int> sorted = cells.OrderBy(cell => (cell - from).sqrMagnitude).ToList();
            List<Vector3Int> result = new List<Vector3Int>();
            if (sorted.Count == 0)
                return result;

            for (int i = 0; i < count; i++)
                result.Add(sorted[i % sorted.Count]);
            return result;
        }
    }
}
