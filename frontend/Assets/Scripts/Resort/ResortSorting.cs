using UnityEngine;
using UnityEngine.Rendering;

namespace ResortIsland.Resort
{
    public static class ResortSorting
    {
        public const string LayerName = "Buildings";

        // čím nižšie na obrazovke, tým viac vpredu
        public static void Apply(GameObject go)
        {
            var group = go.GetComponent<SortingGroup>();
            if (group == null) group = go.AddComponent<SortingGroup>();

            group.sortingLayerName = LayerName;
            group.sortingOrder = -Mathf.RoundToInt(go.transform.position.y * 100f);
        }
    }
}