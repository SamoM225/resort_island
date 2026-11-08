using UnityEngine;
using ResortIsland.Config;

namespace ResortIsland.Resort
{
    public class PlacedBuilding : MonoBehaviour
    {
        public BuildingConfig Config { get; private set; }
        /// Origin (lowest x/y cell) of the footprint.
        public Vector3Int Cell { get; private set; }
        public Vector2Int Footprint => Config != null ? Config.Footprint : Vector2Int.one;

        public void Init(BuildingConfig config) => Config = config;

        public void SetCell(Vector3Int cell, Vector3 worldPosition)
        {
            Cell = cell;
            transform.position = worldPosition;
            ResortSorting.Apply(gameObject);
        }
    }
}