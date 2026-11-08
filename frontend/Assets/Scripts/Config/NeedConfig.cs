using UnityEngine;

namespace ResortIsland.Config
{
    [CreateAssetMenu(fileName = "NeedConfig", menuName = "ResortIsland/Config/NeedConfig")]
    public class NeedConfig : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;

        public string Id => id;
        public string DisplayName => displayName;
    }
}