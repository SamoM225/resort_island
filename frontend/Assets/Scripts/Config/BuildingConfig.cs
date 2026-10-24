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

        [Header("Visual")]
        [SerializeField] private GameObject prefab;

        public string Id => id;
        public string DisplayName => displayName;
        public NeedConfig ServedNeed => servedNeed;
        public string UnlockId => unlockId;
        public int MaxCount => maxCount;
        public GameObject Prefab => prefab;
    }
}