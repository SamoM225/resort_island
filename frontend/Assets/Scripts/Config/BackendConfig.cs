using UnityEngine;

namespace ResortIsland.Config
{
    /// <summary>
    /// Public backend URLs per environment. Only hosts belong here, never keys or passwords.
    /// Lives in Resources so <see cref="ResortIsland.Backend.BackendEnvironment"/> can load it without a scene reference.
    /// </summary>
    [CreateAssetMenu(fileName = "BackendConfig", menuName = "ResortIsland/Config/BackendConfig")]
    public class BackendConfig : ScriptableObject
    {
        public const string ResourcePath = "BackendConfig";

        [Header("Environments")]
        [Tooltip("Used by the Editor and Development Builds.")]
        [SerializeField] private string developmentUrl = "https://dev-api.resort-island.invalid";
        [Tooltip("Used by release builds.")]
        [SerializeField] private string productionUrl = "https://api.resort-island.invalid";

        [Header("Editor")]
        [Tooltip("Optional, Editor only — e.g. http://localhost:8000 for a local backend. Empty = development URL.")]
        [SerializeField] private string editorOverrideUrl = "";

        public string DevelopmentUrl => developmentUrl;
        public string ProductionUrl => productionUrl;
        public string EditorOverrideUrl => editorOverrideUrl;
    }
}
