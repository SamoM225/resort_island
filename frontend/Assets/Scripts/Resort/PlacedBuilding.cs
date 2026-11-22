using UnityEngine;
using ResortIsland.Config;

namespace ResortIsland.Resort
{
    public class PlacedBuilding : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Sprite prefabSprite;

        public BuildingConfig Config { get; private set; }
        /// Origin (lowest x/y cell) of the footprint.
        public Vector3Int Cell { get; private set; }
        /// 0…3, see BuildingRotation.
        public int Rotation { get; private set; }
        /// Footprint in the current rotation.
        public Vector2Int Footprint => Config != null ? BuildingRotation.Size(Config.Footprint, Rotation) : Vector2Int.one;
        /// Cell in front of the door: paths end here and guests go in here.
        public Vector3Int EntranceCell => EntranceFor(Cell, Rotation);

        public Vector3Int EntranceFor(Vector3Int origin, int rotation)
            => Config != null ? BuildingRotation.EntranceCell(origin, Config.Footprint, Config.Entrance, rotation) : origin;

        public void Init(BuildingConfig config)
        {
            Config = config;
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null) prefabSprite = spriteRenderer.sprite;
        }

        public void SetCell(Vector3Int cell, Vector3 worldPosition)
        {
            Cell = cell;
            transform.position = worldPosition;
            ResortSorting.Apply(gameObject);
        }

        public void SetRotation(int rotation)
        {
            Rotation = BuildingRotation.Normalize(rotation);
            ShowRotation(Rotation);
        }

        /// Shows the sprite of a rotation without changing the building (e.g. while dragging).
        public void ShowRotation(int rotation)
        {
            if (spriteRenderer == null) return;
            Sprite sprite = Config != null ? Config.SpriteFor(rotation) : null;
            spriteRenderer.sprite = sprite != null ? sprite : prefabSprite;
        }
    }
}
