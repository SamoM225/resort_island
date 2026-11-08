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

        [Header("Placement")]
        [Tooltip("Cells occupied on the island grid (matches the ResortEmpire sprite footprint).")]
        [SerializeField] private Vector2Int footprint = Vector2Int.one;
        [Tooltip("The island generator places this building on the beach instead of inland.")]
        [SerializeField] private bool preferShore;

        [Header("Visual")]
        [SerializeField] private GameObject prefab;

        public string Id => id;
        public string DisplayName => displayName;
        public NeedConfig ServedNeed => servedNeed;
        public string UnlockId => unlockId;
        public int MaxCount => maxCount;
        public Vector2Int Footprint => new Vector2Int(Mathf.Max(1, footprint.x), Mathf.Max(1, footprint.y));
        public bool PreferShore => preferShore;
        public GameObject Prefab => prefab;
    }
}
