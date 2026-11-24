using UnityEngine;

namespace ResortIsland.Config
{
    [CreateAssetMenu(fileName = "BuildingConfig", menuName = "ResortIsland/Config/BuildingConfig")]
    public class BuildingConfig : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;

        [Header("Gameplay")]
        [SerializeField] private NeedConfig servedNeed;

        [Header("Availability")]
        [SerializeField] private string unlockId;     
        [SerializeField] private int maxCount = 1;
        [Tooltip("Coins paid when the player builds it from the build menu (0 = free).")]
        [SerializeField, Min(0)] private int cost;
        [Header("Service")]
        [SerializeField, Min(1)] private int capacity = 4;

        [Header("Placement")]
        [Tooltip("Cells occupied on the island grid (matches the ResortEmpire sprite footprint).")]
        [SerializeField] private Vector2Int footprint = Vector2Int.one;
        [Tooltip("The island generator places this building on the beach instead of inland.")]
        [SerializeField] private bool preferShore;
        [Tooltip("Cell in front of the door for rotation R0, relative to the footprint origin (its lowest x/y " +
                 "cell). It lies just outside the footprint: paths lead here and guests go in here. " +
                 "(-1, y) = the front-left side, the side the R0 sprites face.")]
        [SerializeField] private Vector2Int entrance = new Vector2Int(-1, 1);

        [Header("Visual")]
        [SerializeField] private GameObject prefab;
        [Tooltip("Sprite for each rotation R0…R3 (the ResortEmpire sheet columns). Empty = the prefab sprite.")]
        [SerializeField] private Sprite[] rotationSprites = new Sprite[4];

        public string Id => id;
        public string DisplayName => displayName;
        public NeedConfig ServedNeed => servedNeed;
        public string UnlockId => unlockId;
        public int MaxCount => maxCount;
        public int Cost => cost;
        public int Capacity => Mathf.Max(1, capacity);
        public Vector2Int Footprint => new Vector2Int(Mathf.Max(1, footprint.x), Mathf.Max(1, footprint.y));
        public bool PreferShore => preferShore;
        public GameObject Prefab => prefab;
        public Vector2Int Entrance => entrance;

        /// Sprite of the building turned to rotation 0…3; null when the config has none for it.
        public Sprite SpriteFor(int rotation)
        {
            if (rotationSprites == null || rotationSprites.Length == 0) return null;
            int index = ((rotation % 4) + 4) % 4;
            return index < rotationSprites.Length ? rotationSprites[index] : null;
        }
    }
}
